using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osuTK;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class RepairUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Repair";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new RepairUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var supplyData = (JObject)jObject["Repair"];
            if (supplyData == null)
                throw new Exception("Capture Replay Action did not contain information about Capture.");

            action.RepairingUnitID = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            var repairedUnit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["repaired"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.RepairedUnitID = (long)repairedUnit["units_id"];
            action.RepairedUnitHP = (int)repairedUnit["units_hit_points"];
            action.FundsAfterRepair = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["funds"], turnData.ActiveTeam, turnData.ActivePlayerID);

            return action;
        }
    }

    public class RepairUnitAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;

        public long RepairingUnitID;

        public long RepairedUnitID;
        public int RepairedUnitHP;

        public int FundsAfterRepair;

        private ReplayUnit originalRepairedUnit;
        private int repairValue;
        private int repairCost;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Repair" : "Repair";

            var repairedUnit = controller.Map.GetDrawableUnit(RepairedUnitID);
            var repairingUnit = controller.Map.GetDrawableUnit(RepairingUnitID);

            if (MoveUnit == null)
                return $"{repairingUnit.UnitData.Name} Repairs {repairedUnit.UnitData.Name}";

            return $"{repairingUnit.UnitData.Name} Moves + Repairs {repairedUnit.UnitData.Name}";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.TryGetValue(RepairedUnitID, out var repairedUnit))
                throw new ReplayMissingUnitException(RepairedUnitID);

            originalRepairedUnit = repairedUnit.Clone();

            var unitData = controller.Map.GetUnitDataForUnitName(repairedUnit.UnitName);
            repairedUnit.Ammo = unitData.MaxAmmo;
            repairedUnit.Fuel = unitData.MaxFuel;
            repairedUnit.HitPoints = RepairedUnitHP;

            repairCost = FundsAfterRepair - context.FundsValuesForPlayers[context.ActivePlayerID];
            context.FundsValuesForPlayers[context.ActivePlayerID] = FundsAfterRepair;

            var co = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[context.ActivePlayerID].ActiveCOID);
            repairValue = ReplayActionHelper.CalculateUnitCost(repairedUnit, co.DayToDayPower, null) - ReplayActionHelper.CalculateUnitCost(originalRepairedUnit, co.DayToDayPower, null);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Repair Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var unit = controller.Map.GetDrawableUnit(RepairedUnitID);
            unit.HealthPoints.Value = RepairedUnitHP;
            unit.Fuel.Value = unit.UnitData.MaxFuel;
            unit.Ammo.Value = unit.UnitData.MaxAmmo;

            controller.ActivePlayer.Funds.Value = FundsAfterRepair;
            controller.ActivePlayer.UnitValue.Value += repairValue;

            controller.Map.PlayEffect("Effects/Supplied", 600, unit.MapPosition, 0,
                x => x.ScaleTo(new Vector2(0, 1))
                      .ScaleTo(1, 250, Easing.OutQuint)
                      .Delay(400).ScaleTo(new Vector2(0, 1), 150, Easing.InQuart)
                      .Delay(125).FadeOut());
        }

        public void UndoAction(ReplayController controller)
        {
            controller.Map.GetDrawableUnit(RepairedUnitID).UpdateUnit(originalRepairedUnit);
            controller.ActivePlayer.Funds.Value -= repairCost;
            controller.ActivePlayer.UnitValue.Value -= repairValue;

            MoveUnit?.UndoAction(controller);
        }
    }
}
