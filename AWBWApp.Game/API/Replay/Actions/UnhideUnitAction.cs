using System;
using System.Collections.Generic;
using AWBWApp.Game.Exceptions;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class UnhideUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Unhide";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new UnhideUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Unhide action was expecting a movement action.");
            }

            var hideData = (JObject)jObject["Unhide"];
            if (hideData == null)
                throw new Exception("Unhide Replay Action did not contain information about Unhide.");

            action.RevealingUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)hideData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID));
            return action;
        }
    }

    public class UnhideUnitAction : IReplayAction
    {
        public ReplayUnit RevealingUnit { get; set; }

        public MoveUnitAction MoveUnit;

        private ReplayUnit originalUnit;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return MoveUnit != null ? "Move + Unhides" : "Unhides";

            var revealedUnit = controller.Map.GetDrawableUnit(RevealingUnit.ID);

            if (MoveUnit == null)
                return $"{revealedUnit.UnitData.Name} Unhides";

            return $"{revealedUnit.UnitData.Name} Moves + Unhides";
        }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            MoveUnit?.SetupAndUpdate(controller, context);

            if (!context.Units.TryGetValue(RevealingUnit.ID, out var unit))
                throw new ReplayMissingUnitException(RevealingUnit.ID);

            originalUnit = unit.Clone();
            unit.Overwrite(RevealingUnit);
        }

        public bool HasVisibleAction(ReplayController controller)
        {
            if (MoveUnit != null && MoveUnit.HasVisibleAction(controller))
                return true;

            return !controller.ShouldPlayerActionBeHidden(originalUnit);
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Unhide Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var hiddenUnit = controller.Map.GetDrawableUnit(RevealingUnit.ID);
            hiddenUnit.UpdateUnit(RevealingUnit);
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Unhide Action.");

            var hiddenUnit = controller.Map.GetDrawableUnit(originalUnit.ID);
            hiddenUnit.UpdateUnit(originalUnit);

            MoveUnit?.UndoAction(controller);
        }
    }
}
