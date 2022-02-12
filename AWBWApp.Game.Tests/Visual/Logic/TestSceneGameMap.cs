using System;
using System.Threading.Tasks;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.IO;
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

        private InterruptDialogueOverlay overlay;
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
            AddStep("Load Map", () => Task.Run(DownloadReplayFile));
            AddUntilStep("Wait Until Map is loaded", () => ReplayController.HasLoadedReplay);
            AddRepeatUntilStep("Finish replay", 3000, () => ReplayController.GoToNextAction(), () => !ReplayController.HasNextAction());
        }

        private int parseReplayString(string replay)
        {
            const string siteLink = "https://awbw.amarriner.com/2030.php?games_id=";

            int replayId;
            if (int.TryParse(replay, out replayId))
                return replayId;

            if (replay.StartsWith(siteLink))
            {
                var turnIndex = replay.IndexOf("&ndx=");

                string possibleId;
                if (turnIndex >= 0 && turnIndex > siteLink.Length)
                    possibleId = replay.Substring(siteLink.Length, turnIndex - siteLink.Length);
                else
                    possibleId = replay.Substring(siteLink.Length);

                if (int.TryParse(possibleId, out replayId))
                    return replayId;

                throw new Exception("Was unable to parse the replay in the website URL: " + replay);
            }

            throw new Exception("Could not parse replay id: " + replay + ".");
        }

        private static string sessionId = null;

        private async void DownloadReplayFile()
        {
            var gameID = parseReplayString(replayString);

            Logger.Log($"Starting replay download.", level: LogLevel.Important);

            var replay = await replayStorage.GetReplayData(gameID);

            if (replay == null)
            {
                if (sessionId == null)
                {
                    Logger.Log($"Replay not Found. Requesting from AWBW.", level: LogLevel.Important);
                    var taskCompletionSource = new TaskCompletionSource<string>();
                    Schedule(() => overlay.Push(new PasswordInputInterrupt(taskCompletionSource)));

                    Logger.Log($"Pushed overlay", level: LogLevel.Important);

                    try
                    {
                        sessionId = await taskCompletionSource.Task.ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        Logger.Log("Logging in was cancelled. Need to abort download.");
                        return;
                    }

                    if (sessionId == null)
                        throw new Exception("Failed to login.");
                }

                Logger.Log($"Successfully logged in.", level: LogLevel.Important);
                var link = "https://awbw.amarriner.com/replay_download.php?games_id=" + gameID;
                var webRequest = new WebRequest(link);
                webRequest.AddHeader("Cookie", sessionId);
                await webRequest.PerformAsync().ConfigureAwait(false);

                if (webRequest.ResponseStream.Length <= 100)
                    throw new Exception($"Unable to find the replay of game '{gameID}'. Is the session cookie correct?");

                replay = replayStorage.ParseAndStoreReplay(gameID, webRequest.ResponseStream);
            }
            else
                Logger.Log($"Replay of id '{gameID}' existed locally.");

            var terrainFile = mapStorage.Get(replay.ReplayInfo.MapId);

            if (terrainFile == null)
            {
                Logger.Log($"Map of id '{replay.ReplayInfo.MapId}' doesn't exist. Requesting from AWBW.");
                var link = "https://awbw.amarriner.com/text_map.php?maps_id=" + replay.ReplayInfo.MapId;
                var webRequest = new WebRequest(link);
                await webRequest.PerformAsync().ConfigureAwait(false);

                if (webRequest.ResponseStream.Length <= 100)
                    throw new Exception($"Unable to find the replay of game '{gameID}'. Is the session cookie correct?");

                terrainFile = mapStorage.ParseAndStoreResponseHTML(replay.ReplayInfo.MapId, webRequest.GetResponseString());
            }
            else
                Logger.Log($"Replay of id '{gameID}' existed locally.");

            terrainFile = generator.CreateCustomShoalVersion(terrainFile);

            ReplayController.LoadReplay(replay, terrainFile);
        }
    }
}
