using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class EliminatedActionBuilder : IReplayActionBuilder
    {
        public string Code => "Eliminated";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new EliminatedAction();

            action.CausedByPlayerID = (long?)jObject["eliminatedByPId"];
            action.EliminatedPlayerID = (long)jObject["playerId"];
            action.EliminationMessage = (string)jObject["message"];
            var resigned = (string)jObject["action"];

            if (resigned == "Resign")
                action.Resigned = true;
            else if (resigned == null)
                action.Resigned = false;
            else
                throw new Exception("Unknown resign action: " + resigned);

            if (jObject.TryGetValue("GameOver", out var jToken))
            {
                var gameOverAction = Database.GetActionBuilder("GameOver").ParseJObjectIntoReplayAction((JObject)jToken, replayData, turnData);
                action.GameOverAction = gameOverAction as GameOverAction;
                if (action.GameOverAction == null)
                    throw new Exception("Elimination action was expecting a game over action.");
            }

            //Todo: Remove once we confirm what is in this action
            foreach (var keyValuePair in jObject)
            {
                switch (keyValuePair.Key)
                {
                    case "eliminatedByPId":
                    case "playerId":
                    case "message":
                    case "GameOver":
                    case "action":
                        break;

                    default:
                        throw new Exception("Unknown key: " + keyValuePair.Key);
                }
            }

            return action;
        }
    }

    public class EliminatedAction : IReplayAction
    {
        public string ReadibleName => Resigned ? "Player Resigned" : "Player Eliminated";

        public long? CausedByPlayerID;
        public long EliminatedPlayerID;
        public string EliminationMessage;
        public bool Resigned;

        public GameOverAction GameOverAction;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var powerAnimation = new EliminationPopupDrawable(controller.Players[EliminatedPlayerID], EliminationMessage, Resigned);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);

            if (GameOverAction != null)
            {
                foreach (var replayWait in GameOverAction.PerformAction(controller))
                    yield return replayWait;
            }
        }

        public void UndoAction(ReplayController controller)
        {
            throw new NotImplementedException("Undo Build Action is not complete");
        }
    }
}
