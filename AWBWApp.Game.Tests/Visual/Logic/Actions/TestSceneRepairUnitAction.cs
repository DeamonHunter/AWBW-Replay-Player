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
        }

        [Test]
        public void TestRepairUnitWithMove()
        {
            AddStep("Setup", supplyTestWithMove);
            AddStep("Repair Unit", ReplayController.GoToNextAction);
        }

        private void supplyTest()
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var blackBoat = CreateBasicReplayUnit(0, 1, "Black Boat", new Vector2I(2, 2));
            turn.ReplayUnit.Add(blackBoat.ID, blackBoat);

            var repairedUnit = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(2, 1));
            repairedUnit.HitPoints = 8;
            turn.ReplayUnit.Add(repairedUnit.ID, repairedUnit);

            var repairUnitAction = new RepairUnitAction();
            repairUnitAction.RepairingUnitID = blackBoat.ID;
            repairUnitAction.RepairedUnitID = repairedUnit.ID;
            repairUnitAction.RepairedUnitHP = 9;
            turn.Actions.Add(repairUnitAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[2 * 5 + 2] = 28;

            ReplayController.LoadReplay(replayData, map);
        }

        private void supplyTestWithMove()
        {
            var replayData = CreateBasicReplayData(2);
            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var blackBoat = CreateBasicReplayUnit(0, 1, "Black Boat", new Vector2I(2, 3));
            turn.ReplayUnit.Add(blackBoat.ID, blackBoat);

            var repairedUnit = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(2, 1));
            repairedUnit.HitPoints = 8;
            turn.ReplayUnit.Add(repairedUnit.ID, repairedUnit);

            var repairUnitAction = new RepairUnitAction();
            repairUnitAction.RepairingUnitID = blackBoat.ID;
            repairUnitAction.RepairedUnitID = repairedUnit.ID;
            repairUnitAction.RepairedUnitHP = 9;

            repairUnitAction.MoveUnit = new MoveUnitAction();
            repairUnitAction.MoveUnit.Distance = 1;
            repairUnitAction.MoveUnit.Trapped = false;
            repairUnitAction.MoveUnit.Unit = new ReplayUnit { ID = blackBoat.ID, Position = new Vector2I(2, 2) };
            repairUnitAction.MoveUnit.Path = new[] { new UnitPosition { X = 2, Y = 3, Unit_Visible = true }, new UnitPosition { X = 2, Y = 2, Unit_Visible = true } };
            turn.Actions.Add(repairUnitAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[2 * 5 + 2] = 28;
            map.Ids[3 * 5 + 2] = 28;

            ReplayController.LoadReplay(replayData, map);
        }
    }
}
