using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class ResignActionBuilder : IReplayActionBuilder
    {
        public string Code => "Resign";

        public ReplayActionDatabase Database { get; set; }

        public IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData) => Database.GetActionBuilder("Eliminated").ParseJObjectIntoReplayAction((JObject)jObject["Resign"], replayData, turnData);
    }
}
