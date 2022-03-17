using System;
using System.Collections.Generic;
using System.Diagnostics;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    [TestFixture]
    public class TestSceneAttackAction : BaseActionsTestScene
    {
        [Test]
        public void TestDestroyUnits()
        {
            AddStep("Setup", destroyTest);
            AddStep("Destroy Land", () => ReplayController.GoToNextAction());
            AddStep("Destroy Sea", () => ReplayController.GoToNextAction());
            AddStep("Destroy Air", () => ReplayController.GoToNextAction());
        }

        [TestCase(1)]
        [TestCase(4)]
        public void TestAttackWithRange(int range)
        {
            AddStep("Setup", () => attackTest(range));
            AddStep("Attack Left", () => ReplayController.GoToNextAction());
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddStep("Attack Down", () => ReplayController.GoToNextAction());
        }

        [Test]
        public void TestAttackWithMovement()
        {
            AddStep("Setup", attackWithMoveTest);
            AddStep("Perform", () => ReplayController.GoToNextAction());
        }

        [Test]
        public void CounterAttackTest()
        {
            AddStep("Setup", counterAttackTest);
            AddStep("Attack Unit with Ammo", () => ReplayController.GoToNextAction());
            AddStep("Transport", () => ReplayController.GoToNextAction());
            AddStep("No ammo", () => ReplayController.GoToNextAction());
            AddStep("No ammo but has secondary", () => ReplayController.GoToNextAction());
            AddStep("Too close", () => ReplayController.GoToNextAction());
            AddStep("Too far", () => ReplayController.GoToNextAction());
        }
        private void destroyTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var attackerUnit = CreateBasicReplayUnit(0, 0, "Artillery", new Vector2I(2, 2));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            //Test Land Explosion
            var attackAction = new AttackUnitAction();
            var defendingLand = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(2, 0));
            defendingLand.Ammo = 0;
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defendingLand.ID, Ammo = 0, HitPoints = 0 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();

            turn.ReplayUnit.Add(defendingLand.ID, defendingLand);
            turn.Actions.Add(attackAction);

            //Test Sea Explosion
            attackAction = new AttackUnitAction();
            var defendingSea = CreateBasicReplayUnit(2, 1, "Lander", new Vector2I(4, 2));
            defendingSea.Ammo = 0;
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defendingSea.ID, Ammo = 0, HitPoints = 0 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();

            turn.ReplayUnit.Add(defendingSea.ID, defendingSea);
            turn.Actions.Add(attackAction);

            //Test Air Explosion
            attackAction = new AttackUnitAction();
            var defendingAir = CreateBasicReplayUnit(3, 1, "Fighter", new Vector2I(2, 4));
            defendingAir.Ammo = 0;
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defendingAir.ID, Ammo = 0, HitPoints = 0 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();

            turn.ReplayUnit.Add(defendingAir.ID, defendingAir);
            turn.Actions.Add(attackAction);

            Debug.Assert(defendingSea.Position.HasValue, "Sea Unit does not have position somehow.");

            var map = CreateBasicMap(5, 5);
            map.Ids[defendingSea.Position.Value.Y * 5 + defendingSea.Position.Value.X] = 28; //Set the tile under the lander to be sea just so it looks more correct.

            ReplayController.LoadReplay(replayData, map);
        }

        private void attackTest(int spaces)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            turn.ReplayUnit = new Dictionary<long, ReplayUnit>();
            turn.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            turn.Actions = new List<IReplayAction>();

            replayData.TurnData.Add(turn);

            var mapSize = 2 * spaces + 1;
            var middle = spaces;

            var defenderUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(middle, middle));
            defenderUnit.Ammo = 0;

            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            for (int i = 0; i < 4; i++)
            {
                Vector2I position;

                switch (i)
                {
                    case 0:
                        position = new Vector2I(0, middle);
                        break;

                    case 1:
                        position = new Vector2I(middle, 0);
                        break;

                    case 2:
                        position = new Vector2I(mapSize - 1, middle);
                        break;

                    case 3:
                        position = new Vector2I(middle, mapSize - 1);
                        break;

                    default:
                        throw new Exception();
                }

                var attackerUnit = CreateBasicReplayUnit(i + 1, 0, "Infantry", position);
                attackerUnit.Ammo = 1;

                turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

                var attackAction = new AttackUnitAction
                {
                    Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 9 },
                    Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 10 - ((i + 1) * 2) },
                    PowerChanges = new List<AttackUnitAction.COPowerChange>()
                };

                turn.Actions.Add(attackAction);
            }

            ReplayController.LoadReplay(replayData, CreateBasicMap(mapSize, mapSize));
        }

        private void attackWithMoveTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            const int map_size = 5;
            const int middle = 2;

            var defenderUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(middle, middle));
            defenderUnit.Ammo = 0;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            var attackerUnit = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(0, 0));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            var attackAction = new AttackUnitAction
            {
                MoveUnit = new MoveUnitAction
                {
                    Distance = 3,
                    Path = new[]
                    {
                        new UnitPosition { UnitVisible = true, X = 0, Y = 0 },
                        new UnitPosition { UnitVisible = true, X = 0, Y = 1 },
                        new UnitPosition { UnitVisible = true, X = 0, Y = 2 },
                        new UnitPosition { UnitVisible = true, X = 1, Y = 2 },
                    },
                    Trapped = false,
                    Unit = new ReplayUnit
                    {
                        ID = 1,
                        Position = new Vector2I(1, 2)
                    }
                },
                Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 9 },
                Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 8 },
                PowerChanges = new List<AttackUnitAction.COPowerChange>()
            };

            turn.Actions.Add(attackAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(map_size, map_size));
        }

        private void counterAttackTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            // Normal Attack
            var attackerUnit = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(1, 0));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            var defenderUnit = CreateBasicReplayUnit(1, 1, "Infantry", new Vector2I(0, 0));
            defenderUnit.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            var attackAction = new AttackUnitAction();
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 8 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 5 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();
            turn.Actions.Add(attackAction);

            //Attack a Transport
            attackerUnit = CreateBasicReplayUnit(2, 0, "Infantry", new Vector2I(1, 1));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            defenderUnit = CreateBasicReplayUnit(3, 1, "APC", new Vector2I(0, 1));
            defenderUnit.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            attackAction = new AttackUnitAction();
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 8 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();
            turn.Actions.Add(attackAction);

            // No Ammo
            attackerUnit = CreateBasicReplayUnit(4, 0, "Infantry", new Vector2I(1, 2));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            defenderUnit = CreateBasicReplayUnit(5, 1, "Infantry", new Vector2I(0, 2));
            defenderUnit.Ammo = 0;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            attackAction = new AttackUnitAction();
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 5 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();
            turn.Actions.Add(attackAction);

            // No Ammo but has secondary
            attackerUnit = CreateBasicReplayUnit(6, 0, "Infantry", new Vector2I(1, 3));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            defenderUnit = CreateBasicReplayUnit(7, 1, "Mega Tank", new Vector2I(0, 3));
            defenderUnit.Ammo = 0;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            attackAction = new AttackUnitAction();
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 0 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 9 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();
            turn.Actions.Add(attackAction);

            // Too Close
            attackerUnit = CreateBasicReplayUnit(8, 0, "Infantry", new Vector2I(1, 4));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            defenderUnit = CreateBasicReplayUnit(9, 1, "Artillery", new Vector2I(0, 4));
            defenderUnit.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            attackAction = new AttackUnitAction();
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 1, HitPoints = 8 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();
            turn.Actions.Add(attackAction);

            // Too Far
            attackerUnit = CreateBasicReplayUnit(10, 0, "Artillery", new Vector2I(2, 5));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            defenderUnit = CreateBasicReplayUnit(11, 1, "Infantry", new Vector2I(0, 5));
            defenderUnit.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            attackAction = new AttackUnitAction();
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 1, HitPoints = 1 };
            attackAction.PowerChanges = new List<AttackUnitAction.COPowerChange>();
            turn.Actions.Add(attackAction);

            var map = CreateBasicMap(3, 6);

            ReplayController.LoadReplay(replayData, map);
        }
    }
}
