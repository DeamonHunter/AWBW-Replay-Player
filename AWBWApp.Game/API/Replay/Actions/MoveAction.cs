using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
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
            action.Distance = (int)jObject["dist"];
            action.Trapped = (bool)jObject["trapped"];

            return action;
        }
    }

    public class MoveUnitAction : IReplayAction
    {
        public string ReadibleName => "Move";

        public ReplayUnit Unit;
        public int Distance { get; set; }
        public UnitPosition[] Path { get; set; }
        public bool Trapped { get; set; }

        private ReplayUnit originalUnit;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            if (!context.Units.TryGetValue(Unit.ID, out var unit))
                throw new ReplayMissingUnitException(Unit.ID);

            originalUnit = unit.Clone();
            unit.Overwrite(Unit);

            ReplayActionHelper.UpdateUnitCargoPositions(context, unit, unit.Position!.Value);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            if (!Unit.Position.HasValue)
                throw new Exception("Improperly made Move Unit Action. Final outcome missing position.");

            Logger.Log("Performing Move Action.");
            var unit = controller.Map.GetDrawableUnit(Unit.ID);
            unit.IsCapturing.Value = false;

            var effect = controller.Map.PlaySelectionAnimation(unit);
            if (Path.Length > 1)
                renderPath(Path, controller);

            yield return ReplayWait.WaitForTransformable(effect);

            if (Path.Length > 1)
            {
                unit.FollowPath(Path);
                yield return ReplayWait.WaitForTransformable(unit);
            }

            unit.MoveToPosition(Unit.Position.Value);

            if (unit.Cargo != null && unit.Cargo.Count > 0)
            {
                foreach (var carriedUnit in unit.Cargo)
                    controller.Map.GetDrawableUnit(carriedUnit).MoveToPosition(Unit.Position.Value);
            }

            unit.CanMove.Value = false;
            unit.CheckForDesyncs(Unit);
            controller.UpdateFogOfWar();

            if (Trapped)
                controller.Map.PlayEffect("Effects/TrapMarker", 650, Unit.Position.Value, 0, x => x.ScaleTo(new Vector2(1, 0)).ScaleTo(new Vector2(1, 1), 250, Easing.OutBounce));
        }

        private void renderPath(UnitPosition[] path, ReplayController controller)
        {
            if (path.Length <= 1)
                throw new Exception("Did we travel a path of only 1 tile?");

            //Skip the zeroth index as we don't want to show an arrow over the top of the unit.
            for (int i = 1; i < path.Length - 1; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
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
            controller.Map.GetDrawableUnit(Unit.ID).UpdateUnit(originalUnit);
            controller.UpdateFogOfWar();
        }
    }
}
