using System.IO;
using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.Tests
{
    public class TestReplayDecoder : Drawable
    {
        [Resolved]
        private ResourceStore<byte[]> storage { get; set; }

        private readonly AWBWReplayParser parser = new AWBWReplayParser();

        public ReplayData GetReplayInStorage(string replay)
        {
            var replayStream = storage.Get(replay);

            return parser.ParseReplay(new MemoryStream(replayStream));
        }

        public async Task<ReplayData> GetReplayInStorageAsync(string replay)
        {
            var replayStream = await storage.GetAsync(replay).ConfigureAwait(false);

            return parser.ParseReplay(new MemoryStream(replayStream));
        }
    }
}
