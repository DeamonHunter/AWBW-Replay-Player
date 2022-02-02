using System;
using System.Collections.Generic;
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

            action.RepairingUnitID = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            var repairedUnit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["repaired"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.RepairedUnitID = (int)repairedUnit["units_id"];
            action.RepairedUnitHP = (int)repairedUnit["units_hit_points"];

            return action;
        }
    }

    public class RepairUnitAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;

        public int RepairingUnitID;

        public int RepairedUnitID;
        public int RepairedUnitHP;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Repair Action.");
            Logger.Log("Repair animation not implemented.");
            Logger.Log("Funds not implemented.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var unit = controller.Map.GetDrawableUnit(RepairedUnitID);
            unit.HealthPoints.Value = RepairedUnitHP;
            unit.Fuel.Value = unit.UnitData.MaxFuel;
            unit.Ammo.Value = unit.UnitData.MaxAmmo;

            controller.Map.PlayEffect("Effects/Supplied", 600, unit.MapPosition)
                      .ScaleTo(new Vector2(0, 1))
                      .ScaleTo(1, 250, Easing.OutQuint)
                      .Delay(400).ScaleTo(new Vector2(0, 1), 150, Easing.InQuart)
                      .Delay(125).FadeOut();
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
