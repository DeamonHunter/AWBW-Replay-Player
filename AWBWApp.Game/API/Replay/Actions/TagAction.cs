using System;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay.Actions
{
    /// <summary>
    /// This action always appears at the end of a turn, and gives information about the next turn.
    /// </summary>
    public class TagActionBuilder : IReplayActionBuilder
    {
        public string Code => "Tag";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var updatedInfo = (JObject)jObject["updatedInfo"];

            var eventName = (string)updatedInfo["event"];

            if (eventName == "NextTurn")
            {
                var action = (EndTurnAction)Database.GetActionBuilder("End").ParseJObjectIntoReplayAction(jObject, replayData, turnData);
                action.TagSwitchOccurred = true;
                return action;
            }

            throw new NotImplementedException("Tag actions, that aren't end turn actions are not implemented.");
        }
    }
}
