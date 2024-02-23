using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using OpenTabletDriver.Plugin;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics.ES11;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class AttackSeamActionBuilder : IReplayActionBuilder
    {
        public string Code => "AttackSeam";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new AttackSeamAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Attack Seam action was expecting a movement action.");
            }
            
            var attackSeamData = (JObject)jObject["AttackSeam"];
            action.Seam = ReplayActionHelper.ParseJObjectIntoReplaySeam((JObject)attackSeamData);

            if (attackSeamData == null)
                throw new Exception("Attack Seam Replay Action did not contain information about Seam.");

            //Convoluted way to find attacker Units ID, which is used to find the Unit itself in SetupAndUpdate()
            var combatInfoVision = attackSeamData["unit"];
            var attackerInfoVisionData = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)combatInfoVision, turnData.ActiveTeam, turnData.ActivePlayerID);
            action.AttackerCombatInfo = attackerInfoVisionData["combatInfo"];
            action.UnitID = (long) action.AttackerCombatInfo["units_id"];
            
            return action;
        }
    }

    public class AttackSeamAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;
        public ReplayBuilding Seam;

        private ReplayBuilding originalSeam;

        public long UnitID;
        public JToken AttackerCombatInfo;

        private ReplayUnit originalUnit;
        private ReplayUnit unit;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName || !controller.Map.TryGetDrawableBuilding(originalSeam.Position, out var building))
                return MoveUnit != null ? "Move + Attacks Seam" : "Attacks Seam";

            string moveUnitString;
            if (MoveUnit != null && controller.Map.TryGetDrawableUnit(MoveUnit.Unit.ID, out var moveUnit))
                moveUnitString = $"{moveUnit.UnitData.Name} Moves + ";
            else if (originalUnit != null)
                moveUnitString = $"{originalUnit.UnitName} ";
            else
                moveUnitString = "";

            var attackState = Seam.TerrainID != originalSeam.TerrainID ? "Destroys " : "Attacks ";

            return moveUnitString + attackState + building.BuildingTile.Name;
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            return !controller.ShouldPlayerActionBeHidden(Seam.Position, false);
        }


        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Buildings.TryGetValue(Seam.Position, out var seam))
                throw new ReplayMissingBuildingException(Seam.ID);

            originalSeam = seam.Clone();

            //Destroyed pipeseams seem to have different CP values depending on Fog and weather voth player have vision on it? idk man but sometimes it gets set to 20 and sometimes not
            //causes desync sadly
            if(Seam.TerrainID != originalSeam.TerrainID) {
                Seam.Capture = originalSeam.Capture;
            }
            Seam.ID = seam.ID;

            seam.Overwrite(Seam);

            if (!context.Units.TryGetValue(UnitID, out unit))
                throw new ReplayMissingUnitException(UnitID);

            originalUnit = unit.Clone();
            
            unit.TimesFired++;
            unit.Ammo = (int) AttackerCombatInfo["units_ammo"];
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Attack Seam Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var actionHidden = controller.ShouldPlayerActionBeHidden(Seam.Position, false);
            controller.Map.TryGetDrawableBuilding(Seam.Position, out var capturingBuilding);

            if (capturingBuilding != null && (controller.ShowAnimationsWhenUnitsHidden.Value || !actionHidden))
            {
                var anim = controller.Map.PlaySelectionAnimation(capturingBuilding);
                yield return ReplayWait.WaitForTransformable(anim);
            }

            controller.Map.UpdateBuilding(Seam, false); //This will set the unit above to be capturing

            var attackerUnit = controller.Map.GetDrawableUnit(UnitID);
            if (!attackerUnit.OwnerID.HasValue)
                throw new Exception("Attacking unit doesn't have an owner id?");
            attackerUnit.CanMove.Value = false;
        }


        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Attack Seam Action.");
            controller.Map.UpdateBuilding(originalSeam, true);
            if(Seam.TerrainID != originalSeam.TerrainID) {
                //HP of restored Seam has to be updated
                controller.Map.TryGetDrawableBuilding(Seam.Position, out var restoredSeam);
                restoredSeam.CaptureHealth.Value = originalSeam.Capture ?? 100;
            }

            if (MoveUnit != null)
                MoveUnit.UndoAction(controller);
            else {
                var attackerUnit = controller.Map.GetDrawableUnit(UnitID);
                if (!attackerUnit.OwnerID.HasValue)
                    throw new Exception("Attacking unit doesn't have an owner id?");
                attackerUnit.CanMove.Value = true;
                unit.Overwrite(originalUnit);
            }
        }
    }
}