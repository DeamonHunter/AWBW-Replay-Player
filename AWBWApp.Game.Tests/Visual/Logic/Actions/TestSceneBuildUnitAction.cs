using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneBuildUnitAction : BaseActionsTestScene
    {
        private static Vector2I unitPosition = new Vector2I(2, 2);
        private ReplayUnit createdUnit;

        [Test]
        public void TestCreateUnit()
        {
            AddStep("Setup", createTest);
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddAssert("Unit was created", () => HasUnit(0));
            AddAssert("Building is done", () => ReplayController.Map.TryGetDrawableBuilding(unitPosition, out var building) && building.HasDoneAction.Value);
            AddAssert("Funds is 0", () => ReplayController.Players[0].Funds.Value == 0);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit doesn't exist", () => !HasUnit(0));
            AddAssert("Building is not done", () => ReplayController.Map.TryGetDrawableBuilding(unitPosition, out var building) && !building.HasDoneAction.Value);
            AddAssert("Funds is 1000", () => ReplayController.Players[0].Funds.Value == 1000);
        }

        private void createTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            turn.Players[0].Funds = 1000;
            replayData.TurnData.Add(turn);

            var building = CreateBasicReplayBuilding(0, unitPosition, 39);
            turn.Buildings.Add(building.Position, building);

            createdUnit = CreateBasicReplayUnit(0, 0, "Infantry", unitPosition);
            createdUnit.TimesMoved = 1;

            var createUnitAction = new BuildUnitAction
            {
                NewUnit = createdUnit
            };
            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
            ReplayController.AllowRewinding = true;
        }
    }
}
