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

            if (jObject.TryGetValue("discovered", out var discovered))
            {
                var collection = new DiscoveryCollection(discovered);
                if (!collection.IsEmpty())
                    action.Discovered = collection;
            }

            return action;
        }
    }

    public class UnloadUnitAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;

        public long TransportID { get; set; }
        public ReplayUnit UnloadedUnit { get; set; }

        public DiscoveryCollection Discovered;

        private ReplayUnit originalLoadedUnit;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Delete" : "Delete";

            var unloadedUnit = controller.Map.GetDrawableUnit(UnloadedUnit.ID);
            var transportUnit = controller.Map.GetDrawableUnit(TransportID);

            if (MoveUnit == null)
                return $"{transportUnit.UnitData.Name} Unloads {unloadedUnit.UnitData.Name}";

            return $"{transportUnit.UnitData.Name} Moves + Unloads {unloadedUnit.UnitData.Name}";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.TryGetValue(UnloadedUnit.ID, out var unloadedUnit))
                throw new ReplayMissingUnitException(UnloadedUnit.ID);

            if (!context.Units.TryGetValue(TransportID, out var transportUnit))
                throw new ReplayMissingUnitException(TransportID);

            originalLoadedUnit = unloadedUnit.Clone();
            unloadedUnit.Overwrite(UnloadedUnit);
            ReplayActionHelper.UpdateUnitCargoPositions(context, unloadedUnit, unloadedUnit.Position!.Value);
            transportUnit.CargoUnits?.Remove(UnloadedUnit.ID);
            if (Discovered != null)
                context.RegisterDiscoveryAndSetUndo(Discovered);
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            return !controller.ShouldPlayerActionBeHidden(originalLoadedUnit) || !controller.ShouldPlayerActionBeHidden(UnloadedUnit);
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

            yield return ReplayWait.WaitForMilliseconds(150);

            unloadingUnit.FollowPath(controller, new List<UnitPosition>
            {
                new UnitPosition(transportUnit.MapPosition),
                new UnitPosition(UnloadedUnit.Position!.Value)
            });

            yield return ReplayWait.WaitForTransformable(unloadingUnit);

            unloadingUnit.MoveToPosition(UnloadedUnit.Position.Value);
            unloadingUnit.CanMove.Value = false;

            controller.UpdateFogOfWar();
            if (Discovered != null)
                controller.Map.RegisterDiscovery(Discovered);
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Unload Action.");

            var unloadingUnit = controller.Map.GetDrawableUnit(UnloadedUnit.ID);
            var transportUnit = controller.Map.GetDrawableUnit(TransportID);

            unloadingUnit.BeingCarried.Value = true;
            transportUnit.Cargo ??= new HashSet<long>();
            transportUnit.Cargo.Remove(unloadingUnit.UnitID);

            if (Discovered != null)
                controller.Map.UndoDiscovery(Discovered);
            controller.UpdateFogOfWar();
            MoveUnit?.UndoAction(controller);
        }
    }
}
