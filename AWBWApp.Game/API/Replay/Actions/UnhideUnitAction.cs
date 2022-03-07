using System;
using System.Collections.Generic;
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

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Unhide Action.");
            Logger.Log("Todo: Play transition effect.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var hiddenUnit = controller.Map.GetDrawableUnit(RevealingUnit.ID);
            hiddenUnit.UpdateUnit(RevealingUnit);
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Unhide Action is not complete");
        }
    }
}
