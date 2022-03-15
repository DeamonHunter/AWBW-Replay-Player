using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class GameOverActionBuilder : IReplayActionBuilder
    {
        public string Code => "GameOver";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new GameOverAction();
            action.FinishedDay = (int)jObject["day"];
            action.GameEndDate = (string)jObject["gameEndDate"];
            action.EndMessage = (string)jObject["message"];

            action.Winners = new List<long>();
            foreach (var winner in (JArray)jObject["winners"])
                action.Winners.Add((long)winner);

            action.Losers = new List<long>();
            foreach (var loser in (JArray)jObject["losers"])
                action.Losers.Add((long)loser);

            foreach (var keyValuePair in jObject)
            {
                switch (keyValuePair.Key)
                {
                    case "day":
                    case "gameEndDate":
                    case "message":
                    case "winners":
                    case "losers":
                        break;

                    default:
                        throw new Exception("Unknown Game Over Key: " + keyValuePair.Key);
                }
            }

            return action;
        }
    }

    public class GameOverAction : IReplayAction
    {
        public string ReadibleName => "Game Over";

        public ReplayUnit NewUnit;

        public int FinishedDay;
        public string GameEndDate;
        public string EndMessage;

        public List<long> Winners;
        public List<long> Losers;

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var powerAnimation = new EndGamePopupDrawable(controller.Players, Winners, Losers, EndMessage);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new NotImplementedException("Undo Build Action is not complete");
        }
    }
}
