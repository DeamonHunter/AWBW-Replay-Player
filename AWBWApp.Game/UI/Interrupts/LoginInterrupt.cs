using System;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Interrupts
{
    public class LoginInterrupt : BaseInterrupt
    {
        private TextBox usernameInput;
        private TextBox passwordInput;

        private SpriteText errorText;

        private Button acceptButton;
        private Button cancelButton;

        private LoadingLayer blockingLayer;

        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        private readonly TaskCompletionSource<bool> sessionIdCallback;

        public LoginInterrupt(TaskCompletionSource<bool> sessionIdCallback)
        {
            this.sessionIdCallback = sessionIdCallback;

            HeaderText = "Input Login Details";

            BodyText = "Please input your login details for AWBW.";

            SetInteractables(new Drawable[]
                {
                    usernameInput = new BasicTextBox()
                    {
                        PlaceholderText = "Username",
                        RelativeSizeAxes = Axes.X,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 0.95f,
                        Margin = new MarginPadding { Top = 5 },
                        Height = 40,
                        TabbableContentContainer = this
                    },
                    passwordInput = new BasicPasswordTextBox()
                    {
                        PlaceholderText = "Password",
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
                        Children = new Drawable[]
                        {
                            cancelButton = new InterruptButton
                            {
                                Text = "Cancel",
                                BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                                Action = cancel,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(-0.25f, 0f)
                            },
                            acceptButton = new InterruptButton
                            {
                                Text = "Accept",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                Action = scheduleLogin,
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

        private void scheduleLogin()
        {
            Schedule(attemptLogin);
        }

        private async void attemptLogin()
        {
            try
            {
                blockingLayer.Show();
                Logger.Log("Attempting to login to awbw.");

                try
                {
                    var success = await sessionHandler.AttemptLogin(usernameInput.Text, passwordInput.Text);

                    if (!success)
                    {
                        failed(sessionHandler.LoginError);
                        return;
                    }
                }
                catch (TaskCanceledException e)
                {
                    failed(sessionHandler.LoginError);
                    return;
                }

                sessionIdCallback.TrySetResult(true);
                ActionInvoked();
                Schedule(Hide);
            }
            catch (Exception e)
            {
                failed("Unknown error has occured while logging in.");
                Logger.Log(e.Message, level: LogLevel.Error);
                return;
            }
        }

        private void failed(string reason)
        {
            Logger.Log("Failed to login: " + errorText, level: LogLevel.Important);
            errorText.Text = reason;
            blockingLayer.Hide();
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
