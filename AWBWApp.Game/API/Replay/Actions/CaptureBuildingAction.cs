using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Building;
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

            var buildingInfo = (JObject)captureData["buildingInfo"];
            action.Building = ReplayActionHelper.ParseJObjectIntoReplayBuilding(buildingInfo);

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
                var eliminatedObj = (JObject)eliminatedData;
                if (!eliminatedObj.ContainsKey("eliminatedByPId"))
                    eliminatedObj["eliminatedByPId"] = turnData.ActivePlayerID;

                if (!eliminatedObj.ContainsKey("playerId"))
                    eliminatedObj["playerId"] = buildingInfo!.ContainsKey("buildings_players_id") ? (long)buildingInfo["buildings_players_id"] : replayData.ReplayInfo.Players.First(x => x.Key != turnData.ActivePlayerID).Key;

                if (!eliminatedObj.ContainsKey("message"))
                {
                    if (eliminatedObj.TryGetValue("GameOver", out var value))
                    {
                        var gameOver = (JObject)value;
                        if (gameOver!.TryGetValue("message", out var message))
                            eliminatedObj["message"] = message;
                        else
                            eliminatedObj["message"] = "Missing";
                    }
                    else
                        eliminatedObj["message"] = "Missing";
                }

                var eliminationAction = Database.GetActionBuilder("Eliminated").ParseJObjectIntoReplayAction(eliminatedObj, replayData, turnData);
                action.EliminatedAction = eliminationAction as EliminatedAction;
                if (eliminationAction == null)
                    throw new Exception("Capture action was expecting a elimination action.");
            }

            if (captureData.TryGetValue("vision", out var visionData))
            {
                if (visionData.Type == JTokenType.Object)
                {
                    foreach (var team in (JObject)visionData)
                    {
                        if (team.Value == null || team.Value.Type == JTokenType.Null)
                            continue;

                        var teamData = (JObject)team.Value;
                        if (!teamData.TryGetValue("onCapture", out var token) || token.Type == JTokenType.String)
                            continue;

                        action.TeamsThatSawCapture.Add(team.Key);
                    }
                }
            }

            return action;
        }
    }

    public class CaptureBuildingAction : IReplayAction, IActionCanEndGame
    {
        public bool SuccessfullySetup { get; set; }

        public MoveUnitAction MoveUnit;
        public ReplayBuilding Building;

        public Dictionary<long, int> IncomeChanges;

        public EliminatedAction EliminatedAction;

        private ReplayBuilding originalBuilding;
        private Dictionary<long, int> originalIncomes;

        public HashSet<string> TeamsThatSawCapture = new HashSet<string>();

        private ReplayUnit originalUnit;
        private Dictionary<string, BuildingTile> originalDiscovery;

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

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            return !controller.ShouldPlayerActionBeHidden(Building.Position, false);
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

            if (Building.TerrainID != null)
            {
                originalDiscovery = new Dictionary<string, BuildingTile>(context.BuildingKnowledge[Building.Position]);
                context.UpdateBuildingAfterCapture(Building, TeamsThatSawCapture);
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

            var actionHidden = controller.ShouldPlayerActionBeHidden(Building.Position, false);
            controller.Map.TryGetDrawableBuilding(Building.Position, out var capturingBuilding);

            if (controller.Map.TryGetDrawableUnit(Building.Position, out var capturingUnit))
            {
                if (!actionHidden)
                    capturingUnit.FadeTo(0.5f, 250, Easing.OutCubic);

                if (MoveUnit == null && (controller.ShowAnimationsWhenUnitsHidden.Value || !actionHidden))
                {
                    var anim = controller.Map.PlaySelectionAnimation(capturingUnit);
                    yield return ReplayWait.WaitForTransformable(anim);
                }

                if (!actionHidden)
                    capturingUnit.CanMove.Value = false;
            }
            else if (capturingBuilding != null && (controller.ShowAnimationsWhenUnitsHidden.Value || !actionHidden))
            {
                var anim = controller.Map.PlaySelectionAnimation(capturingBuilding);
                yield return ReplayWait.WaitForTransformable(anim);
            }

            controller.Map.UpdateBuilding(Building, false); //This will set the unit above to be capturing
            //If the building changes, we no longer have the right building so get it again.
            controller.Map.TryGetDrawableBuilding(Building.Position, out capturingBuilding);

            if (TeamsThatSawCapture != null && TeamsThatSawCapture.Count > 0 && controller.Map.TryGetDrawableBuilding(Building.Position, out var building))
            {
                foreach (var team in TeamsThatSawCapture)
                    building.TeamToTile[team] = building.BuildingTile;
            }

            if (IncomeChanges != null)
            {
                foreach (var incomeChange in IncomeChanges)
                    controller.Players[incomeChange.Key].PropertyValue.Value = incomeChange.Value;
            }

            if (capturingBuilding != null && (!actionHidden || controller.ShowAnimationsWhenUnitsHidden.Value))
            {
                shakeBuilding(capturingBuilding, actionHidden);

                if (MoveUnit != null && actionHidden)
                {
                    var anim = controller.Map.PlaySelectionAnimation(capturingBuilding);
                    yield return ReplayWait.WaitForTransformable(anim);
                }

                yield return ReplayWait.WaitForTransformable(capturingBuilding);

                if (capturingUnit != null && !actionHidden)
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

        private void shakeBuilding(DrawableBuilding capturingBuilding, bool subtler)
        {
            if (Building.TerrainID.HasValue && Building.TerrainID != originalBuilding.TerrainID)
            {
                capturingBuilding.MoveToOffset(new Vector2(subtler ? 1.5f : 3, 0), 45)
                                 .Then().MoveToOffset(new Vector2(subtler ? -3 : -6, 0), 90)
                                 .Then().MoveToOffset(new Vector2(subtler ? 1.5f : 3, 0), 45)
                                 .Then().ScaleTo(new Vector2(subtler ? 1.1f : 1.25f), 200, Easing.InOutSine).MoveToOffset(new Vector2(subtler ? -1 : -2, subtler ? -2 : -4), 200, Easing.InOutSine)
                                 .Then().ScaleTo(new Vector2(1f), 200, Easing.InOutSine).MoveToOffset(new Vector2(subtler ? 1 : 2, subtler ? 2 : 4), 200, Easing.InOutSine);
            }
            else
            {
                capturingBuilding.MoveToOffset(new Vector2(subtler ? 1.5f : 3, 0), 45)
                                 .Then().MoveToOffset(new Vector2(subtler ? -3 : -6, 0), 90)
                                 .Then().MoveToOffset(new Vector2(subtler ? 3 : 6, 0), 45)
                                 .Then().MoveToOffset(new Vector2(subtler ? -3 : -6, 0), 90)
                                 .Then().MoveToOffset(new Vector2(subtler ? 1.5f : 3, 0), 45);
            }
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Capture Action.");
            controller.Map.UpdateBuilding(originalBuilding, true);

            if (originalDiscovery != null)
            {
                if (controller.Map.TryGetDrawableBuilding(Building.Position, out var drawableBuilding))
                    drawableBuilding.TeamToTile.SetTo(originalDiscovery);
            }

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
