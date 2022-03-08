using System.Threading.Tasks;
using AWBWApp.Game.IO;
using NUnit.Framework;

namespace AWBWApp.Game.Tests.Replays
{
    [TestFixture]
    public class TestReplayParsing
    {
        [Resolved]
        private Storage hostStorage { get; set; }

        [Test]
        public async Task TestParsingAllReplays()
        {
            var replayStorage = new ReplayManager(hostStorage, false);

            var replays = replayStorage.GetAllKnownReplays();

            foreach (var replay in replays)
                await replayStorage.GetReplayData(replay);
        }
    }
}
