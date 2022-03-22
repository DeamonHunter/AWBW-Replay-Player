using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
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

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var unit = (JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)jObject["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);

            action.UnloadedUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit(unit);
            action.TransportID = (long)jObject["transportID"];
            return action;
        }
    }

    public class UnloadUnitAction : IReplayAction
    {
        public string ReadibleName => "Unload";

        public MoveUnitAction MoveUnit;

        public long TransportID { get; set; }
        public ReplayUnit UnloadedUnit { get; set; }

        private ReplayUnit originalLoadedUnit;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.TryGetValue(UnloadedUnit.ID, out var unloadedUnit))
                throw new ReplayMissingUnitException(UnloadedUnit.ID);

            if (!context.Units.TryGetValue(TransportID, out var transportUnit))
                throw new ReplayMissingUnitException(TransportID);

            originalLoadedUnit = unloadedUnit.Clone();
            unloadedUnit.Overwrite(UnloadedUnit);
            transportUnit.CargoUnits?.Remove(UnloadedUnit.ID);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Unload Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var transportUnit = controller.Map.GetDrawableUnit(TransportID);
            var unloadingUnit = controller.Map.GetDrawableUnit(UnloadedUnit.ID);

            unloadingUnit.BeingCarried.Value = false;
            transportUnit.Cargo.Remove(unloadingUnit.UnitID);

            unloadingUnit.FollowPath(new List<UnitPosition>
            {
                new UnitPosition(transportUnit.MapPosition),
                new UnitPosition(UnloadedUnit.Position!.Value)
            });

            yield return ReplayWait.WaitForTransformable(unloadingUnit);

            unloadingUnit.MoveToPosition(UnloadedUnit.Position.Value);
            unloadingUnit.CanMove.Value = false;
            controller.UpdateFogOfWar();
        }

        public void UndoAction(ReplayController controller)
        {
            var unloadingUnit = controller.Map.GetDrawableUnit(UnloadedUnit.ID);
            var transportUnit = controller.Map.GetDrawableUnit(TransportID);

            unloadingUnit.BeingCarried.Value = true;
            transportUnit.Cargo ??= new HashSet<long>();
            transportUnit.Cargo.Remove(unloadingUnit.UnitID);

            MoveUnit?.UndoAction(controller);
        }
    }
}
