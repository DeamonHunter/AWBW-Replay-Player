using AWBWApp.Game.API.Replay.Actions;

namespace AWBWApp.Game.API.Replay
{
    public static class ReplayPostProcessor
    {
        /// <summary>
        /// This helps fix some errors with replays that do not occur due to parsing.
        /// </summary>
        /// <param name="replay"></param>
        public static void ProcessReplay(ReplayData replay)
        {
            fixTagEndOfTurns(replay);
        }

        /// <summary>
        /// Tag matches can sometimes add the end of turn action to the next turn, rather than the turn it was supposed to end.
        /// </summary>
        /// <param name="replay"></param>
        private static void fixTagEndOfTurns(ReplayData replay)
        {
            for (int i = 0; i < replay.TurnData.Count; i++)
            {
                var turn = replay.TurnData[i];
                if (turn?.Actions == null || turn.Actions.Count <= 0)
                    continue;

                var endTurnAction = turn.Actions[0] as EndTurnAction;
                if (endTurnAction == null || !endTurnAction.TagSwitchOccurred)
                    continue;

                turn.Actions.RemoveAt(0);
                turn.Actions.Add(endTurnAction);
            }
        }
    }
}
