using System;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI.Interrupts;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor
{
    public partial class UploadMapInterrupt : BaseInterrupt
    {
        [Resolved]
        private ReplayManager replayStorage { get; set; }

        [Resolved]
        private InterruptDialogueOverlay interrupt { get; set; }

        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        private readonly TextBox replayInput;
        private readonly TextFlowContainer errorText;
        private readonly LoadingLayer blockingLayer;

        private readonly ReplayMap mapToUpload;
        private ScheduledDelegate downloadDelegate;

        public override bool CloseWhenParentClicked => blockingLayer.Alpha <= 0;

        public UploadMapInterrupt(ReplayMap mapToUpload)
        {
            this.mapToUpload = mapToUpload;

            HeaderText = "Upload the map to AWBW";

            BodyText = "Please input a map ID, or a link to the map on awbw.amarriner.com"; //Todo: Make link clickable?

            SetInteractables(new Drawable[]
                {
                    replayInput = new BasicTextBox()
                    {
                        PlaceholderText = "Map ID or Map Link",
                        RelativeSizeAxes = Axes.X,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 0.95f,
                        Margin = new MarginPadding { Top = 5 },
                        Height = 40,
                        TabbableContentContainer = this
                    },
                    errorText = new TextFlowContainer()
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        TextAnchor = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
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
                            new InterruptButton
                            {
                                Text = "Cancel",
                                BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                                Action = Cancel,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(-0.25f, 0f)
                            },
                            new InterruptButton
                            {
                                Text = "Upload",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = scheduleDownload,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(0.25f, 0f)
                            }
                        }
                    }
                }
            );

            Add(blockingLayer = new LoadingLayer(true)
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        private void scheduleDownload()
        {
            if (downloadDelegate != null && !(downloadDelegate.Cancelled || downloadDelegate.Completed))
                return;

            downloadDelegate = Schedule(attemptUpload);
        }

        public static long ParseReplayString([NotNull] string replay)
        {
            const string site_link = "https://awbw.amarriner.com/prevmaps.php?maps_id=";

            if (long.TryParse(replay, out var replayID))
                return replayID;

            if (replay.StartsWith(site_link))
            {
                var turnIndex = replay.IndexOf("&ndx=", StringComparison.InvariantCulture);

                string possibleId;
                if (turnIndex >= 0 && turnIndex > site_link.Length)
                    possibleId = replay[site_link.Length..turnIndex];
                else
                    possibleId = replay[site_link.Length..];

                if (long.TryParse(possibleId, out replayID))
                    return replayID;

                throw new Exception("Was unable to parse the replay in the website URL: " + replay);
            }

            throw new Exception("Could not parse replay id: " + replay + ".");
        }

        private async void attemptUpload()
        {
            long mapId;

            try
            {
                mapId = ParseReplayString(replayInput.Text);
            }
            catch (Exception)
            {
                failed("Failed to parse the map id. Must either be of the form '123456' or  'https://awbw.amarriner.com/prevmaps.php?maps_id=123456'");
                return;
            }

            try
            {
                blockingLayer.Show();
                Logger.Log($"Starting replay download.", level: LogLevel.Verbose);

                if (!sessionHandler.LoggedIn)
                {
                    Logger.Log("Not Logged in. Getting user to log in.", level: LogLevel.Verbose);
                    var taskCompletionSource = new TaskCompletionSource<bool>();
                    Schedule(() => interrupt.Push(new LoginInterrupt(taskCompletionSource), false));

                    Logger.Log($"Pushed overlay", level: LogLevel.Verbose);

                    try
                    {
                        await taskCompletionSource.Task.ConfigureAwait(false); //We do not care about the value provided back.
                    }
                    catch (TaskCanceledException)
                    {
                        failed("Login was cancelled.");
                        return;
                    }

                    Logger.Log($"Successfully logged in.", level: LogLevel.Verbose);
                }

                var uploadRequest = new MapUploadWebRequest(mapId, mapToUpload);
                uploadRequest.AddHeader("Cookie", sessionHandler.SessionID);

                await uploadRequest.PerformAsync().ConfigureAwait(false);

                var responseString = uploadRequest.GetResponseString();
                Logger.Log(responseString);

                if (responseString != null && responseString.Contains("Your map has been updated!!"))
                {
                    ActionInvoked();
                    Schedule(Hide);
                }
                else
                    failed("Upload failed due to an unknown error?");
            }
            catch (Exception e)
            {
                failed("Unknown error has occured while attempting to download the file.");
                Logger.Error(e, e.Message);
                return;
            }
        }

        private void failed(string reason)
        {
            Logger.Log("Failed to upload map: " + errorText, level: LogLevel.Verbose);
            Schedule(() =>
            {
                downloadDelegate = null;
                errorText.Text = reason;
                blockingLayer.Hide();
            });
        }

        private partial class InterruptButton : BasicButton
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
