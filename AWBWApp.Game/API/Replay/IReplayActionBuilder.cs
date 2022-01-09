using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API.Replay
{
    public interface IReplayActionBuilder
    {
        string Code { get; }

        ReplayActionDatabase Database { get; set; }

        IReplayAction ParseJObjectIntoReplayAction(JObject jObject, ReplayData replayData, TurnData turnData);
    }
}
