using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class DeleteUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Delete";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new DeleteUnitAction();

            var deleteData = (JObject)jObject["Delete"];
            if (deleteData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Delete action was expecting a movement action.");
            }

            action.DeletedUnitId = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)deleteData["unitId"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class DeleteUnitAction : IReplayAction
    {
        public MoveUnitAction MoveUnit;

        public long DeletedUnitId { get; set; }

        private readonly Dictionary<long, ReplayUnit> originalUnits = new Dictionary<long, ReplayUnit>();
        private int unitValue;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Delete" : "Delete";

            if (MoveUnit == null)
                return $"{originalUnits[DeletedUnitId].UnitName} Deleted";

            return $"{originalUnits[DeletedUnitId].UnitName} Moves + Deleted";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            context.RemoveUnitFromSetupContext(DeletedUnitId, originalUnits, out unitValue);
            context.AdjustStatsToPlayerAction(context.ActivePlayerID, originalUnits.Values);
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            return !controller.ShouldPlayerActionBeHidden(originalUnits[0].Position!.Value);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Delete Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            controller.Map.DeleteUnit(DeletedUnitId, true);
            ReplayActionHelper.AdjustStatsToPlayerAction(controller, originalUnits[DeletedUnitId].PlayerID!.Value, originalUnits.Values, false);
            controller.ActivePlayer.UnitValue.Value -= unitValue;
            controller.UpdateFogOfWar();
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Delete Action.");
            foreach (var unit in originalUnits)
                controller.Map.AddUnit(unit.Value);

            ReplayActionHelper.AdjustStatsToPlayerAction(controller, originalUnits[DeletedUnitId].PlayerID!.Value, originalUnits.Values, true);
            controller.ActivePlayer.UnitValue.Value += unitValue;

            controller.UpdateFogOfWar();
            MoveUnit?.UndoAction(controller);
        }
    }
}
