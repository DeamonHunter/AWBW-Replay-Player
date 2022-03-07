using System;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneMoveAction : BaseActionsTestScene
    {
        [Test]
        public void TestStraightLine1Space()
        {
            AddStep("Setup", () => MoveTestBasic(1));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(1, 1));
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, 1));
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(3, 1));
            AddStep("Attack Down", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(4, 1));
        }

        [Test]
        public void TestStraightLine3Spaces()
        {
            AddLabel("Straight Line - 3 spaces");
            AddStep("Setup", () => MoveTestBasic(3));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(1, 3));
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, 3));
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(3, 3));
            AddStep("Attack Down", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(4, 3));
        }

        [Test]
        public void TestTrapped()
        {
            AddStep("Setup", () => MoveTestTrap());
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == new Vector2I(4, 1));
        }

        [Test]
        public void TestStraightDiagonal1Spaces()
        {
            AddLabel("Move Diagonal - 1 spaces");
            AddStep("Setup", () => MoveTestCorner(1));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, 1));
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(4, 1));
        }

        [Test]
        public void TestStraightDiagonal3Spaces()
        {
            AddLabel("Move Diagonal - 3 spaces");
            AddStep("Setup", () => MoveTestCorner(3));
            AddStep("Move Right", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(2, 3));
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddUntilStep("Wait for unit to move.", () => ReplayController.Map.GetDrawableUnit(0).MapPosition == GetPositionForIteration(4, 3));
        }

        private void MoveTestBasic(int spaces)
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var mapSize = spaces + 1;

            var movingUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(0, 0));

            turn.ReplayUnit.Add(movingUnit.ID, movingUnit);

            for (int i = 1; i < 5; i++)
            {
                var nextPosition = GetPositionForIteration(i, spaces);
                var previousPosition = GetPositionForIteration(i - 1, spaces);

                var moveUnitAction = new MoveUnitAction();
                moveUnitAction.Unit = new ReplayUnit { ID = movingUnit.ID, Position = nextPosition };
                moveUnitAction.Path = CreatePath(previousPosition, nextPosition);
                moveUnitAction.Distance = moveUnitAction.Path.Length;
                moveUnitAction.Trapped = false;

                turn.Actions.Add(moveUnitAction);
            }

            ReplayController.LoadReplay(replayData, CreateBasicMap(mapSize, mapSize));
        }

        private UnitPosition[] CreatePath(Vector2I previousPosition, Vector2I nextPosition)
        {
            var diff = nextPosition - previousPosition;

            var path = new UnitPosition[Math.Abs(diff.X) + Math.Abs(diff.Y) + 1];
            path[0] = new UnitPosition { UnitVisible = true, X = previousPosition.X, Y = previousPosition.Y };

            var pos = previousPosition;
            var pathIdx = 1;

            if (diff.X > 0)
            {
                for (pos.X += 1; pos.X < nextPosition.X; pos = new Vector2I(pos.X + 1, pos.Y))
                    path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
                path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
            }

            if (diff.Y > 0)
            {
                for (pos.Y += 1; pos.Y < nextPosition.Y; pos = new Vector2I(pos.X, pos.Y + 1))
                    path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
                path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
            }

            if (diff.X < 0)
            {
                for (pos.X -= 1; pos.X > nextPosition.X; pos = new Vector2I(pos.X - 1, pos.Y))
                    path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
                path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
            }

            if (diff.Y < 0)
            {
                for (pos.Y -= 1; pos.Y > nextPosition.Y; pos = new Vector2I(pos.X, pos.Y - 1))
                    path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
                path[pathIdx++] = new UnitPosition { UnitVisible = true, X = pos.X, Y = pos.Y };
            }

            return path;
        }

        private void MoveTestTrap()
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var movingUnit = CreateBasicReplayUnit(0, 0, "Recon", new Vector2I(1, 1));
            turn.ReplayUnit.Add(movingUnit.ID, movingUnit);

            var stillUnit = CreateBasicReplayUnit(1, 1, "Recon", new Vector2I(5, 1));
            turn.ReplayUnit.Add(stillUnit.ID, stillUnit);

            for (int i = 1; i < 5; i++)
            {
                var moveUnitAction = new MoveUnitAction();
                moveUnitAction.Unit = new ReplayUnit { ID = movingUnit.ID, Position = new Vector2I(4, 1) };
                moveUnitAction.Path = new[]
                {
                    new UnitPosition { X = 1, Y = 1, UnitVisible = true },
                    new UnitPosition { X = 2, Y = 1, UnitVisible = true },
                    new UnitPosition { X = 3, Y = 1, UnitVisible = true },
                    new UnitPosition { X = 4, Y = 1, UnitVisible = true },
                };
                moveUnitAction.Distance = moveUnitAction.Path.Length;
                moveUnitAction.Trapped = true;

                turn.Actions.Add(moveUnitAction);
            }

            var map = CreateBasicMap(7, 3);
            map.Ids[1 * 7 + 1] = 15;
            map.Ids[1 * 7 + 2] = 15;
            map.Ids[1 * 7 + 3] = 15;
            map.Ids[1 * 7 + 4] = 15;
            map.Ids[1 * 7 + 5] = 3;

            ReplayController.LoadReplay(replayData, map);
        }

        private void MoveTestCorner(int spaces)
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

                var moveUnitAction = new MoveUnitAction();
                moveUnitAction.Unit = new ReplayUnit { ID = movingUnit.ID, Position = nextPosition };

                moveUnitAction.Path = CreatePath(previousPosition, nextPosition);
                moveUnitAction.Distance = moveUnitAction.Path.Length;
                moveUnitAction.Trapped = false;

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
