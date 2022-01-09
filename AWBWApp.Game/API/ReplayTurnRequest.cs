using System.Collections.Generic;
using System.Net.Http;
using AWBWApp.Game.API.Replay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.IO.Network;

namespace AWBWApp.Game.API
{
    public class ReplayTurnRequest
    {
        [JsonProperty]
        public int Day { get; set; }

        [JsonProperty(ItemConverterType = typeof(ReplayActionConverter))]
        public IReplayAction[] Actions { get; set; }

        [JsonProperty("daySelector")]
        public List<int> Days;

        [JsonProperty]
        public AWBWGameState GameState { get; set; }

        /// <summary>
        /// Create a request for a replay turn
        /// </summary>
        /// <param name="gameId">The ID of the game.</param>
        /// <param name="turn">The turn number.</param>
        /// <param name="lastDay">Max number of turns?</param>
        /// <param name="currentPlayer">The ID of the player making the turn</param>
        /// <param name="initial"></param>
        /// <returns></returns>
        public static JsonWebRequest<ReplayTurnRequest> CreateRequest(long gameId, int turn, int lastDay, long currentPlayer, bool initial)
        {
            var request = new JsonWebRequest<ReplayTurnRequest>("https://awbw.amarriner.com/api/game/load_replay.php")
            {
                Method = HttpMethod.Post,
                ContentType = "application/json;charset=utf-8"
            };

            var jsonBlob = new JObject
            {
                ["gameId"] = gameId,
                ["turn"] = turn,
                ["turnPId"] = lastDay,
                ["turnDay"] = currentPlayer,
                ["initial"] = initial
            };

            request.AddRaw(jsonBlob.ToString());
            return request;
        }
    }
}
