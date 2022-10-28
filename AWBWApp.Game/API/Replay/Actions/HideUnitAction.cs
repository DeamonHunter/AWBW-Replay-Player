using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class HideUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Hide";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new HideUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var hideData = (JObject)jObject["Hide"];
            if (hideData == null)
                throw new Exception("Hide Replay Action did not contain information about Hide.");

            action.HidingUnitID = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)hideData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class HideUnitAction : IReplayAction
    {
        public long HidingUnitID { get; set; }
        private Vector2I hidingUnitPosition;

        public MoveUnitAction MoveUnit;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Hide" : "Hide";

            var hidingUnit = controller.Map.GetDrawableUnit(HidingUnitID);

            if (MoveUnit == null)
                return $"{hidingUnit.UnitData.Name} Hides";

            return $"{hidingUnit.UnitData.Name} Moves + Hides";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.TryGetValue(HidingUnitID, out var unit))
                throw new ReplayMissingUnitException(HidingUnitID);

            unit.SubHasDived = true;
            hidingUnitPosition = unit.Position!.Value;
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            if (controller.Map.TryGetDrawableUnit(hidingUnitPosition, out var hidingUnit))
                return !controller.ShouldPlayerActionBeHidden(hidingUnitPosition, hidingUnit.UnitData.MovementType == MovementType.Air);

            return !controller.ShouldPlayerActionBeHidden(hidingUnitPosition, false);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Hide Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var hidingUnit = controller.Map.GetDrawableUnit(HidingUnitID);
            hidingUnit.Dived.Value = true;
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Hide Action.");
            var hidingUnit = controller.Map.GetDrawableUnit(HidingUnitID);
            hidingUnit.Dived.Value = false;

            MoveUnit?.UndoAction(controller);
        }
    }
}
