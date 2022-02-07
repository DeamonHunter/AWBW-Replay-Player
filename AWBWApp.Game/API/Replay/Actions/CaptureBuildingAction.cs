using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class CaptureBuildingActionBuilder : IReplayActionBuilder
    {
        public string Code => "Capt";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new CaptureBuildingAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var captureData = (JObject)jObject["Capt"];
            if (captureData == null)
                throw new Exception("Capture Replay Action did not contain information about Capture.");

            action.Building = ReplayActionHelper.ParseJObjectIntoReplayBuilding((JObject)captureData["buildingInfo"]);

            var incomeObj = jObject["income"];

            if (incomeObj is JObject incomeData)
            {
                action.IncomeChanges = new CaptureBuildingAction.IncomeChanged[incomeData.Count];

                var idx = 0;

                foreach (var playerIncome in incomeData.Children())
                {
                    var playerIncomeData = (JObject)playerIncome;
                    action.IncomeChanges[idx].PlayerId = (int)playerIncomeData["player"];
                    action.IncomeChanges[idx].AmountChanged = (int)playerIncomeData["income"];
                    idx++;
                }
            }
            return action;
        }
    }

    public class CaptureBuildingAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;
        public ReplayBuilding Building;

        public IncomeChanged[] IncomeChanges;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Capture Action.");
            Logger.Log("Income change not implemented.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            DrawableUnit capturingUnit;
            if (MoveUnit != null)
                capturingUnit = controller.Map.GetDrawableUnit(MoveUnit.Unit.ID);
            else
                capturingUnit = controller.Map.GetDrawableUnit(Building.Position);

            yield return ReplayWait.WaitForTransformable(capturingUnit);

            //Todo: Capture building animation
            capturingUnit.CanMove.Value = false;
            controller.Map.UpdateBuilding(Building, false); //This will set the unit above to be capturing
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }

        public struct IncomeChanged
        {
            public int PlayerId;
            public int AmountChanged;
        }
    }
}
