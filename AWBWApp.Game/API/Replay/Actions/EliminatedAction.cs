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

            action.CausedByPlayerID = (long)jObject["eliminatedByPId"];
            action.EliminatedPlayerID = (long)jObject["playerId"];
            action.EliminationMessage = (string)jObject["message"];

            //Todo: Remove once we confirm what is in this action
            foreach (var keyValuePair in jObject)
            {
                switch (keyValuePair.Key)
                {
                    case "eliminatedByPId":
                    case "playerId":
                    case "message":
                    case "GameOver":
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
        public long CausedByPlayerID;
        public long EliminatedPlayerID;
        public string EliminationMessage;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var powerAnimation = new EliminationPopupDrawable(controller.Players[EliminatedPlayerID], EliminationMessage);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);

            yield break;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Build Action is not complete");
        }
    }
}
