using System;
using System.Net.Http;
using System.Threading.Tasks;
using AWBWApp.Game.API;
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
    public partial class LoginInterrupt : SideInterupt
    {
        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        private readonly TextBox usernameInput;
        private readonly TextBox passwordInput;
        private readonly TextFlowContainer errorText;
        private readonly LoadingLayer blockingLayer;

        private readonly TaskCompletionSource<bool> sessionIdCallback;
        public override bool CloseWhenParentClicked => blockingLayer.Alpha <= 0;

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
                                TabbableContentContainer = this,
                                CommitOnFocusLost = false
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
                                Text = "Accept",
                                BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                                HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                                Action = scheduleLogin,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(0.25f, 0f)
                            }
                        }
                    }
                }
            );
            
            passwordInput.OnCommit += onPasswordBoxCommit;

            Add(blockingLayer = new LoadingLayer(true)
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        private void scheduleLogin()
        {
            Schedule(attemptLogin);
        }
        private void onPasswordBoxCommit(TextBox sender, bool newText)
        {
            scheduleLogin();
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
                catch (TaskCanceledException)
                {
                    failed(sessionHandler.LoginError);
                    return;
                }

                sessionIdCallback.TrySetResult(true);
                ActionInvoked();
                Schedule(Hide);
            }
            catch (HttpRequestException)
            {
                failed("Failed to Login to server. Are you connected to the internet?");
                return;
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
            Logger.Log("Failed to login: " + reason, level: LogLevel.Verbose);
            Schedule(() =>
            {
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
    }
}
