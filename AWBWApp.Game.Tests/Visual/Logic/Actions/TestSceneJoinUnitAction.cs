using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneJoinUnitAction : BaseActionsTestScene
    {
        private ReplayUnit unitA;
        private ReplayUnit unitB;

        private ReplayUnit joinedUnit;

        [Test]
        public void TestCreateUnit()
        {
            AddStep("Setup", createTest);
            AddStep("Join Unit", ReplayController.GoToNextAction);
            AddUntilStep("Wait for Unit A to disappear", () => !HasUnit(unitA.ID));
            AddAssert("Check Joined Unit is correct", () => DoesUnitMatchData(unitB.ID, joinedUnit));
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Both units are back to original stats", () => DoesUnitMatchData(unitA.ID, unitA) && DoesUnitMatchData(unitB.ID, unitB));
        }

        private void createTest()
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            unitA = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(1, 2));
            unitA.HitPoints = 5;
            turn.ReplayUnit.Add(unitA.ID, unitA);

            unitB = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(2, 2));
            unitB.HitPoints = 5;
            turn.ReplayUnit.Add(unitB.ID, unitB);

            joinedUnit = unitB.Clone();
            joinedUnit.HitPoints = 10;

            var movedUnit = unitA.Clone();
            movedUnit.Position = new Vector2I(2, 2);
            var createUnitAction = new JoinUnitAction
            {
                MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Path = new[]
                    {
                        new UnitPosition(new Vector2I(1, 2)),
                        new UnitPosition(new Vector2I(2, 2))
                    },
                    Unit = movedUnit
                },
                JoiningUnitId = unitA.ID,
                JoinedUnit = joinedUnit,
            };

            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
            ReplayController.AllowRewinding = true;
        }
    }
}
