using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

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
                var position = new UnitPosition();
                position.X = (int)pathPart["x"];
                position.Y = (int)pathPart["y"];
                position.Unit_Visible = (bool)pathPart["unit_visible"];
                action.Path[i] = position;
            }
            action.Distance = (int)jObject["dist"];
            action.Trapped = (bool)jObject["trapped"];

            Logger.Log("Missing Fog Parse.");

            return action;
        }
    }

    public class MoveUnitAction : IReplayAction
    {
        public ReplayUnit Unit;
        public int Distance { get; set; }
        public UnitPosition[] Path { get; set; }
        public bool Trapped { get; set; }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Move Action.");
            var unit = controller.Map.GetDrawableUnit(Unit.ID);

            controller.Map.SelectionReticule.PlaySelectAnimation(unit);
            if (Path.Length > 1)
                renderPath(Path, controller);

            yield return ReplayWait.WaitForTransformable(controller.Map.SelectionReticule);

            if (Path.Length > 1)
            {
                unit.FollowPath(Path);
                yield return ReplayWait.WaitForTransformable(unit);
            }

            unit.MoveToPosition(Unit.Position.Value);
            unit.CanMove.Value = false;
            unit.CheckForDesyncs(Unit);
            controller.UpdateFogOfWar();
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
                    controller.Map.PlayEffect("UI/Arrow_Body", 250, new Vector2I(current.X, current.Y), delay, diffX > 0 ? 90 : -90);
                else if (Math.Abs(diffY) >= 2)
                    controller.Map.PlayEffect("UI/Arrow_Body", 250, new Vector2I(current.X, current.Y), delay, diffY > 0 ? 180 : 0);
                else
                {
                    var prevToCurrentX = current.X - prev.X;
                    var prevToCurrentY = current.Y - prev.Y;

                    if (prevToCurrentX > 0)
                        controller.Map.PlayEffect("UI/Arrow_Curved", 250, new Vector2I(current.X, current.Y), delay, diffY > 0 ? -90 : 0);
                    else if (prevToCurrentX < 0)
                        controller.Map.PlayEffect("UI/Arrow_Curved", 250, new Vector2I(current.X, current.Y), delay, diffY > 0 ? 180 : 90);
                    else if (prevToCurrentY > 0)
                        controller.Map.PlayEffect("UI/Arrow_Curved", 250, new Vector2I(current.X, current.Y), delay, diffX > 0 ? 90 : 0);
                    else
                        controller.Map.PlayEffect("UI/Arrow_Curved", 250, new Vector2I(current.X, current.Y), delay, diffX > 0 ? 180 : -90);
                }
            }

            var beforeHead = path[^2];
            var head = path[^1];

            var headDiffX = head.X - beforeHead.X;
            var headDiffY = head.Y - beforeHead.Y;

            if (headDiffX > 0)
                controller.Map.PlayEffect("UI/Arrow_Tip", 250, new Vector2I(head.X, head.Y), (path.Length - 2) * 25, -90);
            else if (headDiffX < 0)
                controller.Map.PlayEffect("UI/Arrow_Tip", 250, new Vector2I(head.X, head.Y), (path.Length - 2) * 25, 90);
            else if (headDiffY > 0)
                controller.Map.PlayEffect("UI/Arrow_Tip", 250, new Vector2I(head.X, head.Y), (path.Length - 2) * 25, 0);
            else
                controller.Map.PlayEffect("UI/Arrow_Tip", 250, new Vector2I(head.X, head.Y), (path.Length - 2) * 25, 180);
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Move Action.");
            var unit = controller.Map.GetDrawableUnit(Unit.ID);

            var transformSequence = unit.FollowPath(Path, true);
            transformSequence.Finally(x =>
            {
                unit.MoveToPosition(new Vector2I(Path[0].X, Path[0].Y));
                unit.CanMove.Value = true;

                unit.CheckForDesyncs(Unit);
            });

            if (immediate)
                unit.FinishTransforms();
        }
    }
}
