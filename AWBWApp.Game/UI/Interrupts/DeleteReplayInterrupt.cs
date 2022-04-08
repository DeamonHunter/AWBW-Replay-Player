using System;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI.Select;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Interrupts
{
    public class DeleteReplayInterrupt : BaseInterrupt
    {
        [Resolved]
        private ReplayManager replayManager { get; set; }

        private readonly ReplayInfo replayToDelete;
        private readonly TextFlowContainer errorText;
        private readonly LoadingLayer blockingLayer;

        public DeleteReplayInterrupt(ReplayInfo replayToDelete, string mapName)
        {
            this.replayToDelete = replayToDelete;

            HeaderText = "Are you sure you want to delete?";

            BodyText = "Are you sure you want to delete the following replay:";

            SetInteractables(new Drawable[]
                {
                    new Container()
                    {
                        Masking = true,
                        CornerRadius = 8,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Size = new Vector2(0.8f, 100),
                        Children = new Drawable[]
                        {
                            new ReplayCarouselPanelBackground(),
                            new ReplayCarouselPanelContent(new CarouselReplay(replayToDelete, mapName))
                        }
                    },
                    errorText = new TextFlowContainer()
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        TextAnchor = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.95f,
                        Colour = Color4.Red
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Children = new Drawable[]
                        {
                            new InterruptButton
                            {
                                Text = "No",
                                BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                                Action = Cancel,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(-0.25f, 0f)
                            },
                            new InterruptButton
                            {
                                Text = "Yes",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = () => Schedule(attemptDelete),
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

        private async void attemptDelete()
        {
            try
            {
                blockingLayer.Show();
                Logger.Log("Attempting to delete replay.");

                replayManager.DeleteReplay(replayToDelete);
                ActionInvoked();
                Schedule(Hide);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                failed("Unknown Error has occured.");
            }
        }

        private void failed(string reason)
        {
            Logger.Log("Failed to delete the replay: " + errorText, level: LogLevel.Verbose);
            Schedule(() =>
            {
                errorText.Text = reason;
                blockingLayer.Hide();
            });
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
