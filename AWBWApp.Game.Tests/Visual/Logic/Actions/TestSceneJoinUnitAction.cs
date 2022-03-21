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
        public void TestJoinUnit()
        {
            AddStep("Setup", () => joinTest(false));
            AddStep("Join Unit", ReplayController.GoToNextAction);
            AddUntilStep("Wait for Unit A to disappear", () => !HasUnit(unitA.ID));
            AddAssert("Check Joined Unit is correct", () => DoesUnitMatchData(unitB.ID, joinedUnit));
            AddAssert("Gained no funds", () => ReplayController.ActivePlayer.Funds.Value == 1000);
            AddAssert("Unit Value is 1000", () => ReplayController.ActivePlayer.UnitValue.Value == 1000);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Both units are back to original stats", () => DoesUnitMatchData(unitA.ID, unitA) && DoesUnitMatchData(unitB.ID, unitB));
            AddAssert("Unit Value is 1000", () => ReplayController.ActivePlayer.UnitValue.Value == 1000);
        }

        [Test]
        public void TestJoinUnitOverflow()
        {
            AddStep("Setup", () => joinTest(true));
            AddStep("Join Unit", ReplayController.GoToNextAction);
            AddUntilStep("Wait for Unit A to disappear", () => !HasUnit(unitA.ID));
            AddAssert("Check Joined Unit is correct", () => DoesUnitMatchData(unitB.ID, joinedUnit));
            AddAssert("Gained 600 funds", () => ReplayController.ActivePlayer.Funds.Value == 1600);
            AddAssert("Unit Value is 1000", () => ReplayController.ActivePlayer.UnitValue.Value == 1000);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Both units are back to original stats", () => DoesUnitMatchData(unitA.ID, unitA) && DoesUnitMatchData(unitB.ID, unitB));
            AddAssert("Funds reverted", () => ReplayController.ActivePlayer.Funds.Value == 1000);
            AddAssert("Unit Value is 1600", () => ReplayController.ActivePlayer.UnitValue.Value == 1600);
        }

        private void joinTest(bool higherHP)
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);
            turn.Players[0].Funds = 1000;

            unitA = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(1, 2));
            unitA.HitPoints = higherHP ? 8 : 5;
            turn.ReplayUnit.Add(unitA.ID, unitA);

            unitB = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(2, 2));
            unitB.HitPoints = higherHP ? 8 : 5;
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
                FundsAfterJoin = higherHP ? 1600 : 1000
            };

            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
            ReplayController.AllowRewinding = true;
        }
    }
}
