using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    //Todo: Add elimination testing
    [TestFixture]
    public class TestSceneCaptureBuildingAction : BaseActionsTestScene
    {
        private static Vector2I buildingPosition = new Vector2I(2, 2);

        private const int neutral_city = 34;
        private const int orange_star_city = 38;
        private const int blue_moon_city = 43;

        [TestCase(true)]
        [TestCase(false)]
        public void TestStartCapturingBuilding(bool movement)
        {
            AddStep("Setup", () => captureTest(movement, false));
            AddStep("Start Capturing Building", ReplayController.GoToNextAction);
            AddUntilStep("Unit capturing and done move", () => DoesUnitPassTest(0, x => x.IsCapturing.Value && !x.CanMove.Value));
            AddAssert("Building HP is 10", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == neutral_city && building.CaptureHealth.Value == 10);
            AddAssert("Property Value is still 0", () => ReplayController.ActivePlayer.PropertyValue.Value == 0);
            AddStep("Finish Capturing Building", ReplayController.GoToNextAction);
            AddUntilStep("Building is correct", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == orange_star_city);
            AddAssert("Unit finished capturing and done move", () => DoesUnitPassTest(0, x => !x.IsCapturing.Value && !x.CanMove.Value));
            AddAssert("Player 0 owns building", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.OwnerID == 0);
            AddAssert("Property Value is 1000", () => ReplayController.ActivePlayer.PropertyValue.Value == 1000);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Player 0 no longer owns building", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.OwnerID == null);
            AddAssert("Building uncaptured and 10hp", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == neutral_city && building.CaptureHealth.Value == 10);
            AddAssert("Unit capturing", () => DoesUnitPassTest(0, x => x.IsCapturing.Value)); //Don't test can move as this is technically an illegal setup
            AddAssert("Property Value is 0", () => ReplayController.ActivePlayer.PropertyValue.Value == 0);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Building uncaptured and 20hp", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == neutral_city && building.CaptureHealth.Value == 20);
            AddAssert("Unit not capturing and can move", () => DoesUnitPassTest(0, x => !x.IsCapturing.Value && x.CanMove.Value));
        }

        [Test]
        public void TestStartCapturingOpponentBuilding()
        {
            AddStep("Setup", () => captureTest(false, true));
            AddStep("Start Capturing Building", ReplayController.GoToNextAction);
            AddUntilStep("Unit capturing and done move", () => DoesUnitPassTest(0, x => x.IsCapturing.Value && !x.CanMove.Value));
            AddAssert("Building HP is 10", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == blue_moon_city && building.CaptureHealth.Value == 10);
            AddAssert("Property Value is 0", () => ReplayController.ActivePlayer.PropertyValue.Value == 0);
            AddAssert("Opponent Value is 1000", () => ReplayController.Players[1].PropertyValue.Value == 1000);
            AddStep("Finish Capturing Building", ReplayController.GoToNextAction);
            AddUntilStep("Building is correct", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == orange_star_city);
            AddAssert("Unit finished capturing and done move", () => DoesUnitPassTest(0, x => !x.IsCapturing.Value && !x.CanMove.Value));
            AddAssert("Player 0 owns building", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.OwnerID == 0);
            AddAssert("Property Value is 1000", () => ReplayController.ActivePlayer.PropertyValue.Value == 1000);
            AddAssert("Opponent Value is 0", () => ReplayController.Players[1].PropertyValue.Value == 0);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Player 1 owns building", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.OwnerID == 1);
            AddAssert("Building uncaptured and 10hp", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == blue_moon_city && building.CaptureHealth.Value == 10);
            AddAssert("Unit capturing", () => DoesUnitPassTest(0, x => x.IsCapturing.Value)); //Don't test can move as this is technically an illegal setup
            AddAssert("Property Value is 0", () => ReplayController.ActivePlayer.PropertyValue.Value == 0);
            AddAssert("Opponent Value is 1000", () => ReplayController.Players[1].PropertyValue.Value == 1000);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Building uncaptured and 20hp", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out var building) && building.BuildingTile.AWBWID == blue_moon_city && building.CaptureHealth.Value == 20);
            AddAssert("Unit not capturing and can move", () => DoesUnitPassTest(0, x => !x.IsCapturing.Value && x.CanMove.Value));
        }

        [Test]
        public void TestUnitIsCapturingOnNextTurn()
        {
            AddStep("Setup", () => movementTest(false));
            AddStep("Go To Next Turn", () => ReplayController.GoToNextTurn());
            AddAssert("Unit Not Capturing", () => DoesUnitPassTest(0, x => !x.IsCapturing.Value));
        }

        [Test]
        public void TestUnitIsCapturingOnPrevTurn()
        {
            AddStep("Setup", () => movementTest(true));
            AddStep("Go To Next Turn", () => ReplayController.GoToNextTurn());
            AddStep("Go To Previous Turn", () => ReplayController.GoToPreviousTurn());
            AddAssert("Unit Not Capturing", () => DoesUnitPassTest(0, x => !x.IsCapturing.Value));
        }

        private void captureTest(bool move, bool opponentBuilding)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var building = CreateBasicReplayBuilding(0, buildingPosition, opponentBuilding ? blue_moon_city : neutral_city);
            turn.Buildings.Add(building.Position, building);

            var capturingUnit = CreateBasicReplayUnit(0, 0, "Infantry", move ? buildingPosition - new Vector2I(1, 0) : buildingPosition);
            turn.ReplayUnit.Add(capturingUnit.ID, capturingUnit);

            var captureAction = new CaptureBuildingAction
            {
                Building = new ReplayBuilding
                {
                    ID = 0,
                    Capture = 10,
                    LastCapture = 20,
                    Position = buildingPosition,
                    Team = null,
                    TerrainID = opponentBuilding ? blue_moon_city : neutral_city
                },
            };

            if (move)
            {
                captureAction.MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Path = new[]
                    {
                        new UnitPosition(buildingPosition - new Vector2I(1, 0)),
                        new UnitPosition(buildingPosition)
                    },
                    Unit = capturingUnit.Clone()
                };
                captureAction.MoveUnit.Unit.Position = buildingPosition;
            }

            turn.Actions.Add(captureAction);

            captureAction = new CaptureBuildingAction
            {
                Building = new ReplayBuilding
                {
                    ID = 0,
                    Capture = 0,
                    LastCapture = 10,
                    Position = buildingPosition,
                    Team = null,
                    TerrainID = orange_star_city
                },
                IncomeChanges = new Dictionary<long, int>
                {
                    { 0, replayData.ReplayInfo.FundsPerBuilding }
                }
            };

            if (opponentBuilding)
                captureAction.IncomeChanges.Add(1, 0);

            turn.Actions.Add(captureAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }

        private void movementTest(bool previousTurn)
        {
            var replayData = CreateBasicReplayData(2);

            var turnA = CreateBasicTurnData(replayData);
            var turnB = CreateBasicTurnData(replayData);

            var replayUnit = CreateBasicReplayUnit(0, 0, "Infantry", buildingPosition);
            turnA.ReplayUnit.Add(replayUnit.ID, replayUnit.Clone());
            replayUnit.Position = buildingPosition - new Vector2I(1, 0);
            turnB.ReplayUnit.Add(replayUnit.ID, replayUnit);

            var replayBuilding = CreateBasicReplayBuilding(0, buildingPosition, neutral_city);
            turnA.Buildings.Add(replayBuilding.Position, replayBuilding.Clone());
            replayBuilding.Capture = 10;
            turnB.Buildings.Add(replayBuilding.Position, replayBuilding);

            if (previousTurn)
            {
                replayData.TurnData.Add(turnB);
                replayData.TurnData.Add(turnA);
            }
            else
            {
                replayData.TurnData.Add(turnA);
                replayData.TurnData.Add(turnB);
            }

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }
    }
}
