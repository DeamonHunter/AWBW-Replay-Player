using System;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public partial class TestSceneMoveAction : BaseActionsTestScene
    {
        private AWBWConfigManager config;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.Cache(config = new AWBWConfigManager(LocalStorage));
            return dependencies;
        }

        [TestCase(1)]
        [TestCase(3)]
        public void TestStraightLine(int spaces)
        {
            AddStep("Setup", () => moveTestBasic(spaces));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(1, spaces));
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, spaces));
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(3, spaces));
            AddStep("Attack Down", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(4, spaces));

            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(3, spaces));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, spaces));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(1, spaces));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(0, 0));
        }

        [Test]
        public void TestTrapped()
        {
            AddStep("Setup", moveTestTrap);
            AddStep("Set Vision to player 0", () => ReplayController.CurrentFogView.Value = 0L);
            AddStep("Activate Seeing In Fog", () => config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, true));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(4, 1));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(1, 1));
            AddStep("Deactivate Seeing In Fog", () => config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, false));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(4, 1));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(1, 1));
            AddStep("Set Vision to player 1", () => ReplayController.CurrentFogView.Value = 1L);
            AddStep("Activate Seeing In Fog", () => config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, true));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(4, 1));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(1, 1));
            AddStep("Deactivate Seeing In Fog", () => config.SetValue(AWBWSetting.ReplayOnlyShownKnownInfo, false));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(4, 1));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(1, 1));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void TestStraightDiagonal(int spaces)
        {
            AddStep("Setup", () => moveTestCorner(spaces));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, spaces));
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(4, spaces));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, spaces));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Undone Correctly", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(0, 0));
        }

        private void moveTestBasic(int spaces)
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);
            ReplayController.CurrentFogView.Value = string.Empty;

            var mapSize = spaces + 1;

            var movingUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(0, 0));

            turn.ReplayUnit.Add(movingUnit.ID, movingUnit);

            for (int i = 1; i < 5; i++)
            {
                var nextPosition = GetPositionForIteration(i, spaces);
                var previousPosition = GetPositionForIteration(i - 1, spaces);

                var moveUnitAction = new MoveUnitAction
                {
                    Unit = new ReplayUnit { ID = movingUnit.ID, Position = nextPosition },
                    Path = createPath(previousPosition, nextPosition)
                };
                moveUnitAction.Distance = moveUnitAction.Path.Length;
                moveUnitAction.Trapped = false;

                turn.Actions.Add(moveUnitAction);
            }

            ReplayController.LoadReplay(replayData, CreateBasicMap(mapSize, mapSize));
        }

        private UnitPosition[] createPath(Vector2I previousPosition, Vector2I nextPosition)
        {
            var diff = nextPosition - previousPosition;

            var path = new UnitPosition[Math.Abs(diff.X) + Math.Abs(diff.Y) + 1];
            path[0] = new UnitPosition { UnitVisible = true, X = previousPosition.X, Y = previousPosition.Y };

            var pos = previousPosition;
            var pathIdx = 1;

            if (diff.X > 0)
            {
                for (pos.X += 1; pos.X < nextPosition.X; pos = new Vector2I(pos.X + 1, pos.Y))
                    path[pathIdx++] = new UnitPosition(pos);
                path[pathIdx++] = new UnitPosition(pos);
            }

            if (diff.Y > 0)
            {
                for (pos.Y += 1; pos.Y < nextPosition.Y; pos = new Vector2I(pos.X, pos.Y + 1))
                    path[pathIdx++] = new UnitPosition(pos);
                path[pathIdx++] = new UnitPosition(pos);
            }

            if (diff.X < 0)
            {
                for (pos.X -= 1; pos.X > nextPosition.X; pos = new Vector2I(pos.X - 1, pos.Y))
                    path[pathIdx++] = new UnitPosition(pos);
                path[pathIdx++] = new UnitPosition(pos);
            }

            if (diff.Y < 0)
            {
                for (pos.Y -= 1; pos.Y > nextPosition.Y; pos = new Vector2I(pos.X, pos.Y - 1))
                    path[pathIdx++] = new UnitPosition(pos);
                path[pathIdx] = new UnitPosition(pos);
            }

            return path;
        }

        private void moveTestTrap()
        {
            var replayData = CreateBasicReplayData(2);
            replayData.ReplayInfo.Fog = true;
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);
            ReplayController.CurrentFogView.Value = string.Empty;

            var movingUnit = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(1, 1));
            turn.ReplayUnit.Add(movingUnit.ID, movingUnit);

            var stillUnit = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(5, 1));
            turn.ReplayUnit.Add(stillUnit.ID, stillUnit);

            var moveUnitAction = new MoveUnitAction
            {
                Unit = new ReplayUnit { ID = movingUnit.ID, Position = new Vector2I(4, 1) },
                Path = new[]
                {
                    new UnitPosition(new Vector2I(1, 1)),
                    new UnitPosition(new Vector2I(2, 1)),
                    new UnitPosition(new Vector2I(3, 1)),
                    new UnitPosition(new Vector2I(4, 1)),
                }
            };
            moveUnitAction.Distance = moveUnitAction.Path.Length;
            moveUnitAction.Trapped = true;

            turn.Actions.Add(moveUnitAction);

            var map = CreateBasicMap(7, 3);
            map.Ids[1 * 7 + 1] = 15;
            map.Ids[1 * 7 + 2] = 15;
            map.Ids[1 * 7 + 3] = 15;
            map.Ids[1 * 7 + 4] = 15;
            map.Ids[1 * 7 + 5] = 3;

            ReplayController.LoadReplay(replayData, map);
        }

        private void moveTestCorner(int spaces)
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var mapSize = spaces + 1;

            var movingUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(0, 0));

            turn.ReplayUnit.Add(movingUnit.ID, movingUnit);

            for (int i = 0; i < 2; i++)
            {
                var nextPosition = GetPositionForIteration(i * 2 + 2, spaces);
                var previousPosition = GetPositionForIteration((i - 1) * 2 + 2, spaces);

                var moveUnitAction = new MoveUnitAction
                {
                    Unit = new ReplayUnit { ID = movingUnit.ID, Position = nextPosition },
                    Path = createPath(previousPosition, nextPosition),
                    Trapped = false
                };
                moveUnitAction.Distance = moveUnitAction.Path.Length;

                turn.Actions.Add(moveUnitAction);
            }

            ReplayController.LoadReplay(replayData, CreateBasicMap(mapSize, mapSize));
        }

        public Vector2I GetPositionForIteration(int i, int spaces) =>
            i switch
            {
                0 => new Vector2I(0, 0),
                1 => new Vector2I(spaces, 0),
                2 => new Vector2I(spaces, spaces),
                3 => new Vector2I(0, spaces),
                4 => new Vector2I(0, 0),
                _ => throw new InvalidOperationException($"{nameof(GetPositionForIteration)} only accepts values between 0-4 (inclusive)")
            };
    }
}
