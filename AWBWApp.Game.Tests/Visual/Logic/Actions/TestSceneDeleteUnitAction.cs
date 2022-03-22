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
            AddAssert("Unit Value is 0", () => ReplayController.Players[0].UnitValue.Value == 0);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(originalUnit.ID, originalUnit));
            AddAssert("Unit Value is 1000", () => ReplayController.Players[0].UnitValue.Value == 1000);
        }

        [Test]
        public void TestMoveThenDeleteUnit()
        {
            AddStep("Setup", () => deleteTest(true));
            AddStep("Delete Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit was deleted", () => !HasUnit(originalUnit.ID));
            AddAssert("Unit Value is 0", () => ReplayController.Players[0].UnitValue.Value == 0);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(originalUnit.ID, originalUnit));
            AddAssert("Unit Value is 1000", () => ReplayController.Players[0].UnitValue.Value == 1000);
        }

        private void deleteTest(bool move)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            originalUnit = CreateBasicReplayUnit(0, 0, "Infantry", unitPosition);
            turn.ReplayUnit.Add(originalUnit.ID, originalUnit);

            var deleteUnitAction = new DeleteUnitAction
            {
                DeletedUnitId = originalUnit.ID
            };

            if (move)
            {
                var movedUnit = originalUnit.Clone();
                movedUnit.Position = new Vector2I(2, 3);

                deleteUnitAction.MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Unit = movedUnit,
                    Path = new[]
                    {
                        new UnitPosition(new Vector2I(2, 2)),
                        new UnitPosition(new Vector2I(2, 3)),
                    }
                };
            }

            turn.Actions.Add(deleteUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }
    }
}
