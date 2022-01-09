using System;
using System.Threading.Tasks;
using AWBWApp.Game.API.New;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Interrupts;
using osu.Framework.Allocation;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    public class TestSceneGameMap : BaseGameMapTestScene
    {
        [Resolved]
        private ReplayFileStorage replayStorage { get; set; }

        [Resolved]
        private TerrainFileStorage terrainStorage { get; set; }

        private InterruptDialogueOverlay overlay;

        public TestSceneGameMap()
        {
            Add(overlay = new InterruptDialogueOverlay());
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            //ReplayController.LoadInitialGameState(498571);
            Task.Run(DownloadReplayFile);
        }

        private async void DownloadReplayFile()
        {
            Logger.Log($"Starting replay download.", level: LogLevel.Important);
            var gameId = 524439;

            var stream = replayStorage.GetStream(gameId);

            if (stream == null)
            {
                Logger.Log($"Replay not Found. Requesting from AWBW.", level: LogLevel.Important);
                var taskCompletionSource = new TaskCompletionSource<string>();
                Schedule(() => overlay.Push(new PasswordInputInterrupt(taskCompletionSource)));

                Logger.Log($"Pushed overlay", level: LogLevel.Important);
                string sessionId = await taskCompletionSource.Task.ConfigureAwait(false);

                if (sessionId == null)
                    throw new Exception("Failed to login.");

                Logger.Log($"Successfully logged in.", level: LogLevel.Important);
                var link = "https://awbw.amarriner.com/replay_download.php?games_id=" + gameId;
                var webRequest = new WebRequest(link);
                webRequest.AddHeader("Cookie", sessionId);
                await webRequest.PerformAsync().ConfigureAwait(false);

                if (webRequest.ResponseStream.Length <= 100)
                    throw new Exception($"Unable to find the replay of game '{gameId}'. Is the session cookie correct?");

                replayStorage.StoreStream(gameId, webRequest.ResponseStream);
                stream = webRequest.ResponseStream;
            }
            else
                Logger.Log($"Replay of id '{gameId}' existed locally.");

            var parser = new AWBWReplayParser();

            ReplayData replayData;

            try
            {
                replayData = parser.ParseReplay(stream);
            }
            finally
            {
                stream.Dispose();
            }

            var terrainFile = terrainStorage.Get(replayData.GameData.MapId);

            if (terrainFile == null)
            {
                Logger.Log($"Map of id '{replayData.GameData.MapId}' doesn't exist. Requesting from AWBW.");
                var link = "https://awbw.amarriner.com/text_map.php?maps_id=" + replayData.GameData.MapId;
                var webRequest = new WebRequest(link);
                await webRequest.PerformAsync().ConfigureAwait(false);

                if (webRequest.ResponseStream.Length <= 100)
                    throw new Exception($"Unable to find the replay of game '{gameId}'. Is the session cookie correct?");

                terrainFile = terrainStorage.ParseAndStoreResponseHTML(replayData.GameData.MapId, webRequest.GetResponseString());
            }
            else
                Logger.Log($"Replay of id '{gameId}' existed locally.");

            ReplayController.LoadReplay(replayData, terrainFile);
        }
    }
}
