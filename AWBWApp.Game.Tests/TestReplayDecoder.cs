using System.IO;
using System.IO.Compression;
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
            var replayData = storage.Get(replay);

            using (var stream = new MemoryStream(replayData))
            {
                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                return parser.ParseReplayZip(zipArchive);
            }
        }

        public async Task<ReplayData> GetReplayInStorageAsync(string replay)
        {
            var replayData = await storage.GetAsync(replay).ConfigureAwait(false);

            using (var stream = new MemoryStream(replayData))
            {
                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                return parser.ParseReplayZip(zipArchive);
            }
        }
    }
}
