using System;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.IO;
using AWBWApp.Game.Tests.Replays;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Interrupts;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.IO.Network;
using osu.Framework.Logging;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    public class TestSceneGameMap : BaseGameMapTestScene
    {
        //private const int default_game_id = 545975;
        private const int default_game_id = 478996; //14 player tag team

        [Resolved]
        private ReplayManager replayStorage { get; set; }

        [Resolved]
        private MapFileStorage mapStorage { get; set; }

        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        private readonly InterruptDialogueOverlay overlay;
        private CustomShoalGenerator generator;

        private string replayString = default_game_id.ToString();

        public TestSceneGameMap()
        {
            Add(overlay = new InterruptDialogueOverlay());
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            generator = new CustomShoalGenerator(GetTileStorage(), GetBuildingStorage());
        }

        [Test]
        public void TestLoadMapAndRun()
        {
            AddStep("Clear Replay", () => ReplayController.ClearReplay());
            AddTextStep("Replay Number", default_game_id.ToString(), x => replayString = x);
            AddStep("Load Map", () => Task.Run(downloadReplayFile));
            AddUntilStep("Wait Until Map is loaded", () => ReplayController.HasLoadedReplay);
            AddRepeatUntilStep("Finish replay", 3000, () => ReplayController.GoToNextAction(), () => !ReplayController.HasNextAction());

            AddStep("Parse All Maps", () =>
            {
                var tester = new TestReplayParsing();
                Task.Run(tester.TestParsingAllReplays);
            });
        }

        private async void downloadReplayFile()
        {
            var gameID = GetNewReplayInterrupt.ParseReplayString(replayString);

            Logger.Log("Starting replay download.", level: LogLevel.Important);

            var replay = await replayStorage.GetReplayData(gameID);

            if (replay == null)
            {
                if (!sessionHandler.LoggedIn)
                {
                    Logger.Log("Replay not Found. Requesting from AWBW.", level: LogLevel.Important);
                    var taskCompletionSource = new TaskCompletionSource<bool>();
                    Schedule(() => overlay.Push(new LoginInterrupt(taskCompletionSource)));

                    Logger.Log("Pushed overlay", level: LogLevel.Important);

                    try
                    {
                        await taskCompletionSource.Task.ConfigureAwait(false); //We do not care about the value provided back.
                    }
                    catch (TaskCanceledException)
                    {
                        Logger.Log("Logging in was cancelled. Need to abort download.");
                        return;
                    }
                }

                Logger.Log("Successfully logged in.", level: LogLevel.Important);
                var link = "https://awbw.amarriner.com/replay_download.php?games_id=" + gameID;

                using (var webRequest = new WebRequest(link))
                {
                    webRequest.AddHeader("Cookie", sessionHandler.SessionID);
                    await webRequest.PerformAsync().ConfigureAwait(false);

                    if (webRequest.ResponseStream.Length <= 100)
                        throw new Exception($"Unable to find the replay of game '{gameID}'. Is the session cookie correct?");

                    replay = await replayStorage.ParseAndStoreReplay(gameID, webRequest.ResponseStream);
                }
            }
            else
                Logger.Log($"Replay of id '{gameID}' existed locally.");

            var terrainFile = await mapStorage.GetOrDownloadMap(replay.ReplayInfo.MapId);

            terrainFile = generator.CreateCustomShoalVersion(terrainFile);

            ReplayController.LoadReplay(replay, terrainFile);
        }
    }
}
