using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneSupplyUnitAction : BaseActionsTestScene
    {
        [Test]
        public void TestSupplyUnit()
        {
            AddStep("Setup", supplyTest);
            AddStep("Supply Units", ReplayController.GoToNextAction);
        }

        [Test]
        public void TestSupplyUnitWithMove()
        {
            AddStep("Setup", supplyTestWithMove);
            AddStep("Supply Units", ReplayController.GoToNextAction);
        }

        private void supplyTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = new TurnData();
            turn.ReplayUnit = new Dictionary<long, ReplayUnit>();
            turn.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            turn.Actions = new List<IReplayAction>();

            replayData.TurnData.Add(turn);

            var blackBoat = CreateBasicReplayUnit(0, 1, "APC", new Vector2I(2, 2));
            turn.ReplayUnit.Add(blackBoat.ID, blackBoat);

            var suppliedUnit = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(2, 1));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);
            suppliedUnit = CreateBasicReplayUnit(2, 1, "Infantry", new Vector2I(1, 2));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);
            suppliedUnit = CreateBasicReplayUnit(3, 1, "Infantry", new Vector2I(3, 2));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);
            suppliedUnit = CreateBasicReplayUnit(4, 1, "Infantry", new Vector2I(2, 3));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);

            var supplyUnitAction = new SupplyUnitAction();
            supplyUnitAction.SupplyingUnitId = blackBoat.ID;
            supplyUnitAction.SuppliedUnitIds = new List<int> { 1, 2, 3, 4 };
            turn.Actions.Add(supplyUnitAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[2 * 5 + 2] = 16;

            ReplayController.LoadReplay(replayData, map);
        }

        private void supplyTestWithMove()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = new TurnData();
            turn.ReplayUnit = new Dictionary<long, ReplayUnit>();
            turn.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            turn.Actions = new List<IReplayAction>();

            replayData.TurnData.Add(turn);

            var blackBoat = CreateBasicReplayUnit(0, 1, "APC", new Vector2I(2, 3));
            turn.ReplayUnit.Add(blackBoat.ID, blackBoat);

            var suppliedUnit = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(2, 1));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);
            suppliedUnit = CreateBasicReplayUnit(2, 1, "Infantry", new Vector2I(1, 2));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);
            suppliedUnit = CreateBasicReplayUnit(3, 1, "Infantry", new Vector2I(3, 2));
            turn.ReplayUnit.Add(suppliedUnit.ID, suppliedUnit);

            var supplyUnitAction = new SupplyUnitAction();
            supplyUnitAction.SupplyingUnitId = blackBoat.ID;
            supplyUnitAction.SuppliedUnitIds = new List<int> { 1, 2, 3 };

            supplyUnitAction.MoveUnit = new MoveUnitAction();
            supplyUnitAction.MoveUnit.Distance = 1;
            supplyUnitAction.MoveUnit.Trapped = false;
            supplyUnitAction.MoveUnit.Unit = new ReplayUnit { ID = blackBoat.ID, Position = new Vector2I(2, 2) };
            supplyUnitAction.MoveUnit.Path = new[] { new UnitPosition { X = 2, Y = 3, Unit_Visible = true }, new UnitPosition { X = 2, Y = 2, Unit_Visible = true } };
            turn.Actions.Add(supplyUnitAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[2 * 5 + 2] = 16;
            map.Ids[3 * 5 + 2] = 16;

            ReplayController.LoadReplay(replayData, map);
        }
    }
}
