using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class LoadUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Load";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new LoadUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var loadData = (JObject)jObject["Load"];
            if (loadData == null)
                throw new Exception("Load Replay Action did not contain information about Load.");

            action.LoadedID = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)loadData["loaded"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.TransportID = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)loadData["transport"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class LoadUnitAction : IReplayAction
    {
        public string ReadibleName => "Load";

        public long LoadedID { get; set; }
        public long TransportID { get; set; }

        public MoveUnitAction MoveUnit;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Supply Action.");
            Logger.Log("Load animation not completed.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var loadingUnit = controller.Map.GetDrawableUnit(LoadedID);
            var transportUnit = controller.Map.GetDrawableUnit(TransportID);

            loadingUnit.BeingCarried.Value = true;
            loadingUnit.IsCapturing.Value = false;
            transportUnit.Cargo.Add(loadingUnit.UnitID);
        }

        public void UndoAction(ReplayController controller)
        {
            throw new NotImplementedException("Undo Load Action is not complete");
        }
    }
}
