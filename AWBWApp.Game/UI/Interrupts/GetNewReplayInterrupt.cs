using System;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.IO;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Interrupts
{
    public class GetNewReplayInterrupt : BaseInterrupt
    {
        private TextBox replayInput;

        private SpriteText errorText;

        private Button acceptButton;
        private Button cancelButton;

        private LoadingLayer blockingLayer;

        [Resolved]
        private ReplayManager replayStorage { get; set; }

        [Resolved]
        private InterruptDialogueOverlay interrupt { get; set; }

        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        private readonly TaskCompletionSource<ReplayInfo> sessionIdCallback;

        public GetNewReplayInterrupt(TaskCompletionSource<ReplayInfo> sessionIdCallback)
        {
            this.sessionIdCallback = sessionIdCallback;

            HeaderText = "Add a new Replay";

            BodyText = "Please input a replay ID, or a link to the replay on awbw.amarriner.com"; //Todo: Make link clickable?

            SetInteractables(new Drawable[]
                {
                    replayInput = new BasicTextBox()
                    {
                        PlaceholderText = "Replay ID or Replay Link",
                        RelativeSizeAxes = Axes.X,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 0.95f,
                        Margin = new MarginPadding { Top = 5 },
                        Height = 40,
                        TabbableContentContainer = this
                    },
                    errorText = new SpriteText()
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.95f,
                        Colour = Color4.Red
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Bottom = 10 },
                        Children = new Drawable[]
                        {
                            cancelButton = new InterruptButton
                            {
                                Text = "Cancel",
                                BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                                Action = cancel,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(-0.25f, 0f)
                            },
                            acceptButton = new InterruptButton
                            {
                                Text = "Accept",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = scheduleDownload,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(0.25f, 0f)
                            }
                        }
                    },
                    new TextFlowContainer(t => t.Font = t.Font.With(size: 18))
                    {
                        Origin = Anchor.TopCentre,
                        Anchor = Anchor.TopCentre,
                        TextAnchor = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = "Note: You can also drag a replay zip file into AWBW Replay Player and it will automatically open."
                    },
                }
            );

            Add(blockingLayer = new LoadingLayer(true)
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        private void scheduleDownload()
        {
            Schedule(attemptDownload);
        }

        public static long ParseReplayString(string replay)
        {
            const string siteLink = "https://awbw.amarriner.com/2030.php?games_id=";

            long replayId;
            if (long.TryParse(replay, out replayId))
                return replayId;

            if (replay.StartsWith(siteLink))
            {
                var turnIndex = replay.IndexOf("&ndx=");

                string possibleId;
                if (turnIndex >= 0 && turnIndex > siteLink.Length)
                    possibleId = replay.Substring(siteLink.Length, turnIndex - siteLink.Length);
                else
                    possibleId = replay.Substring(siteLink.Length);

                if (long.TryParse(possibleId, out replayId))
                    return replayId;

                throw new Exception("Was unable to parse the replay in the website URL: " + replay);
            }

            throw new Exception("Could not parse replay id: " + replay + ".");
        }

        private async void attemptDownload()
        {
            long gameID;

            try
            {
                gameID = ParseReplayString(replayInput.Text);
            }
            catch (Exception e)
            {
                failed("Failed to parse the replay id. Must either be of the form '123456' or  'https://awbw.amarriner.com/2030.php?games_id=123456'");
                return;
            }

            try
            {
                blockingLayer.Show();
                Logger.Log($"Starting replay download.", level: LogLevel.Verbose);

                if (!replayStorage.TryGetReplayInfo(gameID, out var replay))
                {
                    if (!sessionHandler.LoggedIn)
                    {
                        Logger.Log($"Replay not Found. Requesting from AWBW.", level: LogLevel.Verbose);
                        var taskCompletionSource = new TaskCompletionSource<bool>();
                        Schedule(() => interrupt.Push(new LoginInterrupt(taskCompletionSource), false));

                        Logger.Log($"Pushed overlay", level: LogLevel.Verbose);

                        try
                        {
                            await taskCompletionSource.Task.ConfigureAwait(false); //We do not care about the value provided back.
                        }
                        catch (TaskCanceledException)
                        {
                            sessionIdCallback.TrySetCanceled();
                            failed("Login was cancelled.");
                            return;
                        }

                        Logger.Log($"Successfully logged in.", level: LogLevel.Verbose);
                    }

                    var link = "https://awbw.amarriner.com/replay_download.php?games_id=" + gameID;

                    using (var webRequest = new WebRequest(link))
                    {
                        webRequest.AddHeader("Cookie", sessionHandler.SessionID);
                        await webRequest.PerformAsync().ConfigureAwait(false);

                        if (webRequest.ResponseStream.Length <= 100)
                            throw new Exception($"Unable to find the replay of game '{gameID}'. Is the session cookie correct?");

                        var replayData = await replayStorage.ParseAndStoreReplay(gameID, webRequest.ResponseStream);
                        replay = replayData.ReplayInfo;
                    }
                }
                else
                    Logger.Log($"Replay of id '{gameID}' existed locally.");

                sessionIdCallback.TrySetResult(replay);
                ActionInvoked();
                Schedule(Hide);
            }
            catch (Exception e)
            {
                failed("Unknown error has occured while logging in.");
                Logger.Error(e, e.Message);
                return;
            }
        }

        private void failed(string reason)
        {
            Logger.Log("Failed to login: " + errorText, level: LogLevel.Verbose);
            Schedule(() =>
            {
                errorText.Text = reason;
                blockingLayer.Hide();
            });
        }

        private void cancel()
        {
            sessionIdCallback.TrySetCanceled();
            ActionInvoked();
            Hide();
        }

        private class InterruptButton : BasicButton
        {
            public InterruptButton()
            {
                Height = 50;
                RelativeSizeAxes = Axes.X;
                Width = 0.45f;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Margin = new MarginPadding { Top = 5 };
                BackgroundColour = Color4Extensions.FromHex(@"150e14");
                SpriteText.Font.With(size: 18);
            }
        }
    }
}
