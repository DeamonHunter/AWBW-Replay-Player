using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Helpers;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneLaunchRocketAction : BaseActionsTestScene
    {
        private static Vector2I explodeCenter = new Vector2I(4, 4);
        private static Vector2I buildingPosition = new Vector2I(0, 4);

        private ReplayUnit launchingUnit;

        [Test]
        public void TestLaunch()
        {
            AddStep("Setup", () => launchTest(false, false));
            AddStep("Launch Rocket", ReplayController.GoToNextAction);
            AddUntilStep("Rocket Launched", () => !ReplayController.HasOngoingAction());
            AddAssert("No Missile", () => !ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out _));
            AddAssert("Opponent Unit Value is 17500", () => ReplayController.Players[1].UnitValue.Value == 17500);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(launchingUnit.ID, launchingUnit));
            AddAssert("Missile Exists", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out _));
            AddAssert("Opponent Unit Value is 24000", () => ReplayController.Players[1].UnitValue.Value == 25000);
        }

        [Test]
        public void TestMoveThenLaunch()
        {
            AddStep("Setup", () => launchTest(true, false));
            AddStep("Launch Rocket", ReplayController.GoToNextAction);
            AddUntilStep("Rocket Launched", () => !ReplayController.HasOngoingAction());
            AddAssert("No Missile", () => !ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out _));
            AddAssert("Opponent Unit Value is 16800", () => ReplayController.Players[1].UnitValue.Value == 17500);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(launchingUnit.ID, launchingUnit));
            AddAssert("Missile Exists", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out _));
            AddAssert("Opponent Unit Value is 24000", () => ReplayController.Players[1].UnitValue.Value == 25000);
        }

        [Test]
        public void TestLaunchCausingDeath()
        {
            AddStep("Setup", () => launchTest(false, true));
            AddStep("Launch Rocket", ReplayController.GoToNextAction);
            AddUntilStep("Rocket Launched", () => !ReplayController.HasOngoingAction());
            AddAssert("No Missile", () => !ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out _));
            AddAssert("Opponent Unit Value is 0", () => ReplayController.Players[1].UnitValue.Value == 0);
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(launchingUnit.ID, launchingUnit));
            AddAssert("Missile Exists", () => ReplayController.Map.TryGetDrawableBuilding(buildingPosition, out _));
            AddAssert("Opponent Unit Value is 3900", () => ReplayController.Players[1].UnitValue.Value == 7500);
        }

        private void launchTest(bool move, bool opponentLowHp)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var launchBuilding = CreateBasicReplayBuilding(0, buildingPosition, 111);
            turn.Buildings.Add(launchBuilding.Position, launchBuilding);

            var unitIdx = 1;

            for (int i = 0; i <= 3; i++)
            {
                foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(explodeCenter, i))
                {
                    var unit = CreateBasicReplayUnit(unitIdx++, 1, "Infantry", tile);
                    unit.HitPoints = opponentLowHp ? 3 : 10;
                    turn.ReplayUnit.Add(unit.ID, unit);
                }
            }

            launchingUnit = CreateBasicReplayUnit(0, 0, "Infantry", move ? buildingPosition - new Vector2I(0, 1) : buildingPosition);
            turn.ReplayUnit.Add(launchingUnit.ID, launchingUnit);

            var explodeUnitAction = new LaunchRocketAction
            {
                SiloPosition = buildingPosition,
                TargetPosition = explodeCenter,
                HPChange = -3
            };

            if (move)
            {
                var movedUnit = launchingUnit.Clone();
                movedUnit.Position = buildingPosition;

                explodeUnitAction.MoveUnit = new MoveUnitAction()
                {
                    Distance = 1,
                    Unit = movedUnit,
                    Path = new[]
                    {
                        new UnitPosition(buildingPosition - new Vector2I(0, 1)),
                        new UnitPosition(buildingPosition)
                    }
                };
            }

            turn.Actions.Add(explodeUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(explodeCenter.X * 2 + 1, explodeCenter.Y * 2 + 1));
        }
    }
}
