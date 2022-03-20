using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneDeleteUnitAction : BaseActionsTestScene
    {
        private static Vector2I unitPosition = new Vector2I(2, 2);

        private ReplayUnit originalUnit;

        [Test]
        public void TestDeleteUnit()
        {
            AddStep("Setup", () => deleteTest(false));
            AddStep("Delete Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit was deleted", () => !HasUnit(originalUnit.ID));
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(originalUnit.ID, originalUnit));
        }

        [Test]
        public void TestMoveThenDeleteUnit()
        {
            AddStep("Setup", () => deleteTest(true));
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit was deleted", () => !HasUnit(originalUnit.ID));
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(originalUnit.ID, originalUnit));
        }

        private void deleteTest(bool move)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            originalUnit = CreateBasicReplayUnit(0, 1, "Infantry", unitPosition);
            turn.ReplayUnit.Add(originalUnit.ID, originalUnit);

            var createUnitAction = new DeleteUnitAction
            {
                DeletedUnitId = originalUnit.ID
            };

            if (move)
            {
                var movedUnit = originalUnit.Clone();
                movedUnit.Position = new Vector2I(2, 3);

                createUnitAction.MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Unit = movedUnit,
                    Path = new[]
                    {
                        new UnitPosition { X = 2, Y = 2 },
                        new UnitPosition { X = 2, Y = 3 },
                    }
                };
            }

            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
            ReplayController.AllowRewinding = true;
        }
    }
}
