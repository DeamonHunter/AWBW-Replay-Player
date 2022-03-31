using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osuTK;

namespace AWBWApp.Game.API.Replay.Actions
{
    /// <summary>
    /// This action always appears at the end of a turn, and gives information about the next turn.
    /// </summary>
    public class EndTurnActionBuilder : IReplayActionBuilder
    {
        public string Code => "End";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new EndTurnAction();

            var updatedInfo = (JObject)jObject["updatedInfo"];

            var endEvent = (string)updatedInfo["event"];

            if (endEvent != "NextTurn")
                throw new NotImplementedException("End turn actions that don't go to the next turn are not implemented.");

            action.NextPlayerID = (long)updatedInfo["nextPId"];
            action.NextDay = (int)updatedInfo["day"];

            var nextTeam = replayData.ReplayInfo.Players[action.NextPlayerID].TeamName;

            action.FundsAfterTurnStart = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)updatedInfo["nextFunds"], nextTeam, action.NextPlayerID);
            action.NextWeather = WeatherHelper.ParseWeatherCode((string)updatedInfo["nextWeather"]);

            var suppliedData = updatedInfo["supplied"];

            if (suppliedData?.Type != JTokenType.Null)
            {
                var supplied = (JArray)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)suppliedData, nextTeam, action.NextPlayerID);

                action.SuppliedUnits = new HashSet<long>();

                foreach (var suppliedUnit in supplied)
                    action.SuppliedUnits.Add((long)suppliedUnit);
            }
            var repairedData = updatedInfo["repaired"];

            if (repairedData?.Type != JTokenType.Null)
            {
                var repaired = (JArray)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)repairedData, nextTeam, action.NextPlayerID);

                action.RepairedUnits = new Dictionary<long, int>();

                foreach (var repairedUnit in repaired)
                {
                    var repairedUnitData = (JObject)repairedUnit;
                    var id = (long)repairedUnitData["units_id"];
                    if (!action.RepairedUnits.ContainsKey(id))
                        action.RepairedUnits.Add(id, (int)repairedUnitData["units_hit_points"]);
                }
            }
            return action;
        }
    }

    public class EndTurnAction : IReplayAction
    {
        public long NextPlayerID;
        public int NextDay;

        public int FundsAfterTurnStart;
        public Weather NextWeather;

        public HashSet<long> SuppliedUnits;
        public Dictionary<long, int> RepairedUnits;

        private Dictionary<long, ReplayUnit> unitsToDestroy = new Dictionary<long, ReplayUnit>();
        private Dictionary<long, ReplayUnit> originalUnits = new Dictionary<long, ReplayUnit>();
        private HashSet<long> waitUnits = new HashSet<long>();
        private int repairCost;
        private int repairValue;
        private int currentDay;

        public string GetReadibleName(ReplayController controller, bool shortName) => "End Turn";

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            currentDay = context.CurrentDay;

            repairCost = context.FundsValuesForPlayers[NextPlayerID] - (FundsAfterTurnStart - context.PropertyValuesForPlayers[NextPlayerID]);
            context.FundsValuesForPlayers[NextPlayerID] -= repairCost;
            context.StatsReadouts[NextPlayerID].MoneySpentOnRepairingUnits += repairCost;

            if (SuppliedUnits != null)
            {
                foreach (var suppliedID in SuppliedUnits)
                {
                    if (!context.Units.TryGetValue(suppliedID, out var supplied))
                        throw new ReplayMissingUnitException(suppliedID);

                    if (!originalUnits.ContainsKey(suppliedID))
                        originalUnits.Add(suppliedID, supplied.Clone());

                    var unitData = controller.Map.GetUnitDataForUnitName(supplied.UnitName);
                    supplied.Ammo = unitData.MaxAmmo;
                    supplied.Fuel = unitData.MaxFuel;
                }
            }

            if (RepairedUnits != null)
            {
                foreach (var repairedPair in RepairedUnits)
                {
                    if (!context.Units.TryGetValue(repairedPair.Key, out var repaired))
                        throw new ReplayMissingUnitException(repairedPair.Key);

                    if (!originalUnits.ContainsKey(repairedPair.Key))
                        originalUnits.Add(repairedPair.Key, repaired.Clone());

                    var dayToDay = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[repaired.PlayerID!.Value].ActiveCOID).DayToDayPower;

                    var originalValue = ReplayActionHelper.CalculateUnitCost(repaired, dayToDay, null);

                    var unitData = controller.Map.GetUnitDataForUnitName(repaired.UnitName);
                    repaired.Ammo = unitData.MaxAmmo;
                    repaired.Fuel = unitData.MaxFuel;
                    repaired.HitPoints = repairedPair.Value;

                    repairValue += ReplayActionHelper.CalculateUnitCost(repaired, dayToDay, null) - originalValue;
                }
            }

            foreach (var unit in context.Units)
            {
                if (!unit.Value.PlayerID.HasValue)
                    continue;

                if (unit.Value.PlayerID == NextPlayerID)
                {
                    var unitData = controller.Map.GetUnitDataForUnitName(unit.Value.UnitName);

                    int fuelUsage = unitData.FuelUsagePerTurn;

                    if (unitData.MovementType == MovementType.Air)
                    {
                        var dayToDay = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[unit.Value.PlayerID!.Value].ActiveCOID).DayToDayPower;
                        fuelUsage -= dayToDay.AirFuelUsageDecrease;
                    }

                    if (fuelUsage > 0 && NextDay > 1)
                    {
                        //If Original Units contains this unit, then they were supplied with fuel/ammo
                        if (!originalUnits.ContainsKey(unit.Key))
                        {
                            originalUnits.Add(unit.Key, unit.Value.Clone());

                            unit.Value.Fuel = Math.Max(0, unit.Value.Fuel!.Value - fuelUsage);

                            if (unit.Value.Fuel <= 0 && unitData.MovementType is MovementType.Air or MovementType.Lander or MovementType.Sea)
                                context.RemoveUnitFromSetupContext(unit.Key, unitsToDestroy, out var _);
                        }
                    }
                }
                else if (unit.Value.PlayerID == context.ActivePlayerID)
                {
                    if (unit.Value.TimesMoved != 0)
                    {
                        unit.Value.TimesMoved = 0;
                        waitUnits.Add(unit.Key);
                    }
                }
            }

            foreach (var destroyedUnit in unitsToDestroy)
            {
                if (!originalUnits.ContainsKey(destroyedUnit.Key))
                    originalUnits.Add(destroyedUnit.Key, destroyedUnit.Value);
            }
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing End Turn Action.");
            //Note: We aren't updating the StatsReadout here as it would just get set by the next turn anyway.

            var player = controller.Players[NextPlayerID];

            var endTurnPopup = new EndTurnPopupDrawable(player, NextDay);
            controller.AddGenericActionAnimation(endTurnPopup);
            yield return ReplayWait.WaitForTransformable(endTurnPopup);

            if (RepairedUnits != null)
            {
                foreach (var repairedUnit in RepairedUnits)
                {
                    var unit = controller.Map.GetDrawableUnit(repairedUnit.Key);
                    unit.HealthPoints.Value = repairedUnit.Value;
                    unit.Ammo.Value = unit.UnitData.MaxAmmo;
                    unit.Fuel.Value = unit.UnitData.MaxFuel;

                    controller.Map.PlayEffect("Effects/Supplied", 600, unit.MapPosition, 0,
                        x => x.ScaleTo(new Vector2(0, 1))
                              .ScaleTo(1, 250, Easing.OutQuint)
                              .Delay(400).ScaleTo(new Vector2(0, 1), 150, Easing.InQuart)
                              .Delay(125).FadeOut());
                    yield return ReplayWait.WaitForMilliseconds(50);
                }
            }

            controller.Players[NextPlayerID].Funds.Value -= repairCost;
            controller.Players[NextPlayerID].UnitValue.Value += repairValue;

            if (SuppliedUnits != null)
            {
                foreach (var suppliedUnit in SuppliedUnits)
                {
                    var unit = controller.Map.GetDrawableUnit(suppliedUnit);
                    unit.Ammo.Value = unit.UnitData.MaxAmmo;
                    unit.Fuel.Value = unit.UnitData.MaxFuel;

                    controller.Map.PlayEffect("Effects/Supplied", 600, unit.MapPosition, 0,
                        x => x.ScaleTo(new Vector2(0, 1))
                              .ScaleTo(1, 250, Easing.OutQuint)
                              .Delay(400).ScaleTo(new Vector2(0, 1), 150, Easing.InQuart)
                              .Delay(125).FadeOut());
                    yield return ReplayWait.WaitForMilliseconds(50);
                }
            }

            foreach (var destroyedUnit in unitsToDestroy)
                controller.Map.DeleteUnit(destroyedUnit.Key, false);

            //Todo: Ignore Funds after turn start and next weather? These are already handled by GoToNextTurn()
            //Maybe have a weather changing animation?
            controller.GoToNextTurn(false);
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing End Turn Action.");

            controller.Stats.CurrentTurnStatsReadout[controller.ActivePlayer.ID].MoneySpentOnRepairingUnits -= repairCost;

            foreach (var unit in originalUnits)
            {
                if (controller.Map.TryGetDrawableUnit(unit.Key, out var drawableUnit))
                    drawableUnit.UpdateUnit(unit.Value);
                else
                {
                    var dayToDay = controller.Players[unit.Value.PlayerID!.Value].ActiveCO.Value.CO.DayToDayPower;
                    var value = ReplayActionHelper.CalculateUnitCost(unit.Value, dayToDay, null);

                    controller.Stats.CurrentTurnStatsReadout[unit.Value.PlayerID!.Value].RegisterUnitStats(UnitStatType.LostUnit | UnitStatType.UnitCountChanged, unit.Value.UnitName, value);
                    controller.Map.AddUnit(unit.Value);
                }
            }

            foreach (var unit in waitUnits)
                controller.Map.GetDrawableUnit(unit).CanMove.Value = false;

            controller.Players[NextPlayerID].Funds.Value += repairCost;
            controller.Players[NextPlayerID].UnitValue.Value -= repairValue;

            if (!controller.SkipEndTurnPopup())
            {
                var endTurnPopup = new EndTurnPopupDrawable(controller.ActivePlayer, currentDay);
                controller.AddGenericActionAnimation(endTurnPopup);
            }

            controller.UpdateFogOfWar();
        }
    }
}
