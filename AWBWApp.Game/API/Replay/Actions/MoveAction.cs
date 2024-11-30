﻿using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;
using osuTK;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class MoveActionBuilder : IReplayActionBuilder
    {
        public string Code => "Move";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new MoveUnitAction();

            var unit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.Unit = ReplayActionHelper.ParseJObjectIntoReplayUnit(unit);

            var path = (JArray)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["paths"], turnData.ActiveTeam, turnData.ActivePlayerID);

            action.Path = new UnitPosition[path.Count];

            for (int i = 0; i < path.Count; i++)
            {
                var pathPart = (JObject)path[i];
                var position = new UnitPosition
                {
                    X = (int)pathPart["x"],
                    Y = (int)pathPart["y"],
                    UnitVisible = (bool)pathPart["unit_visible"]
                };
                action.Path[i] = position;
            }
            action.Distance = ((int?)jObject["dist"]) ?? 0;
            action.Trapped = (bool)jObject["trapped"];

            if (jObject.TryGetValue("discovered", out var discovered))
            {
                var collection = new DiscoveryCollection(discovered);
                if (!collection.IsEmpty())
                    action.Discovered = collection;
            }

            return action;
        }
    }

    public class MoveUnitAction : IReplayAction
    {
        public bool SuccessfullySetup { get; set; }

        public ReplayUnit Unit;
        public int Distance { get; set; }
        public UnitPosition[] Path { get; set; }
        public bool Trapped { get; set; }

        public DiscoveryCollection Discovered;

        private ReplayUnit originalUnit;
        private int? belowBuildingHP;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return "Move";

            if (controller.Map.TryGetDrawableUnit(Unit.ID, out var moveUnit))
                return $"{moveUnit.UnitData.Name} Moves";

            return $"{Unit.UnitName} Moves";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            if (!context.Units.TryGetValue(Unit.ID, out var unit))
                throw new ReplayMissingUnitException(Unit.ID);

            originalUnit = unit.Clone();
            unit.Overwrite(Unit);

            if (context.Buildings.TryGetValue(originalUnit.Position!.Value, out var belowBuilding) && belowBuilding.Capture != 20)
            {
                belowBuildingHP = belowBuilding.Capture;
                belowBuilding.Capture = 20;
            }

            if (Discovered != null)
                context.RegisterDiscoveryAndSetUndo(Discovered);
            ReplayActionHelper.UpdateUnitCargoPositions(context, unit, unit.Position!.Value);
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            bool isAirUnit;
            if (controller.Map.TryGetDrawableUnit(Unit.ID, out var drawableUnit))
                isAirUnit = drawableUnit.UnitData.MovementType == MovementType.Air;
            else
                isAirUnit = controller.Map.GetUnitDataForUnitName(Unit.UnitName).MovementType == MovementType.Air;

            foreach (var position in Path)
            {
                if (controller.ShouldPlayerActionBeHidden(new Vector2I(position.X, position.Y), isAirUnit))
                    continue;

                return true;
            }

            return false;
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Move Action.");

            if (!Unit.Position.HasValue)
                throw new Exception("Improperly made Move Unit Action. Final outcome missing position.");

            var unit = controller.Map.GetDrawableUnit(Unit.ID);
            var isAirUnit = unit.UnitData.MovementType == MovementType.Air;

            if (belowBuildingHP != null)
            {
                if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out var drawableBuilding))
                    drawableBuilding.CaptureHealth.Value = 20;
                unit.IsCapturing.Value = false;
            }

            EffectAnimation effect = null;
            if (controller.ShowAnimationsWhenUnitsHidden.Value || !controller.ShouldPlayerActionBeHidden(unit.MapPosition, isAirUnit))
                effect = controller.Map.PlaySelectionAnimation(unit);

            if (controller.ShowMovementArrows)
            {
                if (Path.Length > 1)
                    renderPath(Path, controller, isAirUnit);
            }

            if (effect != null)
                yield return ReplayWait.WaitForTransformable(effect);

            if (Path.Length > 1)
            {
                unit.FollowPath(controller, Path);
                yield return ReplayWait.WaitForTransformable(unit);
            }

            unit.UpdateUnit(Unit, true);

            unit.CanMove.Value = false;

            controller.UpdateFogOfWar();
            if (Discovered != null)
                controller.Map.RegisterDiscovery(Discovered);

            if (Trapped)
                controller.Map.PlayEffect("Effects/TrapMarker", 650, Unit.Position.Value, 0, x => x.ScaleTo(new Vector2(1, 0)).ScaleTo(new Vector2(1, 1), 250, Easing.OutBounce));
        }

        private void renderPath(UnitPosition[] path, ReplayController controller, bool isAirUnit)
        {
            if (path.Length <= 1)
                throw new Exception("Did we travel a path of only 1 tile?");

            //Skip the zeroth index as we don't want to show an arrow over the top of the unit.
            for (int i = 1; i < path.Length - 1; i++)
            {
                var current = path[i];

                if (controller.ShouldPlayerActionBeHidden(new Vector2I(current.X, current.Y), isAirUnit))
                    continue;

                var prev = path[i - 1];
                var next = path[i + 1];

                var diffX = next.X - prev.X;
                var diffY = next.Y - prev.Y;

                var delay = (i - 1) * 25;

                if (Math.Abs(diffX) >= 2)
                    createArrowPiece(controller, "UI/Arrow_Body", new Vector2I(current.X, current.Y), delay, diffX > 0 ? 90 : -90);
                else if (Math.Abs(diffY) >= 2)
                    createArrowPiece(controller, "UI/Arrow_Body", new Vector2I(current.X, current.Y), delay, diffY > 0 ? 180 : 0);
                else
                {
                    var prevToCurrentX = current.X - prev.X;
                    var prevToCurrentY = current.Y - prev.Y;

                    if (prevToCurrentX > 0)
                        createArrowPiece(controller, "UI/Arrow_Curved", new Vector2I(current.X, current.Y), delay, diffY > 0 ? -90 : 0);
                    else if (prevToCurrentX < 0)
                        createArrowPiece(controller, "UI/Arrow_Curved", new Vector2I(current.X, current.Y), delay, diffY > 0 ? 180 : 90);
                    else if (prevToCurrentY > 0)
                        createArrowPiece(controller, "UI/Arrow_Curved", new Vector2I(current.X, current.Y), delay, diffX > 0 ? 90 : 0);
                    else
                        createArrowPiece(controller, "UI/Arrow_Curved", new Vector2I(current.X, current.Y), delay, diffX > 0 ? 180 : -90);
                }
            }

            var beforeHead = path[^2];
            var head = path[^1];

            if (controller.ShouldPlayerActionBeHidden(new Vector2I(head.X, head.Y), isAirUnit))
                return;

            var headDiffX = head.X - beforeHead.X;
            var headDiffY = head.Y - beforeHead.Y;

            if (headDiffX > 0)
                createArrowPiece(controller, "UI/Arrow_Tip", new Vector2I(head.X, head.Y), (path.Length - 2) * 25, -90);
            else if (headDiffX < 0)
                createArrowPiece(controller, "UI/Arrow_Tip", new Vector2I(head.X, head.Y), (path.Length - 2) * 25, 90);
            else if (headDiffY > 0)
                createArrowPiece(controller, "UI/Arrow_Tip", new Vector2I(head.X, head.Y), (path.Length - 2) * 25, 0);
            else
                createArrowPiece(controller, "UI/Arrow_Tip", new Vector2I(head.X, head.Y), (path.Length - 2) * 25, 180);
        }

        private void createArrowPiece(ReplayController controller, string type, Vector2I position, int delay, float rotation)
        {
            controller.Map.PlayEffect(type, 250, position, delay, x =>
            {
                x.RotateTo(rotation).ScaleTo(0.8f).ScaleTo(1f, 75, Easing.OutQuint);
            });
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Move Action.");

            var unit = controller.Map.GetDrawableUnit(Unit.ID);
            unit.UpdateUnit(originalUnit, true);

            if (belowBuildingHP != null)
            {
                if (controller.Map.TryGetDrawableBuilding(unit.MapPosition, out var drawableBuilding))
                    drawableBuilding.CaptureHealth.Value = belowBuildingHP.Value;
                unit.IsCapturing.Value = true;
            }

            if (Discovered != null)
                controller.Map.UndoDiscovery(Discovered);
            controller.UpdateFogOfWar();
        }
    }
}
