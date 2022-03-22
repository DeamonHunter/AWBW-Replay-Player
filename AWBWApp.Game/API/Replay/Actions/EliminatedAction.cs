using System;
using System.Collections.Generic;
using System.Linq;
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
            if (GameOverAction != null || controller.TurnCount - 1 != context.CurrentTurnIndex)
                return;

            var winners = new List<long>();
            var losers = new List<long>();

            var teamsAlive = new HashSet<string>();

            foreach (var player in context.PlayerTurns)
            {
                if (player.Value.Eliminated || player.Key == EliminatedPlayerID)
                    continue;

                var team = context.PlayerInfos[player.Key].TeamName;

                if (team == null || team == player.Key.ToString())
                    continue;

                teamsAlive.Add(team);
            }

            foreach (var player in context.PlayerTurns)
            {
                var team = context.PlayerInfos[player.Key].TeamName;

                if (teamsAlive.Count > 0)
                {
                    if (teamsAlive.Contains(team))
                        winners.Add(player.Key);
                    else
                        losers.Add(player.Key);
                }
                else if (!player.Value.Eliminated && player.Key != EliminatedPlayerID)
                    winners.Add(player.Key);
                else
                    losers.Add(player.Key);
            }

            var compareTo = new Comparison<long>((x, y) =>
            {
                var xEliminated = context.PlayerInfos[x].EliminatedOn;
                var yEliminated = context.PlayerInfos[y].EliminatedOn;

                if (x == EliminatedPlayerID)
                    xEliminated = context.CurrentTurnIndex;
                else if (y == EliminatedPlayerID)
                    yEliminated = context.CurrentTurnIndex;

                if (xEliminated == null && yEliminated == null)
                    return context.PlayerInfos[x].RoundOrder.CompareTo(context.PlayerInfos[y].RoundOrder);
                if (xEliminated == null)
                    return -1;
                if (yEliminated == null)
                    return 1;

                return -1 * xEliminated.Value.CompareTo(yEliminated.Value);
            });

            GameOverAction = new GameOverAction();
            GameOverAction.Winners = winners.ToList();
            GameOverAction.Winners.Sort(compareTo);
            GameOverAction.Losers = losers.ToList();
            GameOverAction.Losers.Sort(compareTo);

            GameOverAction.FinishedDay = context.CurrentTurn.Day;

            if (teamsAlive.Count > 0)
                GameOverAction.EndMessage = "";
            else
                GameOverAction.EndMessage = "";
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            var powerAnimation = new EliminationPopupDrawable(controller.Players[EliminatedPlayerID], EliminationMessage, Resigned);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);

            controller.ActivePlayer.Eliminated.Value = true;

            if (GameOverAction != null)
            {
                foreach (var replayWait in GameOverAction.PerformAction(controller))
                    yield return replayWait;
            }
        }

        public void UndoAction(ReplayController controller)
        {
            controller.Players[EliminatedPlayerID].Eliminated.Value = false;
        }
    }
}
