using System;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Interrupts;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor
{
    public partial class DownloadOrCreateMapInterrupt : BaseInterrupt
    {
        [Resolved]
        private InterruptDialogueOverlay interrupt { get; set; }

        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        private readonly NumberOnlyTextBox mapSizeXTextBox;
        private readonly NumberOnlyTextBox mapSizeYTextBox;
        private readonly TextBox mapLinkInput;
        private readonly TextFlowContainer errorText;
        private readonly LoadingLayer blockingLayer;

        private readonly TaskCompletionSource<ReplayMap> sessionIdCallback;
        private ScheduledDelegate downloadDelegate;

        public override bool CloseWhenParentClicked => blockingLayer.Alpha <= 0;

        private const short base_tile_id = 1;

        public DownloadOrCreateMapInterrupt(TaskCompletionSource<ReplayMap> sessionIdCallback)
        {
            this.sessionIdCallback = sessionIdCallback;

            SetInnerPositionOffsets(new Vector2(0, -0.3f));

            SetInteractables(new Drawable[]
                {
                    new TextFlowContainer(t => t.Font = t.Font.With(size: 32))
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        TextAnchor = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Width = 0.95f,
                        Text = "Create a New Map with Size:"
                    },
                    mapSizeXTextBox = new NumberOnlyTextBox()
                    {
                        PlaceholderText = "Map Width",
                        TabbableContentContainer = this
                    },
                    mapSizeYTextBox = new NumberOnlyTextBox()
                    {
                        PlaceholderText = "Map Height",
                        TabbableContentContainer = this
                    },
                    new InterruptButton
                    {
                        Text = "Create New Map",
                        BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                        HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                        Action = attemptCreate,
                        Width = 0.95f,
                    },
                    new Box()
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Colour = new Color4(60, 60, 60, 255),
                        Size = new Vector2(0.9f, 2),
                        Margin = new MarginPadding { Vertical = 15 }
                    },
                    new TextFlowContainer(t => t.Font = t.Font.With(size: 32))
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        TextAnchor = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Width = 0.95f,
                        Text = "Download an existing Map:"
                    },
                    mapLinkInput = new BasicTextBox()
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
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Bottom = 10 },
                        Children = new Drawable[]
                        {
                            new InterruptButton
                            {
                                Text = "Open Existing Map",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = scheduleDownload,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(-0.25f, 0f)
                            },
                            new InterruptButton
                            {
                                Text = "Download",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = scheduleDownload,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(0.25f, 0f)
                            }
                        }
                    },
                    new Box()
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Colour = new Color4(60, 60, 60, 255),
                        Size = new Vector2(0.9f, 2),
                        Margin = new MarginPadding { Vertical = 15 }
                    },
                    new InterruptButton
                    {
                        Text = "Return To Main Menu",
                        BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                        HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                        Width = 0.95f,
                        Action = Cancel
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

            downloadDelegate = Schedule(attemptDownload);
        }

        public static long ParseMapString([NotNull] string replay)
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

                throw new Exception("Was unable to parse the map in the website URL: " + replay);
            }

            throw new Exception("Could not parse map id: " + replay + ".");
        }

        private void attemptCreate()
        {
            int mapSizeX;

            if (mapSizeXTextBox.Text.IsNullOrEmpty() || !int.TryParse(mapSizeXTextBox.Text, out mapSizeX) || mapSizeX < 5 || mapSizeX > 36)
            {
                failed("Failed to create map: Please input a valid width. Must be between 5 and 36");
                return;
            }

            int mapSizeY;

            if (mapSizeYTextBox.Text.IsNullOrEmpty() || !int.TryParse(mapSizeXTextBox.Text, out mapSizeY) || mapSizeY < 5 || mapSizeY > 36)
            {
                failed("Failed to create map: Please input a valid height. Must be between 5 and 36");
                return;
            }

            var outputMap = new ReplayMap();
            outputMap.TerrainName = "Test Map";
            outputMap.Size = new Vector2I(mapSizeX, mapSizeY);
            outputMap.Ids = new short[mapSizeX * mapSizeY];
            Array.Fill(outputMap.Ids, base_tile_id);

            sessionIdCallback.TrySetResult(outputMap);
            ActionInvoked();
            Schedule(Hide);
        }

        private async void attemptDownload()
        {
            long mapId;

            try
            {
                mapId = ParseMapString(mapLinkInput.Text);
            }
            catch (Exception)
            {
                failed("Failed to parse the map id. Must either be of the form '123456' or  'https://awbw.amarriner.com/prevmaps.php?maps_id=123456'");
                return;
            }

            try
            {
                blockingLayer.Show();
                Logger.Log($"Starting map download.", level: LogLevel.Verbose);

                ReplayMap map;

                using (var webRequest = new MapDownloadWebRequest(mapId))
                {
                    //webRequest.AddHeader("Cookie", sessionHandler.SessionID);
                    await webRequest.PerformAsync().ConfigureAwait(false);

                    if (sessionIdCallback.Task.IsCanceled)
                    {
                        Logger.Log("Download was cancelled.");
                        ActionInvoked();
                        Schedule(Hide);
                        return;
                    }

                    map = webRequest.ParsedMap;
                }

                sessionIdCallback.TrySetResult(map);
                ActionInvoked();
                Schedule(Hide);
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
            Logger.Log("Failed to get map: " + errorText, level: LogLevel.Verbose);
            Schedule(() =>
            {
                downloadDelegate = null;
                errorText.Text = reason;
                blockingLayer.Hide();
            });
        }

        protected override void Cancel()
        {
            sessionIdCallback.TrySetCanceled();
            base.Cancel();
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

        private partial class NumberOnlyTextBox : BasicTextBox
        {
            public NumberOnlyTextBox()
            {
                RelativeSizeAxes = Axes.X;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
                Width = 0.95f;
                Margin = new MarginPadding { Top = 5 };
                Height = 40;
            }

            protected override bool CanAddCharacter(char character) => char.IsNumber(character);
        }
    }
}
