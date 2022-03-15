using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class UnloadUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Unload";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new UnloadUnitAction();

            var unit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            action.UnloadedUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit(unit);
            action.TransportID = (long)jObject["transportID"];
            return action;
        }
    }

    public class UnloadUnitAction : IReplayAction
    {
        public string ReadibleName => "Unload";

        public long TransportID { get; set; }
        public ReplayUnit UnloadedUnit { get; set; }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Unload Action.");
            var transportUnit = controller.Map.GetDrawableUnit(TransportID);
            var unloadingUnit = controller.Map.GetDrawableUnit(UnloadedUnit.ID);

            unloadingUnit.BeingCarried.Value = false;
            transportUnit.Cargo.Remove(unloadingUnit.UnitID);

            unloadingUnit.FollowPath(new List<UnitPosition>
            {
                new UnitPosition { UnitVisible = true, X = transportUnit.MapPosition.X, Y = transportUnit.MapPosition.Y },
                new UnitPosition { UnitVisible = true, X = UnloadedUnit.Position.Value.X, Y = UnloadedUnit.Position.Value.Y },
            });
            yield return ReplayWait.WaitForTransformable(unloadingUnit);

            unloadingUnit.MoveToPosition(UnloadedUnit.Position.Value);
            unloadingUnit.CanMove.Value = false;
            controller.UpdateFogOfWar();
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Unload Action is not complete");
        }
    }
}
