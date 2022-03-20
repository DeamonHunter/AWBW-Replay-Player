using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneDeleteUnitAction : BaseActionsTestScene
    {
        private static Vector2I unitPosition = new Vector2I(2, 2);

        [Test]
        public void TestCreateUnit()
        {
            AddStep("Setup", createTest);
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddAssert("Unit was deleted", () => !HasUnit(0));
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit was created", () => HasUnit(0));
        }

        private void createTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var createdUnit = CreateBasicReplayUnit(0, 1, "Infantry", unitPosition);
            turn.ReplayUnit.Add(createdUnit.ID, createdUnit);

            var createUnitAction = new DeleteUnitAction
            {
                DeletedUnitId = createdUnit.ID
            };
            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
            ReplayController.AllowRewinding = true;
        }
    }
}
