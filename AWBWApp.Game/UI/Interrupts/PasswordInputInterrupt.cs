using System;
using System.Net.Http;
using System.Threading.Tasks;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osuTK;

namespace AWBWApp.Game.UI.Interrupts
{
    public class PasswordInputInterrupt : BaseInterrupt
    {
        private TextBox usernameInput;
        private TextBox passwordInput;

        private Button acceptButton;
        private Button cancelButton;

        private readonly TaskCompletionSource<string> sessionIdCallback;

        public PasswordInputInterrupt(TaskCompletionSource<string> sessionIdCallback)
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
        }

        private void scheduleLogin()
        {
            Schedule(attemptLogin);
        }

        private async void attemptLogin()
        {
            Logger.Log($"Attempting to login to awbw.");
            var loginRequest = new WebRequest("https://awbw.amarriner.com/logincheck.php");
            loginRequest.Method = HttpMethod.Post;
            loginRequest.AddParameter("username", usernameInput.Text);
            loginRequest.AddParameter("password", passwordInput.Text);

            await loginRequest.PerformAsync().ConfigureAwait(false);

            if (loginRequest.Aborted)
                throw new Exception();

            var cookieValues = loginRequest.ResponseHeaders.GetValues("Set-Cookie");

            string sessionID = null;

            foreach (var cookie in cookieValues)
            {
                if (!cookie.StartsWith("PHPSESSID"))
                    continue;

                var index = cookie.IndexOf(';');
                if (index == -1)
                    throw new Exception("Invalid Cookie");

                sessionID = cookie.Substring(0, index);
            }

            if (sessionID != null)
                sessionIdCallback.TrySetResult(sessionID);
            else
                sessionIdCallback.TrySetCanceled();

            ActionInvoked();
            Schedule(Hide);
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
