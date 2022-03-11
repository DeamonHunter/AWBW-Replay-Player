using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
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

            var incomeObj = captureData["income"];

            if (incomeObj is JObject incomeData)
            {
                action.IncomeChanges = new CaptureBuildingAction.IncomeChanged[incomeData.Count];

                var idx = 0;

                foreach (var playerIncome in incomeData)
                {
                    var playerIncomeData = (JObject)playerIncome.Value;
                    action.IncomeChanges[idx].PlayerId = (long)playerIncomeData["player"];
                    action.IncomeChanges[idx].AmountChanged = (int)playerIncomeData["income"];
                    idx++;
                }
            }

            if (captureData.TryGetValue("eliminated", out var eliminatedData) && eliminatedData.Type != JTokenType.Null)
            {
                var eliminationAction = Database.GetActionBuilder("Eliminated").ParseJObjectIntoReplayAction((JObject)eliminatedData, replayData, turnData);
                action.EliminatedAction = eliminationAction as EliminatedAction;
                if (eliminationAction == null)
                    throw new Exception("Capture action was expecting a elimination action.");
            }

            return action;
        }
    }

    public class CaptureBuildingAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;
        public ReplayBuilding Building;

        public IncomeChanged[] IncomeChanges;

        public EliminatedAction EliminatedAction;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Capture Action.");
            Logger.Log("Income change not implemented.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var capturingUnit = MoveUnit != null ? controller.Map.GetDrawableUnit(MoveUnit.Unit.ID) : controller.Map.GetDrawableUnit(Building.Position);

            yield return ReplayWait.WaitForTransformable(capturingUnit);

            //Todo: Capture building animation
            capturingUnit.CanMove.Value = false;
            controller.Map.UpdateBuilding(Building, false); //This will set the unit above to be capturing

            if (IncomeChanges != null)
            {
                foreach (var incomeChange in IncomeChanges)
                    controller.Players[incomeChange.PlayerId].PropertyValue.Value += incomeChange.AmountChanged;
            }

            //Capturing a building can eliminate a player. i.e. They have no buildings left or reached the total building goal.
            if (EliminatedAction != null)
            {
                foreach (var transformable in EliminatedAction.PerformAction(controller))
                    yield return transformable;
            }
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Capture Action is not complete");
        }

        public struct IncomeChanged
        {
            public long PlayerId;
            public int AmountChanged;
        }
    }
}
