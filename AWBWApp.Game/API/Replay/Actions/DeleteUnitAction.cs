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

            if (deleteData.ContainsKey("Move"))
                throw new Exception("Movement data in delete action.");

            action.DeletedUnitId = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)deleteData["unitId"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class DeleteUnitAction : IReplayAction
    {
        public long DeletedUnitId { get; set; }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Delete Action.");

            controller.Map.DeleteUnit(DeletedUnitId, true);
            yield break;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            Logger.Log("Undoing Capture Action.");
            throw new NotImplementedException("Undo Action for Capture Building is not complete");
            //controller.Map.DestroyUnit(NewUnit.ID, false, immediate);
        }
    }
}
