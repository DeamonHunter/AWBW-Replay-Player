using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
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
                action.Attacker = action.MoveUnit.Unit;
                action.Attacker.HitPoints = 0;
            }
            else
                action.Attacker = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)combatInfo["attacker"]);

            action.Defender = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)combatInfo["defender"]);

            action.COPChanges = ReplayActionHelper.ParseJObjectIntoAttackCOPChange((JObject)attackData["copValues"]);

            return action;
        }
    }

    public class AttackUnitAction : IReplayAction
    {
        public ReplayUnit Attacker { get; set; }
        public ReplayUnit Defender { get; set; }
        public ReplayAttackCOPChange COPChanges { get; set; }
        public long? GainedFunds { get; set; }

        public MoveUnitAction MoveUnit;

        public List<Transformable> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Attack Action.");
            Logger.Log("Attack animation not implemented.");

            var attackerUnit = controller.Map.GetDrawableUnit(Attacker.ID);
            var defenderUnit = controller.Map.GetDrawableUnit(Defender.ID);

            List<Transformable> transformables;

            Vector2I attackerPosition;

            if (MoveUnit != null)
            {
                transformables = MoveUnit.PerformAction(controller);
                attackerPosition = MoveUnit.Unit.Position.Value;
            }
            else
            {
                transformables = new List<Transformable>();
                attackerPosition = attackerUnit.MapPosition;
            }

            controller.Map.TargetReticule.WaitForTransformationToComplete(attackerUnit);
            var sequence = controller.Map.TargetReticule.PlayAttackAnimation(attackerPosition, defenderUnit.MapPosition, attackerUnit);
            sequence.OnComplete(x =>
            {
                defenderUnit.UpdateUnit(Defender);
                attackerUnit.CanMove.Value = false;

                if (defenderUnit.HealthPoints.Value <= 0)
                {
                    attackerUnit.UpdateUnit(Attacker);
                    controller.Map.DestroyUnit(defenderUnit.UnitID);
                }
            });

            if (defenderUnit.HealthPoints.Value >= 0)
            {
                sequence = controller.Map.TargetReticule.PlayAttackAnimation(defenderUnit.MapPosition, attackerPosition, defenderUnit);
                sequence.OnComplete(x =>
                {
                    attackerUnit.UpdateUnit(Attacker);

                    if (attackerUnit.HealthPoints.Value <= 0)
                    {
                        controller.Map.DestroyUnit(attackerUnit.UnitID);
                        controller.UpdateFogOfWar();
                    }
                });
            }
            transformables.Add(controller.Map.TargetReticule);
            return transformables;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
