using System;
using System.IO;
using System.Threading.Tasks;
using AWBWApp.Game.IO;
using NUnit.Framework;
using osu.Framework.Platform;

namespace AWBWApp.Game.Tests.Replays
{
    [TestFixture]
    [Category("NoCI")]
    public class TestReplayParsing
    {
        //Todo: This test doesn't really work outside of a desktop environment.
        // Probably needs to be upgraded such that it loads a file from a dll and then parses all of the files in that.

        [Test]
        public async Task TestParsingAllReplays()
        {
            var storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AWBWReplayPlayer");
            var storage = new DesktopStorage(storagePath, null);

            var replayStorage = new ReplayManager(storage);

            var replays = replayStorage.GetAllKnownReplays();

            foreach (var replay in replays)
                await replayStorage.GetReplayData(replay);
        }
    }
}
