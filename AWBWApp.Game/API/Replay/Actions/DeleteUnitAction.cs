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
        public string ReadibleName => "Delete";

        public MoveUnitAction MoveUnit;
        public long DeletedUnitId { get; set; }

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Delete Action.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            var unit = controller.Map.DeleteUnit(DeletedUnitId, true);

            controller.ActivePlayer.Funds.Value += ReplayActionHelper.CalculateUnitCost(unit, controller.ActivePlayer.ActiveCO.Value.CO.DayToDayPower, null); //Unit funds does not care about the current active powers
        }

        public void UndoAction(ReplayController controller)
        {
            throw new NotImplementedException("Undo Delete Action is not complete");
        }
    }
}
