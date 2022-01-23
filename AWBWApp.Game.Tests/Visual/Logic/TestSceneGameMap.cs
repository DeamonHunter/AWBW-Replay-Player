using System;
using System.Threading.Tasks;
using AWBWApp.Game.Game.Logic;
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
        private ReplayManager replayStorage { get; set; }

        [Resolved]
        private MapFileStorage mapStorage { get; set; }

        private InterruptDialogueOverlay overlay;
        private CustomShoalGenerator generator;

        public TestSceneGameMap()
        {
            Add(overlay = new InterruptDialogueOverlay());
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            generator = new CustomShoalGenerator(GetTileStorage(), GetBuildingStorage());

            //ReplayController.LoadInitialGameState(498571);
            Task.Run(DownloadReplayFile);
        }

        private async void DownloadReplayFile()
        {
            Logger.Log($"Starting replay download.", level: LogLevel.Important);
            var gameId = 524439;

            var replay = replayStorage.GetReplayData(gameId);

            if (replay == null)
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

                replay = replayStorage.ParseAndStoreReplay(gameId, webRequest.ResponseStream);
            }
            else
                Logger.Log($"Replay of id '{gameId}' existed locally.");

            var terrainFile = mapStorage.Get(replay.ReplayInfo.MapId);

            if (terrainFile == null)
            {
                Logger.Log($"Map of id '{replay.ReplayInfo.MapId}' doesn't exist. Requesting from AWBW.");
                var link = "https://awbw.amarriner.com/text_map.php?maps_id=" + replay.ReplayInfo.MapId;
                var webRequest = new WebRequest(link);
                await webRequest.PerformAsync().ConfigureAwait(false);

                if (webRequest.ResponseStream.Length <= 100)
                    throw new Exception($"Unable to find the replay of game '{gameId}'. Is the session cookie correct?");

                terrainFile = mapStorage.ParseAndStoreResponseHTML(replay.ReplayInfo.MapId, webRequest.GetResponseString());
            }
            else
                Logger.Log($"Replay of id '{gameId}' existed locally.");

            terrainFile = generator.CreateCustomShoalVersion(terrainFile);

            ReplayController.LoadReplay(replay, terrainFile);
        }
    }
}
