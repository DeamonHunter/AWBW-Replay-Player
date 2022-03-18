using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class JoinUnitActionBuilder : IReplayActionBuilder
    {
        public string Code => "Join";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new JoinUnitAction();

            var moveObj = jObject["Move"];

            if (moveObj is JObject moveData)
            {
                var moveAction = Database.ParseJObjectIntoReplayAction(moveData, replayData, turnData);
                action.MoveUnit = moveAction as MoveUnitAction;
                if (moveAction == null)
                    throw new Exception("Capture action was expecting a movement action.");
            }

            var joinData = (JObject)jObject["Join"];
            if (joinData == null)
                throw new Exception("Join Replay Action did not contain information about Join.");

            action.JoiningUnitId = (long)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["joinID"], turnData.ActiveTeam, turnData.ActivePlayerID);
            action.JoinedUnit = ReplayActionHelper.ParseJObjectIntoReplayUnit((JObject)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["unit"], turnData.ActiveTeam, turnData.ActivePlayerID));
            action.FundsAfterJoin = (int)ReplayActionHelper.GetPlayerSpecificDataFromJObject((JObject)joinData["newFunds"], turnData.ActiveTeam, turnData.ActivePlayerID);
            return action;
        }
    }

    public class JoinUnitAction : IReplayAction
    {
        public string ReadibleName => "Join";

        public MoveUnitAction MoveUnit;

        public long JoiningUnitId { get; set; }

        public ReplayUnit JoinedUnit { get; set; }

        public int FundsAfterJoin { get; set; }

        public void Setup(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Join Action.");
            Logger.Log("Join animation not completed.");
            Logger.Log("Income change not implemented.");

            if (MoveUnit != null)
            {
                foreach (var transformable in MoveUnit.PerformAction(controller))
                    yield return transformable;
            }

            controller.ActivePlayer.Funds.Value = FundsAfterJoin;

            controller.Map.DeleteUnit(JoiningUnitId, false);
            var joinedUnit = controller.Map.GetDrawableUnit(JoinedUnit.ID);
            joinedUnit.UpdateUnit(JoinedUnit);
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Join Action is not complete");
        }
    }
}
