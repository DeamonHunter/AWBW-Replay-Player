using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using osu.Framework.IO.Network;

namespace AWBWApp.Game.API
{
    /// <summary>
    /// Make a request to get the current activity of players in a game. This can be used to get the player ids of a game.
    /// </summary>
    public class ReplayActivity
    {
        public Dictionary<long, bool> ActivePlayers = new Dictionary<long, bool>();

        public static async Task<ReplayActivity> RunRequest(long gameId)
        {
            var request = new GenericJsonWebRequest("https://awbw.amarriner.com/api/game/update_user_activity.php")
            {
                Method = HttpMethod.Post,
                ContentType = "application/json;charset=utf-8"
            };
            var jsonBlob = new JObject
            {
                ["gameId"] = gameId,
            };

            request.AddRaw(jsonBlob.ToString());

            await request.PerformAsync().ConfigureAwait(false);

            var activity = new ReplayActivity();
            foreach (var player in request.ResponseObject)
                activity.ActivePlayers.Add(long.Parse(player.Key), (bool)player.Value);
            return activity;
        }
    }
}
