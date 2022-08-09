using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneExplodeUnitAction : BaseActionsTestScene
    {
        private static Vector2I explodeCenter = new Vector2I(4, 4);

        private ReplayUnit explodingUnit;

        [Test]
        public void TestExplodeUnit()
        {
            AddStep("Setup", () => explodeTest(false, false));
            AddStep("Explode Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit was deleted", () => !HasUnit(explodingUnit.ID));
            AddAssert("Unit Value is 0", () => ReplayController.Players[0].UnitValue.Value == 0);
            AddAssert("Opponent Unit Value is 16800", () => ReplayController.Players[1].UnitValue.Value == 16800);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/7200", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 7200));
            AddUntilStep("Damage Stats: (0) 0/7200, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 7200, 0, 0));

            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(explodingUnit.ID, explodingUnit));
            AddAssert("Unit Value is 25000", () => ReplayController.Players[0].UnitValue.Value == 25000);
            AddAssert("Opponent Unit Value is 24000", () => ReplayController.Players[1].UnitValue.Value == 24000);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 0));
            AddUntilStep("Damage Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 0, 0, 0));
        }

        [Test]
        public void TestMoveThenExplodeUnit()
        {
            AddStep("Setup", () => explodeTest(true, false));
            AddStep("Explode Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit was explode", () => !HasUnit(explodingUnit.ID));
            AddAssert("Unit Value is 0", () => ReplayController.Players[0].UnitValue.Value == 0);
            AddAssert("Opponent Unit Value is 7700", () => ReplayController.Players[1].UnitValue.Value == 14700);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/6300", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 6300));
            AddUntilStep("Damage Stats: (0) 0/6300, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 6300, 0, 0));

            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(explodingUnit.ID, explodingUnit));
            AddAssert("Unit Value is 25000", () => ReplayController.Players[0].UnitValue.Value == 25000);
            AddAssert("Opponent Unit Value is 11000", () => ReplayController.Players[1].UnitValue.Value == 21000);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 0));
            AddUntilStep("Damage Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 0, 0, 0));
        }

        [Test]
        public void TestExplodeUnitCausingNearDeath()
        {
            AddStep("Setup", () => explodeTest(false, true));
            AddStep("Explode Unit", ReplayController.GoToNextAction);
            AddUntilStep("Unit was deleted", () => !HasUnit(explodingUnit.ID));
            AddAssert("Unit Value is 0", () => ReplayController.Players[0].UnitValue.Value == 0);
            AddAssert("Opponent Unit Value is 2400", () => ReplayController.Players[1].UnitValue.Value == 2400);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/4800", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 4800));
            AddUntilStep("Damage Stats: (0) 0/4800, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 4800, 0, 0));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddAssert("Unit reverted correctly", () => DoesUnitMatchData(explodingUnit.ID, explodingUnit));
            AddAssert("Unit Value is 25000", () => ReplayController.Players[0].UnitValue.Value == 25000);
            AddAssert("Opponent Unit Value is 7200", () => ReplayController.Players[1].UnitValue.Value == 7200);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 0));
            AddUntilStep("Damage Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 0, 0, 0));
        }

        private void explodeTest(bool move, bool opponentLowHp)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var unitIdx = 1;

            for (int i = 1; i <= 3; i++)
            {
                foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(explodeCenter, i))
                {
                    if (move && tile.X < explodeCenter.X && tile.Y == explodeCenter.Y)
                        continue;

                    var unit = CreateBasicReplayUnit(unitIdx++, 1, "Infantry", tile);
                    unit.HitPoints = opponentLowHp ? 3 : 10;
                    turn.ReplayUnit.Add(unit.ID, unit);
                }
            }

            explodingUnit = CreateBasicReplayUnit(0, 0, "Black Bomb", move ? new Vector2I(0, explodeCenter.Y) : explodeCenter);
            turn.ReplayUnit.Add(explodingUnit.ID, explodingUnit);

            var explodeUnitAction = new ExplodeUnitAction
            {
                ExplodedUnitId = explodingUnit.ID,
                HPChange = -3
            };

            if (move)
            {
                var movedUnit = explodingUnit.Clone();
                movedUnit.Position = explodeCenter;

                var path = new UnitPosition[explodeCenter.X + 1];

                for (int i = 0; i <= explodeCenter.X; i++)
                    path[i] = new UnitPosition(new Vector2I(i, explodeCenter.Y));

                explodeUnitAction.MoveUnit = new MoveUnitAction()
                {
                    Distance = path.Length - 1,
                    Unit = movedUnit,
                    Path = path
                };
            }

            turn.Actions.Add(explodeUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(explodeCenter.X * 2 + 1, explodeCenter.Y * 2 + 1));
        }
    }
}
