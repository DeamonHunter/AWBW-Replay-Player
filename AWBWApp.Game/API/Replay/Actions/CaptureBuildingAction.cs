using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
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
                action.IncomeChanges = new Dictionary<long, int>();
                var idx = 0;

                foreach (var playerIncome in incomeData)
                {
                    var playerIncomeData = (JObject)playerIncome.Value;
                    action.IncomeChanges.Add((long)playerIncomeData["player"], (int)playerIncomeData["income"]);
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
        public string ReadibleName => "Capture";

        public MoveUnitAction MoveUnit;
        public ReplayBuilding Building;

        public Dictionary<long, int> IncomeChanges;

        public EliminatedAction EliminatedAction;

        private ReplayBuilding originalBuilding;
        private Dictionary<long, int> originalIncomes;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Buildings.TryGetValue(Building.Position, out var building))
                throw new ReplayMissingBuildingException(Building.ID);

            originalBuilding = building.Clone();
            building.Copy(Building);

            foreach (var unit in context.Units)
            {
                if (unit.Value.Position == building.Position)
                    unit.Value.TimesMoved = 1;
            }

            if (IncomeChanges != null)
            {
                originalIncomes = new Dictionary<long, int>();

                foreach (var incomeChange in IncomeChanges)
                {
                    originalIncomes.Add(incomeChange.Key, context.PropertyValuesForPlayers[incomeChange.Key]);
                    context.PropertyValuesForPlayers[incomeChange.Key] = incomeChange.Value;
                }
            }
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Capture Action.");
            Logger.Log("Todo: Building capture animation not implemented.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            if (controller.Map.TryGetDrawableUnit(Building.Position, out var capturingUnit))
                capturingUnit.CanMove.Value = false;

            controller.Map.UpdateBuilding(Building, false); //This will set the unit above to be capturing

            if (IncomeChanges != null)
            {
                foreach (var incomeChange in IncomeChanges)
                    controller.Players[incomeChange.Key].PropertyValue.Value = incomeChange.Value;
            }

            //Capturing a building can eliminate a player. i.e. They have no buildings left or reached the total building goal.
            if (EliminatedAction != null)
            {
                foreach (var transformable in EliminatedAction.PerformAction(controller))
                    yield return transformable;
            }
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Capture Action.");
            controller.Map.UpdateBuilding(originalBuilding, true);

            if (originalIncomes != null)
            {
                foreach (var incomeChange in originalIncomes)
                    controller.Players[incomeChange.Key].PropertyValue.Value = incomeChange.Value;
            }

            if (MoveUnit != null)
                MoveUnit.UndoAction(controller);
            else if (controller.Map.TryGetDrawableUnit(originalBuilding.Position, out var capturingUnit))
                capturingUnit.CanMove.Value = true;
        }
    }
}
