using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneHidingActions : BaseActionsTestScene
    {
        private static Vector2I unitPosition = new Vector2I(2, 2);

        [Test]
        public void TestHideThenUnhide()
        {
            AddStep("Setup", () => hideAndUnhideTest(false));
            AddStep("Hide Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is Hidden", () => DoesUnitPassTest(0, x => x.Dived.Value));
            AddStep("Unhide Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is not Hidden", () => DoesUnitPassTest(0, x => !x.Dived.Value));
            AddStep("Undo", ReplayController.UndoAction);
            AddUntilStep("Unit is Hidden", () => DoesUnitPassTest(0, x => x.Dived.Value));
            AddStep("Undo", ReplayController.UndoAction);
            AddUntilStep("Unit is not Hidden", () => DoesUnitPassTest(0, x => !x.Dived.Value));
        }

        [Test]
        public void TestHideThenUnhideWithMovement()
        {
            AddStep("Setup", () => hideAndUnhideTest(true));
            AddStep("Hide Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is Hidden", () => DoesUnitPassTest(0, x => x.Dived.Value && x.MapPosition == unitPosition));
            AddStep("Unhide Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is not Hidden", () => DoesUnitPassTest(0, x => !x.Dived.Value && x.MapPosition == unitPosition + new Vector2I(1, 0)));
            AddStep("Undo", ReplayController.UndoAction);
            AddUntilStep("Unit is Hidden", () => DoesUnitPassTest(0, x => x.Dived.Value && x.MapPosition == unitPosition));
            AddStep("Undo", ReplayController.UndoAction);
            AddUntilStep("Unit is not Hidden", () => DoesUnitPassTest(0, x => !x.Dived.Value && x.MapPosition == unitPosition - new Vector2I(1, 0)));
        }

        private void hideAndUnhideTest(bool move)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var unit = CreateBasicReplayUnit(0, 1, "Stealth", move ? unitPosition - new Vector2I(1, 0) : unitPosition);
            turn.ReplayUnit.Add(unit.ID, unit);

            var hideUnitAction = new HideUnitAction
            {
                HidingUnitID = unit.ID
            };

            var unhideUnitAction = new UnhideUnitAction
            {
                RevealingUnit = unit.Clone()
            };
            unhideUnitAction.RevealingUnit.Position = move ? unitPosition + new Vector2I(1, 0) : unitPosition;

            if (move)
            {
                hideUnitAction.MoveUnit = new MoveUnitAction
                {
                    Distance = 1,
                    Path = new[]
                    {
                        new UnitPosition(unitPosition - new Vector2I(1, 0)),
                        new UnitPosition(unitPosition),
                    },
                    Unit = unit.Clone()
                };
                hideUnitAction.MoveUnit.Unit.Position = unitPosition;

                unhideUnitAction.MoveUnit = new MoveUnitAction
                {
                    Distance = 1,
                    Path = new[]
                    {
                        new UnitPosition(unitPosition),
                        new UnitPosition(unitPosition + new Vector2I(1, 0)),
                    },
                    Unit = unit.Clone()
                };
                unhideUnitAction.MoveUnit.Unit.Position = unitPosition + new Vector2I(1, 0);
            }

            turn.Actions.Add(hideUnitAction);
            turn.Actions.Add(unhideUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
            ReplayController.AllowRewinding = true;
        }
    }
}
