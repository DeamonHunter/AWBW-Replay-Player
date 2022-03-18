using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class AttackUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Fire";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new AttackUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var attackData = (JObject)jObject["Fire"];
            if (attackData == null)
                throw new Exception("Capture Replay Action did not contain information about Capture.");

            var combatInfoVision = attackData["combatInfoVision"];
            var combatInfoVisionData = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)combatInfoVision, turnData.ActiveTeam, turnData.ActivePlayerID);

            var hasVision = (bool)combatInfoVisionData["hasVision"]; //Todo: What does this entail?

            if (!hasVision)
                throw new Exception("Replay contains fight that player has no vision on."); //Is this meant to be for team battles.

            var combatInfo = (JObject)combatInfoVisionData["combatInfo"];

            if (combatInfo["attacker"].Type == JTokenType.String)
            {
                //Todo: What is a "?" attacker when the player attacked with it. A dead unit?
                Logger.Log("Attack action didn't have information on the player attacking?");
                action.Attacker = action.MoveUnit?.Unit;
                if (action.Attacker != null)
                    action.Attacker.HitPoints = 0;
            }
            else
                action.Attacker = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)combatInfo["attacker"]);

            action.Defender = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)combatInfo["defender"]);

            var copValues = (JObject)attackData["copValues"];

            if (copValues != null)
            {
                action.PowerChanges = new List<AttackUnitAction.COPowerChange>();

                foreach (var player in copValues)
                {
                    var powerChange = new AttackUnitAction.COPowerChange
                    {
                        PlayerID = (long)player.Value["playerId"],
                        PowerChange = (int)player.Value["copValue"],
                        TagPowerChange = (int?)player.Value["tagValue"]
                    };
                    action.PowerChanges.Add(powerChange);
                }
            }

            var gainedFunds = (JObject)combatInfo["gainedFunds"];

            if (gainedFunds != null)
            {
                action.GainedFunds = new List<(long, int)>();

                foreach (var player in gainedFunds)
                {
                    if (player.Value.Type == JTokenType.Null)
                        continue;

                    action.GainedFunds.Add((long.Parse(player.Key), (int)player.Value));
                }
            }

            if (attackData.TryGetValue("eliminated", out var eliminatedData) && eliminatedData.Type != JTokenType.Null)
            {
                var eliminationAction = Database.GetActionBuilder("Eliminated").ParseJObjectIntoReplayAction((JObject)eliminatedData, replayData, turnData);
                action.EliminatedAction = eliminationAction as EliminatedAction;
                if (eliminationAction == null)
                    throw new Exception("Attack action was expecting a elimination action.");
            }

            return action;
        }
    }

    public class AttackUnitAction : IReplayAction
    {
        public string ReadibleName => "Attack";

        public ReplayUnit Attacker { get; set; }
        public ReplayUnit Defender { get; set; }
        public List<COPowerChange> PowerChanges { get; set; }
        public List<(long playerID, int funds)> GainedFunds { get; set; }

        public MoveUnitAction MoveUnit;
        public EliminatedAction EliminatedAction;

        private ReplayUnit undoAttacker;
        private ReplayUnit undoDefender;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var attackerUnit = controller.Map.GetDrawableUnit(Attacker.ID);
            var defenderUnit = controller.Map.GetDrawableUnit(Defender.ID);

            var attackerStats = Attacker;
            var defenderStats = Defender;

            if (!attackerUnit.OwnerID.HasValue)
                throw new Exception("Attacking unit doesn't have an owner id?");
            if (!defenderUnit.OwnerID.HasValue)
                throw new Exception("Defending unit doesn't have an owner id?");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;

                attackerUnit.CanMove.Value = true;
            }

            //Reverse order if the defender has a power active that reverses order, but not if the attacker also has a power to reverse order.
            var attackerPower = controller.GetActivePowerForPlayer(attackerUnit.OwnerID.Value);
            var defenderPower = controller.GetActivePowerForPlayer(defenderUnit.OwnerID.Value);

            var defenderCounters = !attackerUnit.Dived.Value && canCounterAttack(defenderUnit, attackerUnit.MapPosition);

            var swapAttackOrder = (defenderPower?.COPower.AttackFirst ?? false) && !(attackerPower?.COPower.AttackFirst ?? false);

            if (swapAttackOrder && defenderCounters)
            {
                (attackerUnit, defenderUnit) = (defenderUnit, attackerUnit);
                (attackerStats, defenderStats) = (defenderStats, attackerStats);
            }

            //Perform Attack vs Defender
            var reticule = PlayAttackAnimation(controller, attackerUnit.MapPosition, defenderUnit.MapPosition, attackerUnit);
            yield return ReplayWait.WaitForTransformable(reticule);

            attackerUnit.CanMove.Value = false;
            defenderUnit.UpdateUnit(defenderStats);

            if (defenderUnit.HealthPoints.Value <= 0 || !defenderCounters)
            {
                attackerUnit.UpdateUnit(attackerStats);
                if (defenderUnit.HealthPoints.Value <= 0)
                    controller.Map.DeleteUnit(defenderUnit.UnitID, true);
                afterAttackChanges(controller);

                //Destroying a unit can eliminate a player. i.e. They have no units left.
                if (EliminatedAction != null)
                {
                    foreach (var transformable in EliminatedAction.PerformAction(controller))
                        yield return transformable;
                }
                yield break;
            }

            //Todo: Figure out ammo usage
            if (attackerUnit.UnitData.MaxAmmo != 99)
                attackerUnit.Ammo.Value -= 1;

            //Perform Attack vs Attacker
            reticule = PlayAttackAnimation(controller, defenderUnit.MapPosition, attackerUnit.MapPosition, defenderUnit);
            yield return ReplayWait.WaitForTransformable(reticule);

            attackerUnit.UpdateUnit(attackerStats);

            if (attackerUnit.HealthPoints.Value <= 0)
            {
                controller.Map.DeleteUnit(attackerUnit.UnitID, true);
                controller.UpdateFogOfWar();
            }

            afterAttackChanges(controller);

            //Destroying a unit can eliminate a player. i.e. They have no units left.
            if (EliminatedAction != null)
            {
                foreach (var transformable in EliminatedAction.PerformAction(controller))
                    yield return transformable;
            }
        }

        private bool canCounterAttack(DrawableUnit defendingUnit, Vector2I attackerLocation)
        {
            //If both values are 0, then the unit cannot attack
            if (defendingUnit.UnitData.AttackRange.X == 0 && defendingUnit.UnitData.AttackRange.Y == 0)
                return false;

            var distance = (defendingUnit.MapPosition - attackerLocation).ManhattonDistance();
            if (distance < defendingUnit.UnitData.AttackRange.X || distance > defendingUnit.UnitData.AttackRange.Y)
                return false;

            if (defendingUnit.UnitData.SecondWeapon)
                return true;

            return defendingUnit.UnitData.MaxAmmo <= 0 || defendingUnit.Ammo.Value > 0;
        }

        private void afterAttackChanges(ReplayController controller)
        {
            if (GainedFunds != null)
            {
                foreach (var (playerID, funds) in GainedFunds)
                    controller.Players[playerID].Funds.Value += funds;
            }

            foreach (var player in PowerChanges)
            {
                var playerData = controller.Players[player.PlayerID];

                var coPower = playerData.ActiveCO.Value;
                coPower.Power = player.PowerChange;
                playerData.ActiveCO.Value = coPower;

                if (player.TagPowerChange.HasValue)
                {
                    var tagPower = playerData.TagCO.Value;
                    tagPower.Power = player.TagPowerChange;
                    playerData.TagCO.Value = tagPower;
                }
            }
        }

        public EffectAnimation PlayAttackAnimation(ReplayController controller, Vector2I start, Vector2I end, DrawableUnit attacker)
        {
            var effect = controller.Map.PlayEffect("Effects/Target", 100, start, 0, x =>
            {
                x.WaitForTransformationToComplete(attacker)
                 .MoveTo(GameMap.GetDrawablePositionForBottomOfTile(start) + DrawableTile.HALF_BASE_SIZE).FadeTo(0.5f).ScaleTo(0.5f)
                 .FadeTo(1, 250, Easing.In).MoveTo(GameMap.GetDrawablePositionForBottomOfTile(end) + DrawableTile.HALF_BASE_SIZE, 400, Easing.In).ScaleTo(1, 600, Easing.OutBounce).RotateTo(180, 400).Then().Expire();
            });

            return effect;
        }

        public void UndoAction(ReplayController controller)
        {
            throw new NotImplementedException("Undo Attack Action is not complete");
        }

        public class COPowerChange
        {
            public long PlayerID;
            public int PowerChange;
            public int? TagPowerChange;
        }
    }
}
