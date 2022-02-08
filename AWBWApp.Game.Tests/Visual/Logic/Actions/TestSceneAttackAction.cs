using System;
using System.Collections.Generic;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic.Actions
{
    public class TestSceneAttackAction : BaseActionsTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            AddLabel("Destroy Unit");
            AddStep("Setup", () => DestroyTest());
            AddStep("Destroy Land", () => ReplayController.GoToNextAction());
            AddStep("Destroy Sea", () => ReplayController.GoToNextAction());
            AddStep("Destroy Air", () => ReplayController.GoToNextAction());

            AddLabel("1 space");
            AddStep("Setup", () => AttackTest(1));
            AddStep("Attack Left", () => ReplayController.GoToNextAction());
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddStep("Attack Down", () => ReplayController.GoToNextAction());

            AddLabel("2 spaces");
            AddStep("Setup", () => AttackTest(2));
            AddStep("Attack Left", () => ReplayController.GoToNextAction());
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddStep("Attack Down", () => ReplayController.GoToNextAction());

            AddLabel("3 spaces");
            AddStep("Setup", () => AttackTest(3));
            AddStep("Attack Left", () => ReplayController.GoToNextAction());
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddStep("Attack Down", () => ReplayController.GoToNextAction());

            AddLabel("4 spaces");
            AddStep("Setup", () => AttackTest(4));
            AddStep("Attack Left", () => ReplayController.GoToNextAction());
            AddStep("Attack Up", () => ReplayController.GoToNextAction());
            AddStep("Attack Right", () => ReplayController.GoToNextAction());
            AddStep("Attack Down", () => ReplayController.GoToNextAction());

            AddLabel("Attack with Movement");
            AddStep("Setup", () => AttackWithMoveTest());
            AddStep("Perform", () => ReplayController.GoToNextAction());
        }

        private void DestroyTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData();
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

            turn.ReplayUnit.Add(defendingLand.ID, defendingLand);
            turn.Actions.Add(attackAction);

            //Test Sea Explosion
            attackAction = new AttackUnitAction();
            var defendingSea = CreateBasicReplayUnit(2, 1, "Lander", new Vector2I(4, 2));
            defendingSea.Ammo = 0;
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defendingSea.ID, Ammo = 0, HitPoints = 0 };

            turn.ReplayUnit.Add(defendingSea.ID, defendingSea);
            turn.Actions.Add(attackAction);

            //Test Air Explosion
            attackAction = new AttackUnitAction();
            var defendingAir = CreateBasicReplayUnit(3, 1, "Fighter", new Vector2I(2, 4));
            defendingAir.Ammo = 0;
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 10 };
            attackAction.Defender = new ReplayUnit { ID = defendingAir.ID, Ammo = 0, HitPoints = 0 };

            turn.ReplayUnit.Add(defendingAir.ID, defendingAir);
            turn.Actions.Add(attackAction);

            var map = CreateBasicMap(5, 5);
            map.Ids[defendingSea.Position.Value.Y * 5 + defendingSea.Position.Value.X] = 28; //Set the tile under the lander to be sea just so it looks more correct.

            ReplayController.LoadReplay(replayData, map);
        }

        private void AttackTest(int spaces)
        {
            var replayData = CreateBasicReplayData(2);

            var turn = new TurnData();
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

                var attackAction = new AttackUnitAction();
                attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = attackerUnit.Ammo - 1, HitPoints = 9 };
                attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 10 - ((i + 1) * 2) };

                turn.Actions.Add(attackAction);
            }

            ReplayController.LoadReplay(replayData, CreateBasicMap(mapSize, mapSize));
        }

        private void AttackWithMoveTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = new TurnData();
            turn.ReplayUnit = new Dictionary<long, ReplayUnit>();
            turn.Buildings = new Dictionary<Vector2I, ReplayBuilding>();
            turn.Actions = new List<IReplayAction>();

            replayData.TurnData.Add(turn);

            var mapSize = 5;
            var middle = 2;

            var defenderUnit = CreateBasicReplayUnit(0, 1, "Infantry", new Vector2I(middle, middle));
            defenderUnit.Ammo = 0;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            var attackerUnit = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(0, 0));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            var attackAction = new AttackUnitAction();
            attackAction.MoveUnit = new MoveUnitAction
            {
                Distance = 3,
                Path = new[]
                {
                    new UnitPosition { Unit_Visible = true, X = 0, Y = 0 },
                    new UnitPosition { Unit_Visible = true, X = 0, Y = 1 },
                    new UnitPosition { Unit_Visible = true, X = 0, Y = 2 },
                    new UnitPosition { Unit_Visible = true, X = 1, Y = 2 },
                },
                Trapped = false,
                Unit = new ReplayUnit
                {
                    ID = 1,
                    Position = new Vector2I(1, 2)
                }
            };
            attackAction.Attacker = new ReplayUnit { ID = attackerUnit.ID, Ammo = 0, HitPoints = 9 };
            attackAction.Defender = new ReplayUnit { ID = defenderUnit.ID, Ammo = 0, HitPoints = 8 };

            turn.Actions.Add(attackAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(mapSize, mapSize));
        }
    }
}
