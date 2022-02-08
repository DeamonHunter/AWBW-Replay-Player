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
    public class ExplodeUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Explode";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new ExplodeUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var explodeData = (JObject)jObject["Explode"];
            if (explodeData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            action.HPChange = (float)explodeData["hp"];
            action.ExplodedUnitId = (int)explodeData["unitId"];
            return action;
        }
    }

    public class ExplodeUnitAction : IReplayAction
    {
        public long ExplodedUnitId { get; set; }

        public float HPChange { get; set; }

        public MoveUnitAction MoveUnit;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Explode Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var explodingUnit = controller.Map.GetDrawableUnit(ExplodedUnitId);
            if (explodingUnit.OwnerID == null)
                throw new Exception("A bomb without an owner exploded?");

            //Todo: Bigger explosion?
            controller.Map.DeleteUnit(ExplodedUnitId, false);
            var effect = controller.Map.PlayEffect("Effects/Explosion/Explosion-Land", 500, explodingUnit.MapPosition + new Vector2I(0, -1));
            effect.ScaleTo(3);

            for (int i = 1; i <= 3; i++)
            {
                foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(explodingUnit.MapPosition, i))
                {
                    if (controller.Map.TryGetDrawableUnit(tile, out var unit))
                    {
                        if (!unit.OwnerID.HasValue || controller.Map.Players[explodingUnit.OwnerID.Value].Team == controller.Map.Players[unit.OwnerID.Value].Team)
                            continue;

                        unit.HealthPoints.Value += (int)HPChange;
                        if (unit.HealthPoints.Value <= 0)
                            controller.Map.DeleteUnit(unit.UnitID, true);
                    }
                }
                yield return ReplayWait.WaitForMilliseconds(100);
            }
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
