using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
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

        //Todo: Figure out if anything of this is needed.
        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData)
        {
            var action = new TagAction();

            var updatedInfo = (JObject)jObject["updatedInfo"];

            var eventName = (string)updatedInfo["event"];

            if (eventName == "NextTurn")
                return new EndTurnAction(); //Todo: Is there anything special to this end turn action?

            return action;
        }
    }

    public class TagAction : IReplayAction
    {
        public IEnumerable<ReplayWait> PerformAction(ReplayController controller) { yield break; }

        public void UndoAction(ReplayController controller, bool immediate) { }
    }
}
