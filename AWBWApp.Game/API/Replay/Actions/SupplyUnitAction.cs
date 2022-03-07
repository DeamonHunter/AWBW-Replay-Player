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
    public class SupplyUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Supply";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new SupplyUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var supplyData = (JObject)jObject["Supply"];
            if (supplyData == null)
                throw new Exception("Capture Replay Action did not contain information about Capture.");

            action.SupplyingUnitId = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            action.SuppliedUnitIds = new List<long>();
            foreach (var id in (JArray)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)supplyData["supplied"], turnData.ActiveTeam, turnData.ActivePlayerID))
                action.SuppliedUnitIds.Add((long)id);
            return action;
        }
    }

    public class SupplyUnitAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;

        public long SupplyingUnitId;
        public List<long> SuppliedUnitIds;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Supply Action.");
            Logger.Log("Supply animation not implemented.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            foreach (var unitId in SuppliedUnitIds)
            {
                var suppliedUnit = controller.Map.GetDrawableUnit(unitId);
                suppliedUnit.Ammo.Value = suppliedUnit.UnitData.MaxAmmo;
                suppliedUnit.Fuel.Value = suppliedUnit.UnitData.MaxFuel;

                controller.Map.PlayEffect("Effects/Supplied", 600, suppliedUnit.MapPosition, 0,
                    x => x.ScaleTo(new Vector2(0, 1))
                          .ScaleTo(1, 250, Easing.OutQuint)
                          .Delay(400).ScaleTo(new Vector2(0, 1), 150, Easing.InQuart)
                          .Delay(125).FadeOut());
                yield return ReplayWait.WaitForMilliseconds(100);
            }
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
