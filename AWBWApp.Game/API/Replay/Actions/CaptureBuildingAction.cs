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

    public class CaptureBuildingAction : IReplayAction, IActionCanEndGame
    {
        public MoveUnitAction MoveUnit;
        public ReplayBuilding Building;

        public Dictionary<long, int> IncomeChanges;

        public EliminatedAction EliminatedAction;

        private ReplayBuilding originalBuilding;
        private Dictionary<long, int> originalIncomes;

        private ReplayUnit originalUnit;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName || !controller.Map.TryGetDrawableBuilding(originalBuilding.Position, out var building))
                return MoveUnit != null ? "Move + Capture" : "Capture";

            string moveUnitString;
            if (MoveUnit != null && controller.Map.TryGetDrawableUnit(MoveUnit.Unit.ID, out var moveUnit))
                moveUnitString = $"{moveUnit.UnitData.Name} Moves + ";
            else if (originalUnit != null)
                moveUnitString = $"{originalUnit.UnitName} ";
            else
                moveUnitString = "";

            var captureState = Building.TerrainID != originalBuilding.TerrainID ? "Captures " : (Building.LastCapture == 20 ? "Begins Capturing " : "Capturing ");

            return moveUnitString + captureState + building.BuildingTile.Name;
        }

        public bool EndsGame() => EliminatedAction?.EndsGame() ?? false;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Buildings.TryGetValue(Building.Position, out var building))
                throw new ReplayMissingBuildingException(Building.ID);

            originalBuilding = building.Clone();
            building.Overwrite(Building);

            foreach (var unit in context.Units)
            {
                if (unit.Value.Position == building.Position)
                {
                    originalUnit = unit.Value.Clone();
                    unit.Value.TimesMoved = 1;
                }
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

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var actionHidden = controller.ShouldPlayerActionBeHidden(Building.Position);

            if (controller.Map.TryGetDrawableUnit(Building.Position, out var capturingUnit))
            {
                if (!actionHidden)
                    capturingUnit.FadeTo(0.5f, 250, Easing.OutCubic);

                if (MoveUnit == null || !actionHidden)
                {
                    var anim = controller.Map.PlaySelectionAnimation(capturingUnit);
                    yield return ReplayWait.WaitForTransformable(anim);
                }

                if (!actionHidden)
                    capturingUnit.CanMove.Value = false;
            }

            if (controller.Map.TryGetDrawableBuilding(Building.Position, out var capturingBuilding))
            {
                if (capturingUnit == null)
                {
                    var anim = controller.Map.PlaySelectionAnimation(capturingBuilding);
                    yield return ReplayWait.WaitForTransformable(anim);
                }

                if (!actionHidden)
                {
                    capturingBuilding.MoveToOffset(new Vector2(3, 0), 30).Then().MoveToOffset(new Vector2(-6, 0), 60).Then().MoveToOffset(new Vector2(3, 0), 30);
                    yield return ReplayWait.WaitForTransformable(capturingBuilding);
                }
            }

            controller.Map.UpdateBuilding(Building, false); //This will set the unit above to be capturing

            if (IncomeChanges != null)
            {
                foreach (var incomeChange in IncomeChanges)
                    controller.Players[incomeChange.Key].PropertyValue.Value = incomeChange.Value;
            }

            if (controller.Map.TryGetDrawableBuilding(Building.Position, out capturingBuilding))
            {
                capturingBuilding.MoveToOffset(new Vector2(3, 0), 30).Then().MoveToOffset(new Vector2(-6, 0), 60).Then().MoveToOffset(new Vector2(3, 0), 30);

                if (Building.TerrainID.HasValue && Building.TerrainID != originalBuilding.TerrainID)
                {
                    capturingBuilding.ScaleTo(new Vector2(1.25f), 200, Easing.InOutSine).MoveToOffset(new Vector2(-2, -4), 200, Easing.InOutSine)
                                     .Then().ScaleTo(new Vector2(1f), 200, Easing.InOutSine).MoveToOffset(new Vector2(2, 4), 200, Easing.InOutSine);
                }
                else
                    capturingBuilding.MoveToOffset(new Vector2(3, 0), 30).Then().MoveToOffset(new Vector2(-6, 0), 60).Then().MoveToOffset(new Vector2(3, 0), 30);

                yield return ReplayWait.WaitForTransformable(capturingBuilding);
            }

            if (!actionHidden)
            {
                if (capturingUnit != null)
                {
                    capturingUnit?.FadeTo(capturingUnit.Dived.Value ? 0.7f : 1, 250, Easing.OutCubic);
                    yield return ReplayWait.WaitForTransformable(capturingUnit);
                }
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
