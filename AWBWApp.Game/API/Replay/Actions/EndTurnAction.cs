using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
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

                action.SuppliedUnits = new List<long>();

                foreach (var suppliedUnit in supplied)
                    action.SuppliedUnits.Add((long)suppliedUnit);
            }
            var repairedData = updatedInfo["repaired"];

            if (repairedData?.Type != JTokenType.Null)
            {
                var repaired = (JArray)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)repairedData, nextTeam, action.NextPlayerID);

                action.RepairedUnits = new List<(long, int)>();

                foreach (var repairedUnit in repaired)
                {
                    var repairedUnitData = (JObject)repairedUnit;
                    action.RepairedUnits.Add(((long)repairedUnitData["units_id"], (int)repairedUnitData["units_hit_points"]));
                }
            }
            return action;
        }
    }

    public class EndTurnAction : IReplayAction
    {
        public string ReadibleName => "End Turn";

        public long NextPlayerID;
        public int NextDay;

        public int FundsAfterTurnStart;
        public Weather NextWeather;

        public List<long> SuppliedUnits;
        public List<(long id, int hp)> RepairedUnits;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var player = controller.Players[NextPlayerID];

            var endTurnPopup = new EndTurnPopupDrawable(player, NextDay);
            controller.AddGenericActionAnimation(endTurnPopup);
            yield return ReplayWait.WaitForTransformable(endTurnPopup);

            if (RepairedUnits != null)
            {
                foreach (var (unitID, unitHP) in RepairedUnits)
                {
                    var unit = controller.Map.GetDrawableUnit(unitID);
                    unit.HealthPoints.Value = unitHP;
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

            //Todo: Ignore Funds after turn start and next weather? These are already handled by GoToNextTurn()
            //Maybe have a weather changing animation?
            controller.GoToNextTurn(false);
        }

        public void UndoAction(ReplayController controller)
        {
            throw new NotImplementedException("Undo EndTurn Action is not complete");
        }
    }
}
