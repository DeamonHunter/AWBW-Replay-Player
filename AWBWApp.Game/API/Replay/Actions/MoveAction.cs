using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
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

            var unit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["unit"], turnData.PlayerID.ToString());
            action.Unit = ReplayActionHelper.ParseJObjectIntoReplayUnit(unit);

            var path = (JArray)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["paths"], turnData.PlayerID.ToString());

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

        public List<Transformable> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Move Action.");
            var unit = controller.Map.GetDrawableUnit(Unit.ID);

            var animations = new List<Transformable>();

            var sequence = unit.FollowPath(Path);
            sequence.OnComplete(x =>
            {
                unit.MoveToPosition(Unit.Position.Value);
                unit.CanMove.Value = false;
                unit.CheckForDesyncs(Unit);
            });

            animations.Add(unit);
            return animations;
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
