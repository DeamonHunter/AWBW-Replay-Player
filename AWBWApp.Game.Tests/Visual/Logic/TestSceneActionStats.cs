using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    [TestFixture]
    public partial class TestSceneActionStats : BaseActionsTestScene
    {
        [Test]
        public void TestAttack()
        {
            AddStep("Setup", setupAttackTest);
            AddStep("Attack with little damage", () => ReplayController.GoToNextAction());
            AddUntilStep("Lost Stats: (0) 0/200, (1) 0/500", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 200, 0, 500));
            AddUntilStep("Damage Stats: (0) 0/500, (1) 0/200", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 500, 0, 200));
            AddStep("Attack to kill damaged", () => ReplayController.GoToNextAction());
            AddUntilStep("Lost Stats: (0) 0/200, (1) 1/1000", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 200, 1, 1000));
            AddUntilStep("Damage Stats: (0) 1/1000, (1) 0/200", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 1, 1000, 0, 200));
            AddStep("Attack to kill full health", () => ReplayController.GoToNextAction());
            AddUntilStep("Lost Stats: (0) 0/200, (1) 2/2000", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 200, 2, 2000));
            AddUntilStep("Damage Stats: (0) 2/2000, (1) 0/200", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 2, 2000, 0, 200));
            AddStep("Next Turn", () => ReplayController.GoToNextTurn());
            AddStep("Activate Sonja Power", () => ReplayController.GoToNextAction());
            AddStep("Next Turn", () => ReplayController.GoToNextTurn());
            AddStep("Attack with counter attack", () => ReplayController.GoToNextAction());
            AddUntilStep("Lost Stats: (0) 0/700, (1) 2/2200", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 700, 2, 2200));
            AddUntilStep("Lost Stats: (0) 2/2200, (1) 0/700", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 2, 2200, 0, 700));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Lost Stats: (0) 0/200, (1) 2/2000", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 200, 2, 2000));
            AddAssert("Lost Stats: (0) 2/2000, (1) 0/200", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 2, 2000, 0, 200));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Lost Stats: (0) 0/200, (1) 1/1000", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 200, 1, 1000));
            AddAssert("Damage Stats: (0) 1/1000, (1) 0/200", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 1, 1000, 0, 200));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Lost Stats: (0) 0/200, (1) 0/500", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 200, 0, 500));
            AddAssert("Damage Stats: (0) 0/500, (1) 0/200", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 500, 0, 200));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddAssert("Lost Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 0));
            AddAssert("Damage Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.DamageUnit, "Infantry", 0, 0, 0, 0));
        }

        [Test]
        public void TestJoin()
        {
            AddStep("Setup", setupJoinTest);
            AddStep("Join 2 weak units", () => ReplayController.GoToNextAction());
            AddUntilStep("Join Stats: (0) 1/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.JoinUnit, "Infantry", 1, 0, 0, 0));
            AddStep("Join 2 strong units", () => ReplayController.GoToNextAction());
            AddUntilStep("Join Stats: (0) 2/600, (1) 0/0", () => DoesStatsMatch(UnitStatType.JoinUnit, "Infantry", 2, 600, 0, 0));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddUntilStep("Join Stats: (0) 1/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.JoinUnit, "Infantry", 1, 0, 0, 0));
            AddStep("Undo", () => ReplayController.GoToPreviousAction());
            AddUntilStep("Join Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.JoinUnit, "Infantry", 0, 0, 0, 0));
        }

        [Test]
        public void TestCreateUnit()
        {
            AddStep("Setup", setupBuildTest);
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddUntilStep("Build Stats: (0) 1/1000, (1) 0/0", () => DoesStatsMatch(UnitStatType.BuildUnit, "Infantry", 1, 1000, 0, 0));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Build Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.BuildUnit, "Infantry", 0, 0, 0, 0));
        }

        [Test]
        public void TestDestroyUnit()
        {
            AddStep("Setup", setupDestroyTest);
            AddStep("Create Unit", ReplayController.GoToNextAction);
            AddUntilStep("Lost Stats: (0) 1/1000, (1) 0/0", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 1, 1000, 0, 0));
            AddStep("Undo", ReplayController.GoToPreviousAction);
            AddUntilStep("Lost Stats: (0) 0/0, (1) 0/0", () => DoesStatsMatch(UnitStatType.LostUnit, "Infantry", 0, 0, 0, 0));
        }

        private void setupAttackTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            turn.Players[1].ActiveCOID = 18;
            turn.Players[1].RequiredPowerForNormal = 270000;
            turn.Players[1].RequiredPowerForSuper = 450000;
            replayData.TurnData.Add(turn);

            var attackerUnit = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(1, 1));
            attackerUnit.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit.ID, attackerUnit);

            var attackerUnit2 = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(3, 1));
            attackerUnit2.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit2.ID, attackerUnit2);

            var attackerUnit3 = CreateBasicReplayUnit(2, 0, "Infantry", new Vector2I(1, 2));
            attackerUnit2.Ammo = 1;
            turn.ReplayUnit.Add(attackerUnit3.ID, attackerUnit3);

            var defenderUnit = CreateBasicReplayUnit(3, 1, "Infantry", new Vector2I(2, 1));
            defenderUnit.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit.ID, defenderUnit);

            var defenderUnit2 = CreateBasicReplayUnit(4, 1, "Infantry", new Vector2I(2, 2));
            defenderUnit2.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit2.ID, defenderUnit2);

            var defenderUnit3 = CreateBasicReplayUnit(5, 1, "Infantry", new Vector2I(1, 3));
            defenderUnit3.Ammo = 1;
            turn.ReplayUnit.Add(defenderUnit3.ID, defenderUnit3);

            turn.Actions.Add(createAttackUnitAction(attackerUnit, defenderUnit, 8, 5, false));
            turn.Actions.Add(createAttackUnitAction(attackerUnit2, defenderUnit, 10, 0, false));
            turn.Actions.Add(createAttackUnitAction(attackerUnit3, defenderUnit2, 10, 0, false));

            turn = CreateBasicTurnData(replayData);
            turn.Players[1].ActiveCOID = 18;
            turn.Players[1].RequiredPowerForNormal = 270000;
            turn.Players[1].RequiredPowerForSuper = 450000;
            turn.ActivePlayerID = 1;

            replayData.TurnData.Add(turn);
            var turn2Unit = attackerUnit.Clone();
            turn2Unit.HitPoints = 8;

            turn.ReplayUnit.Add(turn2Unit.ID, turn2Unit);
            turn.ReplayUnit.Add(attackerUnit2.ID, attackerUnit2.Clone());
            turn.ReplayUnit.Add(attackerUnit3.ID, attackerUnit3.Clone());
            turn.ReplayUnit.Add(defenderUnit3.ID, defenderUnit3.Clone());

            var powerAction = new PowerAction
            {
                CombatOfficerName = "Sonja",
                PowerName = "Counter Break",
                COPower = GetCOStorage().GetCOByName("Sonja").SuperPower,
                IsSuperPower = true,
                SightRangeIncrease = 1
            };

            turn.Actions.Add(powerAction);

            turn = CreateBasicTurnData(replayData);
            turn.Players[1].ActiveCOID = 18;
            turn.Players[1].RequiredPowerForNormal = 270000;
            turn.Players[1].RequiredPowerForSuper = 450000;
            replayData.TurnData.Add(turn);

            turn.ReplayUnit.Add(turn2Unit.ID, turn2Unit.Clone());
            turn.ReplayUnit.Add(attackerUnit2.ID, attackerUnit2.Clone());
            turn.ReplayUnit.Add(attackerUnit3.ID, attackerUnit3.Clone());
            turn.ReplayUnit.Add(defenderUnit3.ID, defenderUnit3.Clone());

            turn.Actions.Add(createAttackUnitAction(attackerUnit3, defenderUnit3, 5, 8, false));

            var map = CreateBasicMap(5, 5);
            ReplayController.LoadReplay(replayData, map);
        }

        private void setupJoinTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            replayData.TurnData.Add(turn);

            var joining1 = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(1, 2));
            joining1.HitPoints = 4;
            turn.ReplayUnit.Add(joining1.ID, joining1);

            var joining2 = CreateBasicReplayUnit(1, 0, "Infantry", new Vector2I(3, 2));
            joining2.HitPoints = 8;
            turn.ReplayUnit.Add(joining2.ID, joining2);

            var joined = CreateBasicReplayUnit(2, 0, "Infantry", new Vector2I(2, 2));
            joined.HitPoints = 4;
            turn.ReplayUnit.Add(joined.ID, joined);

            var join1 = new JoinUnitAction
            {
                JoiningUnitID = joining1.ID,
                JoinedUnit = joined.Clone(),
                FundsAfterJoin = 0,
            };
            join1.JoinedUnit.HitPoints = 8;
            turn.Actions.Add(join1);

            var join2 = new JoinUnitAction
            {
                JoiningUnitID = joining2.ID,
                JoinedUnit = joined.Clone(),
                FundsAfterJoin = 600,
            };
            join2.JoinedUnit.HitPoints = 10;
            turn.Actions.Add(join2);

            var map = CreateBasicMap(5, 5);
            ReplayController.LoadReplay(replayData, map);
        }

        private void setupBuildTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            turn.Players[0].Funds = 1000;
            replayData.TurnData.Add(turn);

            var building = CreateBasicReplayBuilding(0, new Vector2I(2, 2), 39);
            turn.Buildings.Add(building.Position, building);

            var createdUnit = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(2, 2));
            createdUnit.TimesMoved = 1;

            var createUnitAction = new BuildUnitAction
            {
                NewUnit = createdUnit
            };
            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }

        private void setupDestroyTest()
        {
            var replayData = CreateBasicReplayData(2);

            var turn = CreateBasicTurnData(replayData);
            turn.Players[0].Funds = 1000;
            replayData.TurnData.Add(turn);

            var destroyedUnit = CreateBasicReplayUnit(0, 0, "Infantry", new Vector2I(2, 2));
            turn.ReplayUnit.Add(destroyedUnit.ID, destroyedUnit);

            var createUnitAction = new DeleteUnitAction()
            {
                DeletedUnitId = destroyedUnit.ID,
            };
            turn.Actions.Add(createUnitAction);

            ReplayController.LoadReplay(replayData, CreateBasicMap(5, 5));
        }

        private AttackUnitAction createAttackUnitAction(ReplayUnit attacker, ReplayUnit defender, int attackerHealthAfter, int defenderHealthAfter, bool counterAttack)
        {
            var attackerAmmo = attacker.Ammo ?? 0;
            var defenderAmmo = defender.Ammo ?? 0;

            var attack = new AttackUnitAction
            {
                Attacker = attacker.Clone(),
                Defender = defender.Clone(),
                PowerChanges = new List<AttackUnitAction.COPowerChange>()
            };

            attack.Attacker.Ammo = Math.Max(0, attackerAmmo - 1);
            attack.Attacker.HitPoints = attackerHealthAfter;

            attack.Defender.Ammo = Math.Max(0, (counterAttack ? defenderAmmo - 1 : defenderAmmo));
            attack.Defender.HitPoints = defenderHealthAfter;

            return attack;
        }
    }
}
