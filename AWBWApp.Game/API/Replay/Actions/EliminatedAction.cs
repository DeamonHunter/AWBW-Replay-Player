using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using Newtonsoft.Json.Linq;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class EliminatedActionBuilder : IReplayActionBuilder
    {
        public string Code => "Eliminated";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new EliminatedAction()
            {
                CausedByPlayerID = (long?)jObject["eliminatedByPId"],
                EliminatedPlayerID = (long)jObject["playerId"],
                EliminationMessage = (string)jObject["message"]
            };

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

    public class EliminatedAction : IReplayAction, IActionCanEndGame
    {
        public long? CausedByPlayerID;
        public long EliminatedPlayerID;
        public string EliminationMessage;
        public bool Resigned;

        public GameOverAction GameOverAction;

        public string GetReadibleName(ReplayController controller, bool shortName)
        {
            if (shortName)
                return Resigned ? "Resigned" : "Eliminated";

            var playerInfo = controller.Players[EliminatedPlayerID];
            return $"{playerInfo.Username ?? $"[Unknown Username:{playerInfo.UserID}]"} {(Resigned ? "Resigned" : "Eliminated")}";
        }

        public bool EndsGame() => GameOverAction != null;

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context)
        {
            if (GameOverAction != null)
            {
                GameOverAction.SetupAndUpdate(controller, context);
                return;
            }

            (int lastTurn, int lastAction) = controller.GetLastTurnAndLastAction();
            if (lastTurn != context.CurrentTurnIndex || lastAction != context.CurrentActionIndex)
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

            GameOverAction = new GameOverAction
            {
                FinishedDay = context.CurrentTurn.Day,
                Winners = winners.ToList(),
                Losers = losers.ToList()
            };

            GameOverAction.Winners.Sort(compareTo);
            GameOverAction.Losers.Sort(compareTo);

            if (teamsAlive.Count > 0)
            {
                var teams = teamsAlive.ToArray();

                if (teamsAlive.Count > 1)
                    GameOverAction.EndMessage = $"The game is over! Teams {createTeamWinnerString(teamsAlive)} are the winners!";
                else
                    GameOverAction.EndMessage = $"The game is over! Team {teams[0]} are the winners!";
            }
            else if (GameOverAction.Winners.Count > 1)
                GameOverAction.EndMessage = $"The game is over! {createSoloWinnerString(context, GameOverAction.Winners)} are the winners!";
            else
            {
                var username = context.PlayerInfos[GameOverAction.Winners[0]].Username ?? $"[Unknown Username:{GameOverAction.Winners[0]}]";
                GameOverAction.EndMessage = $"The game is over! {username} is the winner!";
            }

            GameOverAction.SetupAndUpdate(controller, context);
        }

        private string createTeamWinnerString(HashSet<string> winners)
        {
            var sb = new StringBuilder();

            var idx = 0;

            foreach (var winner in winners)
            {
                if (idx == winners.Count - 1)
                    sb.Append($"and {winner}");
                else if (idx != 0)
                    sb.Append($", {winner}");
                else
                    sb.Append(winner);
            }

            return sb.ToString();
        }

        private string createSoloWinnerString(ReplaySetupContext context, List<long> winners)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < winners.Count; i++)
            {
                var username = context.PlayerInfos[winners[i]].Username ?? $"[Unknown Username:{winners[i]}]";

                if (i == winners.Count - 1)
                    sb.Append($"and {username}");
                else if (i != 0)
                    sb.Append($", {username}");
                else
                    sb.Append(username);
            }

            return sb.ToString();
        }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Elimination Action.");

            var powerAnimation = new EliminationPopupDrawable(controller.Players[EliminatedPlayerID], EliminationMessage, Resigned);
            controller.AddGenericActionAnimation(powerAnimation);
            yield return ReplayWait.WaitForTransformable(powerAnimation);

            controller.Players[EliminatedPlayerID].Eliminated.Value = true;

            if (GameOverAction != null)
            {
                foreach (var replayWait in GameOverAction.PerformAction(controller))
                    yield return replayWait;
            }
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Elimination Action.");

            GameOverAction?.UndoAction(controller);
            controller.Players[EliminatedPlayerID].Eliminated.Value = false;
        }
    }
}
