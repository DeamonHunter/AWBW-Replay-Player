using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
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

            var copValues = (JObject)attackData["copValues"];
            if (copValues == null)
                throw new Exception("COP Values were null");

            long? defenderID = null;

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

                if (player.Key == "defender")
                    defenderID = powerChange.PlayerID;
            }

            if (defenderID == null)
                throw new Exception("Unknown defender ID.");

            var defendingTeam = replayData.ReplayInfo.Players[defenderID.Value].TeamName;

            var combatInfoVision = attackData["combatInfoVision"];

            var attackerInfoVisionData = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)combatInfoVision, turnData.ActiveTeam, turnData.ActivePlayerID);
            var attackerCombatInfo = attackerInfoVisionData["combatInfo"];

            var defenderInfoVisionData = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)combatInfoVision, defendingTeam, defenderID.Value);
            var defenderCombatInfo = defenderInfoVisionData["combatInfo"];

            if (!(bool)attackerInfoVisionData["hasVision"] || !(bool)defenderInfoVisionData["hasVision"])
                throw new Exception("Replay contains fight that player has no vision on.");

            action.Attacker = parseInfoIntoUnit(attackerCombatInfo["attacker"], defenderCombatInfo["attacker"]);
            action.Defender = parseInfoIntoUnit(defenderCombatInfo["defender"], attackerCombatInfo["defender"]);

            if (action.Attacker == null)
            {
                Logger.Log("Attack action didn't have information on the player attacking?");
                action.Attacker = action.MoveUnit?.Unit;
                if (action.Attacker != null)
                    action.Attacker.HitPoints = 0;
            }

            if (action.Attacker == null || action.Defender == null)
                throw new Exception("Unknown attacker or defender?");

            var attackerGainedFunds = (JObject)attackerCombatInfo["gainedFunds"];

            if (attackerGainedFunds != null)
            {
                action.GainedFunds ??= new Dictionary<long, int>();

                foreach (var player in attackerGainedFunds)
                {
                    if (player.Value.Type == JTokenType.Null)
                        continue;

                    var key = long.Parse(player.Key);
                    if (action.GainedFunds.ContainsKey(key))
                        continue;

                    action.GainedFunds.Add(key, (int)player.Value);
                }
            }
            var defenderGainedFunds = (JObject)defenderCombatInfo["gainedFunds"];

            if (defenderGainedFunds != null)
            {
                action.GainedFunds ??= new Dictionary<long, int>();

                foreach (var player in defenderGainedFunds)
                {
                    if (player.Value.Type == JTokenType.Null)
                        continue;

                    var key = long.Parse(player.Key);
                    if (action.GainedFunds.ContainsKey(key))
                        continue;

                    action.GainedFunds.Add(key, (int)player.Value);
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

        private ReplayUnit parseInfoIntoUnit(JToken owner, JToken other)
        {
            if (owner == null || owner.Type == JTokenType.Null || owner.Type == JTokenType.String)
            {
                if (other == null || other.Type == JTokenType.Null || other.Type == JTokenType.String)
                    return null;

                return ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)other);
            }

            return ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)owner);
        }
    }

    public class AttackUnitAction : IReplayAction, IActionCanEndGame
    {
        public ReplayUnit Attacker { get; set; }
        public ReplayUnit Defender { get; set; }
        public List<COPowerChange> PowerChanges { get; set; }
        public Dictionary<long, int> GainedFunds { get; set; }

        public MoveUnitAction MoveUnit;
        public EliminatedAction EliminatedAction;

        private ReplayUnit originalAttacker;
        private int attackerValueLost;
        private ReplayUnit originalDefender;
        private int defenderValueLost;
        private readonly Dictionary<long, ReplayUnit> originalUnits = new Dictionary<long, ReplayUnit>();
        private readonly Dictionary<long, int> originalPowers = new Dictionary<long, int>();
        private Dictionary<long, int> originalFunds;

        private Dictionary<Vector2I, int> buildingsHP = new Dictionary<Vector2I, int>();

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Attack" : "Attack";

            if (MoveUnit == null)
                return $"{originalAttacker.UnitName} Attacks {originalDefender.UnitName}";

            return $"{originalAttacker.UnitName} Moves + Attacks {originalDefender.UnitName}";
        }

        public bool EndsGame() => EliminatedAction?.EndsGame() ?? false;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.TryGetValue(Attacker.ID, out var attacker))
                throw new ReplayMissingUnitException(Attacker.ID);
            if (!context.Units.TryGetValue(Defender.ID, out var defender))
                throw new ReplayMissingUnitException(Defender.ID);

            originalAttacker = attacker.Clone();
            originalDefender = defender.Clone();

            originalUnits.Add(originalAttacker.ID, originalAttacker);
            originalUnits.Add(originalDefender.ID, originalDefender);

            var attackerCO = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[attacker.PlayerID!.Value].ActiveCOID);
            var defenderCO = controller.COStorage.GetCOByAWBWId(context.PlayerTurns[defender.PlayerID!.Value].ActiveCOID);

            if (Attacker.HitPoints!.Value > 0)
            {
                attacker.Overwrite(Attacker);
                attackerValueLost = ReplayActionHelper.CalculateUnitCost(originalAttacker, attackerCO.DayToDayPower, null) - ReplayActionHelper.CalculateUnitCost(attacker, attackerCO.DayToDayPower, null);
            }
            else
            {
                context.RemoveUnitFromSetupContext(Attacker.ID, originalUnits, out attackerValueLost);

                if (context.Buildings.TryGetValue(attacker.Position!.Value, out var buildingBelow) && buildingBelow.Capture != 20)
                {
                    buildingsHP.Add(attacker.Position.Value, buildingBelow.Capture!.Value);
                    buildingBelow.Capture = 20;
                }
            }

            if (Defender.HitPoints!.Value > 0)
            {
                defender.Overwrite(Defender);
                defenderValueLost = ReplayActionHelper.CalculateUnitCost(originalDefender, defenderCO.DayToDayPower, null) - ReplayActionHelper.CalculateUnitCost(defender, defenderCO.DayToDayPower, null);
            }
            else
            {
                context.RemoveUnitFromSetupContext(Defender.ID, originalUnits, out defenderValueLost);

                if (context.Buildings.TryGetValue(defender.Position!.Value, out var buildingBelow) && buildingBelow.Capture != 20)
                {
                    buildingsHP.Add(defender.Position.Value, buildingBelow.Capture!.Value);
                    buildingBelow.Capture = 20;
                }
            }

            context.AdjustStatReadoutsFromUnitList(originalAttacker.PlayerID!.Value, originalUnits.Values);

            //Note: All transports can't attack, so there should only ever be one unit here.
            controller.Stats.CurrentTurnStatsReadout[originalDefender.PlayerID!.Value].RegisterUnitStats(UnitStatType.DamageUnit, originalAttacker.UnitName, originalAttacker.PlayerID!.Value, attackerValueLost);

            foreach (var powerChange in PowerChanges)
            {
                originalPowers.Add(powerChange.PlayerID, context.PowerValuesForPlayers[powerChange.PlayerID]);
                context.PowerValuesForPlayers[powerChange.PlayerID] = powerChange.PowerChange;
            }

            if (GainedFunds != null && GainedFunds.Count > 0)
            {
                originalFunds = new Dictionary<long, int>();

                foreach (var funds in GainedFunds)
                {
                    originalFunds.Add(funds.Key, context.FundsValuesForPlayers[funds.Key]);
                    context.FundsValuesForPlayers[funds.Key] += funds.Value;
                }
            }
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Attack Action.");

            var attackerUnit = controller.Map.GetDrawableUnit(Attacker.ID);
            var defenderUnit = controller.Map.GetDrawableUnit(Defender.ID);

            var attackerStats = Attacker;
            var defenderStats = Defender;

            var attackerValue = attackerValueLost;
            var defenderValue = defenderValueLost;

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
                (attackerValue, defenderValue) = (defenderValue, attackerValue);
            }

            //Perform Attack vs Defender

            EffectAnimation reticule;

            if (swapAttackOrder || !controller.ShouldPlayerActionBeHidden(attackerUnit.MapPosition))
                reticule = PlayAttackAnimation(controller, attackerUnit.MapPosition, defenderUnit.MapPosition, attackerUnit, false);
            else //If we can't see the attacker just play the animation on them.
                reticule = PlayAttackAnimation(controller, defenderUnit.MapPosition, defenderUnit.MapPosition, defenderUnit, false);

            yield return ReplayWait.WaitForTransformable(reticule);

            attackerUnit.CanMove.Value = false;
            defenderUnit.UpdateUnit(defenderStats);
            controller.Players[defenderUnit.OwnerID!.Value].UnitValue.Value -= defenderValue;

            if (defenderUnit.HealthPoints.Value <= 0 || !defenderCounters)
            {
                attackerUnit.UpdateUnit(attackerStats);
                if (defenderUnit.HealthPoints.Value <= 0)
                    controller.Map.DeleteUnit(defenderUnit.UnitID, true);
                controller.Players[attackerUnit.OwnerID!.Value].UnitValue.Value -= attackerValue;
                afterAttackChanges(controller);

                //Destroying a unit can eliminate a player. i.e. They have no units left.
                if (EliminatedAction != null)
                {
                    foreach (var transformable in EliminatedAction.PerformAction(controller))
                        yield return transformable;
                }
                yield break;
            }

            //Perform Attack vs Attacker
            reticule = PlayAttackAnimation(controller, defenderUnit.MapPosition, attackerUnit.MapPosition, defenderUnit, true);
            yield return ReplayWait.WaitForTransformable(reticule);

            attackerUnit.UpdateUnit(attackerStats);

            if (attackerUnit.HealthPoints.Value <= 0)
            {
                controller.Map.DeleteUnit(attackerUnit.UnitID, true);
                controller.UpdateFogOfWar();
            }

            controller.Players[attackerUnit.OwnerID!.Value].UnitValue.Value -= attackerValue;
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

            //Indirect attacks cannot be countered.
            if (distance > 1)
                return false;

            if (distance < defendingUnit.UnitData.AttackRange.X || distance > defendingUnit.UnitData.AttackRange.Y)
                return false;

            if (defendingUnit.UnitData.SecondWeapon)
                return true;

            return defendingUnit.UnitData.MaxAmmo <= 0 || defendingUnit.Ammo.Value > 0;
        }

        private void afterAttackChanges(ReplayController controller)
        {
            if ((Attacker.HitPoints ?? 0) <= 0 || (Defender.HitPoints ?? 0f) <= 0)
                controller.UpdateFogOfWar();

            ReplayActionHelper.AdjustStatReadoutsFromUnitList(controller, controller.ActivePlayer.ID, originalUnits.Values, false);

            //Note: All transports can't attack, so there should only ever be one unit here.
            controller.Stats.CurrentTurnStatsReadout[originalDefender.PlayerID!.Value].RegisterUnitStats(UnitStatType.DamageUnit, originalAttacker.UnitName, originalAttacker.PlayerID!.Value, attackerValueLost);

            if (GainedFunds != null)
            {
                foreach (var (playerID, funds) in GainedFunds)
                    controller.Players[playerID].Funds.Value += funds;
            }

            foreach (var building in buildingsHP)
            {
                if (controller.Map.TryGetDrawableBuilding(building.Key, out var drawableBuilding))
                    drawableBuilding.CaptureHealth.Value = 20;
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

        public EffectAnimation PlayAttackAnimation(ReplayController controller, Vector2I start, Vector2I end, DrawableUnit attacker, bool counterAttack)
        {
            var scale = counterAttack ? 0.75f : 1f;
            var lengthModifier = counterAttack ? 0.66f : 0.9f;

            var effect = controller.Map.PlayEffect("Effects/Target", 600 * lengthModifier, start, 0, x =>
            {
                x.WaitForTransformationToComplete(attacker)
                 .MoveTo(GameMap.GetDrawablePositionForBottomOfTile(start) + DrawableTile.HALF_BASE_SIZE).FadeTo(0.5f).ScaleTo(0.5f * scale)
                 .FadeTo(1, 250 * lengthModifier, Easing.In).MoveTo(GameMap.GetDrawablePositionForBottomOfTile(end) + DrawableTile.HALF_BASE_SIZE, 400 * lengthModifier, Easing.In)
                 .ScaleTo(scale, 600 * lengthModifier, Easing.OutBounce).RotateTo(180, 400 * lengthModifier).Then().Expire();
            });

            return effect;
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Attack Action.");
            ReplayActionHelper.AdjustStatReadoutsFromUnitList(controller, controller.ActivePlayer.ID, originalUnits.Values, true);

            //Note: All transports can't attack, so there should only ever be one unit here.
            controller.Stats.CurrentTurnStatsReadout[originalDefender.PlayerID!.Value].RegisterUnitStats(UnitStatType.DamageUnit | UnitStatType.Undo, originalAttacker.UnitName, originalAttacker.PlayerID!.Value, attackerValueLost);

            foreach (var originalUnit in originalUnits)
            {
                if (controller.Map.TryGetDrawableUnit(originalUnit.Key, out var drawableUnit))
                    drawableUnit.UpdateUnit(originalUnit.Value);
                else
                    controller.Map.AddUnit(originalUnit.Value);
            }

            controller.Players[originalAttacker.PlayerID!.Value].UnitValue.Value += attackerValueLost;
            controller.Players[originalDefender.PlayerID!.Value].UnitValue.Value += defenderValueLost;

            foreach (var power in originalPowers)
            {
                var value = controller.Players[power.Key].ActiveCO.Value;
                value.Power = power.Value;
                controller.Players[power.Key].ActiveCO.Value = value;
            }

            foreach (var building in buildingsHP)
            {
                if (controller.Map.TryGetDrawableBuilding(building.Key, out var drawableBuilding))
                    drawableBuilding.CaptureHealth.Value = building.Value;
            }

            if (originalFunds != null)
            {
                foreach (var funds in originalFunds)
                    controller.Players[funds.Key].Funds.Value = funds.Value;
            }

            if (MoveUnit != null)
                MoveUnit.UndoAction(controller);
            else
            {
                controller.Map.GetDrawableUnit(originalAttacker.ID).CanMove.Value = true;
                controller.UpdateFogOfWar();
            }
        }

        public class COPowerChange
        {
            public long PlayerID;
            public int PowerChange;
            public int? TagPowerChange;
        }
    }
}
