using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
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
        public string ReadibleName => "Hide";

        public long HidingUnitID { get; set; }

        public MoveUnitAction MoveUnit;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Hide Action.");
            Logger.Log("Todo: Play transition effect.");

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
            throw new NotImplementedException("Undo Hide Action is not complete");
        }
    }
}
