using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay.Actions
{
    /// <summary>
    /// This action always appears at the end of a turn, and gives information about the next turn.
    /// </summary>
    public class EndTurnActionBuilder : IReplayActionBuilder
    {
        public string Code => "End";

        public ReplayActionDatabase Database { get; set; }

        //Todo: Figure out if anything of this is needed.
        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData) => null;
    }
}
