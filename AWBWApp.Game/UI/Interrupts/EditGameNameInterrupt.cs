using System;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.IO;
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
    public class EditGameNameInterrupt : BaseInterrupt
    {
        [Resolved]
        private ReplayManager replayStorage { get; set; }

        private readonly TextBox usernameInput;
        private readonly TextFlowContainer errorText;

        private readonly ReplayInfo replayInfo;

        public EditGameNameInterrupt(ReplayInfo replayInfo)
        {
            this.replayInfo = replayInfo;

            HeaderText = "Editing a Game Name";

            var currentName = replayInfo.GetDisplayName();
            BodyText = $"Please enter a new name for the replay: {currentName} (ID: {replayInfo.ID})";

            SetInteractables(new Drawable[]
                {
                    usernameInput = new BasicTextBox()
                    {
                        PlaceholderText = "Game Name",
                        Text = currentName,
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
                        Padding = new MarginPadding { Bottom = 5 },
                        Children = new Drawable[]
                        {
                            new InterruptButton
                            {
                                Text = "Cancel",
                                BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                                Action = Cancel,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(-0.31f, 0f)
                            },
                            new InterruptButton
                            {
                                Text = "Reset to Original",
                                BackgroundColour = Color4Extensions.FromHex(@"323d84"),
                                HoverColour = Color4Extensions.FromHex(@"323d84").Lighten(0.2f),
                                Action = attemptReset,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(0f, 0f)
                            },
                            new InterruptButton
                            {
                                Text = "Accept",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = attemptRename,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(0.31f, 0f)
                            }
                        }
                    },
                }
            );
        }

        private void attemptRename()
        {
            try
            {
                Logger.Log($"Attempting to rename game: {replayInfo.ID}.");

                var gameName = usernameInput.Text.Trim();
                replayStorage.UpdateGameName(replayInfo, gameName);

                ActionInvoked();
                Schedule(Hide);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                failed("Unknown Error has occured.");
                return;
            }
        }

        private void attemptReset()
        {
            try
            {
                Logger.Log($"Attempting to reset game name: {replayInfo.ID}.");

                replayStorage.UpdateGameName(replayInfo, null);

                ActionInvoked();
                Schedule(Hide);
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                failed("Unknown Error has occured.");
                return;
            }
        }

        private void failed(string reason)
        {
            Logger.Log("Failed to change game name: " + errorText, level: LogLevel.Verbose);
            Schedule(() =>
            {
                errorText.Text = reason;
            });
        }

        private class InterruptButton : BasicButton
        {
            public InterruptButton()
            {
                Height = 50;
                RelativeSizeAxes = Axes.X;
                Width = 0.30f;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Margin = new MarginPadding { Top = 5 };
                BackgroundColour = Color4Extensions.FromHex(@"150e14");
                SpriteText.Font.With(size: 18);
            }
        }
    }
}
