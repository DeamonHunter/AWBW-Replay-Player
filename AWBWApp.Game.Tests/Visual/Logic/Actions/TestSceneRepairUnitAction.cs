using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneRepairUnitAction : BaseActionsTestScene
    {
        [Test]
        public void TestRepairUnit()
        {
            AddStep("Setup", supplyTest);
            AddStep("Repair Unit", ReplayController.GoToNextAction);
            AddUntilStep("Repaired", () => !ReplayController.HasOngoingAction());
            AddAssert("Unit HP is 9", () => DoesUnitPassTest(1, x => x.HealthPoints.Value == 9));
            AddAssert("Funds is 0", () => ReplayController.ActivePlayer.Funds.Value == 0);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit HP is 8", () => DoesUnitPassTest(1, x => x.HealthPoints.Value == 8));
            AddAssert("Funds is 100", () => ReplayController.ActivePlayer.Funds.Value == 100);
        }

        [Test]
        public void TestRepairUnitWithMove()
        {
            AddStep("Setup", supplyTestWithMove);
            AddStep("Repair Unit", ReplayController.GoToNextAction);
            AddUntilStep("Repaired", () => !ReplayController.HasOngoingAction());
            AddAssert("Unit HP is 9", () => DoesUnitPassTest(1, x => x.HealthPoints.Value == 9));
            AddAssert("Funds is 0", () => ReplayController.ActivePlayer.Funds.Value == 0);
            AddStep("Undo", ReplayController.UndoAction);
            AddAssert("Unit HP is 8", () => DoesUnitPassTest(1, x => x.HealthPoints.Value == 8));
            AddAssert("Funds is 100", () => ReplayController.ActivePlayer.Funds.Value == 100);
        }

        private void supplyTest()
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);
            turn.Players[0].Funds = 100;

            var blackBoat = CreateBasicReplayUnit(0, 0, "Black Boat", new Vector2I(2, 2));
            turn.ReplayUnit.Add(blackBoat.ID, blackBoat);

            var repairedUnit = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(2, 1));
            repairedUnit.HitPoints = 8;
            turn.ReplayUnit.Add(repairedUnit.ID, repairedUnit);

            var repairUnitAction = new RepairUnitAction
            {
                RepairingUnitID = blackBoat.ID,
                RepairedUnitID = repairedUnit.ID,
                RepairedUnitHP = 9
            };
            turn.Actions.Add(repairUnitAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[2 * 5 + 2] = 28;

            ReplayController.LoadReplay(replayData, map);
            ReplayController.AllowRewinding = true;
        }

        private void supplyTestWithMove()
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);
            turn.Players[0].Funds = 100;

            var blackBoat = CreateBasicReplayUnit(0, 0, "Black Boat", new Vector2I(2, 3));
            turn.ReplayUnit.Add(blackBoat.ID, blackBoat);

            var repairedUnit = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(2, 1));
            repairedUnit.HitPoints = 8;
            turn.ReplayUnit.Add(repairedUnit.ID, repairedUnit);

            var repairUnitAction = new RepairUnitAction
            {
                RepairingUnitID = blackBoat.ID,
                RepairedUnitID = repairedUnit.ID,
                RepairedUnitHP = 9,
                MoveUnit = new MoveUnitAction
                {
                    Distance = 1,
                    Trapped = false,
                    Unit = new ReplayUnit { ID = blackBoat.ID, Position = new Vector2I(2, 2) },
                    Path = new[]
                    {
                        new UnitPosition(new Vector2I(2, 3)),
                        new UnitPosition(new Vector2I(2, 2))
                    }
                }
            };

            turn.Actions.Add(repairUnitAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[2 * 5 + 2] = 28;
            map.Ids[3 * 5 + 2] = 28;

            ReplayController.LoadReplay(replayData, map);
            ReplayController.AllowRewinding = true;
        }
    }
}
