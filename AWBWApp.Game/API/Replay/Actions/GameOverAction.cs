using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

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

    public class GameOverAction : IReplayAction, IActionCanEndGame
    {
        public int FinishedDay;
        public string GameEndDate;
        public string EndMessage;

        public List<long> Winners;
        public List<long> Losers;

        public string GetReadibleName(ReplayController controller, bool shortName) => "Game Over";

        public bool EndsGame() => true;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context) { }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Game Over Action.");

            var powerAnimation = new EndGamePopupDrawable(controller.Players, Winners, Losers, EndMessage);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Game Over Action.");
        }
    }
}
