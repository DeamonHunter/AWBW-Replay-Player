using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class LaunchUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Launch";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new LaunchUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var launchData = (JObject)jObject["Launch"];
            if (launchData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            action.SiloPosition = new Vector2I((int)launchData["siloX"], (int)launchData["siloY"]);
            action.TargetPosition = new Vector2I((int)launchData["targetX"], (int)launchData["targetY"]);
            action.HPChange = (float)launchData["hp"];

            return action;
        }
    }

    public class LaunchUnitAction : IReplayAction
    {
        public string ReadibleName => "Launch";

        public Vector2I SiloPosition;
        public Vector2I TargetPosition;
        public float HPChange;

        public MoveUnitAction MoveUnit;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Launch Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            Logger.Log("Todo: Initial launch animation.");

            if (controller.Map.TryGetDrawableBuilding(SiloPosition, out var building))
            {
                var replayBuilding = new ReplayBuilding
                {
                    Position = SiloPosition,
                    TerrainID = 112, //Todo: Remove hard coding of this
                };

                controller.Map.UpdateBuilding(replayBuilding, false);
            }

            controller.Map.PlayEffect("Effects/Target", 1500, TargetPosition, 0, x =>
            {
                x.ScaleTo(10).ScaleTo(1, 1000, Easing.In)
                 .FadeTo(1, 500)
                 .RotateTo(0).RotateTo(90 * 4, 1200, Easing.Out).Then().Expire();
            });

            var explosion = controller.Map.PlayEffect("Effects/Explosion/Explosion-Land", 500, TargetPosition + new Vector2I(0, -1), 1350, x => x.ScaleTo(3));

            yield return ReplayWait.WaitForTransformable(explosion);

            for (int i = 1; i <= 3; i++)
            {
                foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(TargetPosition, i))
                {
                    if (controller.Map.TryGetDrawableUnit(tile, out var unit))
                    {
                        if (!unit.OwnerID.HasValue || controller.ActivePlayer.Team == controller.Players[unit.OwnerID.Value].Team)
                            continue;

                        unit.HealthPoints.Value += (int)HPChange;
                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DeleteUnit(unit.UnitID, true);
                    }
                }
                yield return ReplayWait.WaitForMilliseconds(100);
            }
        }

        public void UndoAction(ReplayController controller)
        {
            throw new NotImplementedException("Undo Launch Action is not complete");
        }
    }
}
