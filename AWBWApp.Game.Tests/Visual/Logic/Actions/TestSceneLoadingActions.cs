using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public partial class TestSceneLoadingActions : BaseActionsTestScene
    {
        private static Vector2I unitPosition = new Vector2I(2, 2);

        [Test]
        public void TestLoadThenUnload()
        {
            AddStep("Setup", () => loadAndUnloadTest());
            AddStep("Load Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is Loaded", () => DoesUnitPassTest(0, x => x.BeingCarried.Value));
            AddStep("Unload Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is not Loaded", () => DoesUnitPassTest(0, x => !x.BeingCarried.Value));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Unit is Loaded", () => DoesUnitPassTest(0, x => x.BeingCarried.Value));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Unit is not Loaded", () => DoesUnitPassTest(0, x => !x.BeingCarried.Value));
        }

        [Test]
        public void TestLoadThenMoveThenUnload()
        {
            AddStep("Setup", () => loadMoveAndUnloadTest());
            AddStep("Load Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is Loaded", () => DoesUnitPassTest(0, x => x.BeingCarried.Value));
            AddStep("Move Unit Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is Loaded", () => DoesUnitPassTest(0, x => x.BeingCarried.Value));
            AddStep("Unload Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit is not Loaded", () => DoesUnitPassTest(0, x => !x.BeingCarried.Value));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Unit is Loaded", () => DoesUnitPassTest(0, x => x.BeingCarried.Value));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Unit is Loaded", () => DoesUnitPassTest(0, x => x.BeingCarried.Value));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Unit is not Loaded", () => DoesUnitPassTest(0, x => !x.BeingCarried.Value));
        }

        private void loadAndUnloadTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var loadingUnit = CreateBasicReplayUnit(0, 0, "Infantry", unitPosition - new Vector2I(1, 0));
            turn.ReplayUnit.Add(loadingUnit.ID, loadingUnit);
            var transportUnit = CreateBasicReplayUnit(1, 0, "APC", unitPosition);
            turn.ReplayUnit.Add(transportUnit.ID, transportUnit);

            var loadUnitAction = new LoadUnitAction
            {
                LoadedID = loadingUnit.ID,
                TransportID = transportUnit.ID,
                MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Unit = loadingUnit.Clone(),
                    Path = new[]
                    {
                        new UnitPosition(unitPosition - new Vector2I(1, 0)),
                        new UnitPosition(unitPosition)
                    }
                }
            };

            loadUnitAction.MoveUnit.Unit.Position = unitPosition;
            turn.Actions.Add(loadUnitAction);

            var unloadUnitAction = new UnloadUnitAction
            {
                UnloadedUnit = loadingUnit.Clone(),
                TransportID = transportUnit.ID
            };

            unloadUnitAction.UnloadedUnit.Position = unitPosition + new Vector2I(1, 0);

            turn.Actions.Add(unloadUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }

        private void loadMoveAndUnloadTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var loadingUnit = CreateBasicReplayUnit(0, 0, "Infantry", unitPosition - new Vector2I(1, 0));
            turn.ReplayUnit.Add(loadingUnit.ID, loadingUnit);
            var transportUnit = CreateBasicReplayUnit(1, 0, "APC", unitPosition);
            turn.ReplayUnit.Add(transportUnit.ID, transportUnit);

            var loadUnitAction = new LoadUnitAction
            {
                LoadedID = loadingUnit.ID,
                TransportID = transportUnit.ID,
                MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Unit = loadingUnit.Clone(),
                    Path = new[]
                    {
                        new UnitPosition(unitPosition - new Vector2I(1, 0)),
                        new UnitPosition(unitPosition)
                    }
                }
            };

            loadUnitAction.MoveUnit.Unit.Position = unitPosition;
            turn.Actions.Add(loadUnitAction);

            var moveAction = new MoveUnitAction()
            {
                Unit = transportUnit.Clone(),
                Path = new[]
                {
                    new UnitPosition() { X = transportUnit.Position.Value.X, Y = transportUnit.Position.Value.Y },
                    new UnitPosition { X = transportUnit.Position.Value.X, Y = transportUnit.Position.Value.Y - 1 },
                },
                Distance = 1
            };
            moveAction.Unit.Position = new Vector2I(transportUnit.Position.Value.X, transportUnit.Position.Value.Y - 1);
            turn.Actions.Add(moveAction);

            var unloadUnitAction = new UnloadUnitAction
            {
                UnloadedUnit = loadingUnit.Clone(),
                TransportID = transportUnit.ID
            };

            unloadUnitAction.UnloadedUnit.Position = unitPosition + new Vector2I(1, -1);

            turn.Actions.Add(unloadUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }
    }
}
