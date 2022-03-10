using System;
using System.Threading;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public class ProgressNotification : SimpleNotification, ICanBeCompleted
    {
        public Func<bool> CompletionClickAction;

        public string CompletionText { get; set; } = "Task has completed!";

        public bool CompleteImportant { get; set; }

        public float Progress
        {
            get => progress;
            set
            {
                progress = value;
                Scheduler.AddOnce(updateProgress, progress);
            }
        }

        private float progress;

        public ProgressNotificationState State
        {
            get => state;
            set
            {
                if (state == value) return;
                state = value;

                if (IsLoaded)
                    Schedule(updateState);
            }
        }

        public CancellationToken CancellationToken => cancellationTokenSource.Token;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private ProgressNotificationState state;

        private readonly ProgressBar progressBar;
        private readonly LoadingSpinner loadingSpinner;
        private readonly SpriteIcon spriteIcon;

        public Action<Notification> SendCompleteNotification { get; set; }

        public ProgressNotification(bool important)
            : base(important)
        {
            TextDrawable.Margin = new MarginPadding { Left = 40 };

            NotificationContent.AddRange(new Drawable[]
            {
                new Container()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 20 },
                    Masking = true,
                    CornerRadius = 4,
                    Size = new Vector2(30),
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(128, 128, 128, 255)
                        },
                        spriteIcon = new SpriteIcon()
                        {
                            Alpha = 1,
                            RelativeSizeAxes = Axes.Both
                        },
                        loadingSpinner = new LoadingSpinner()
                        {
                            Size = new Vector2(20)
                        }
                    }
                },
                progressBar = new ProgressBar
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativeSizeAxes = Axes.X
                }
            });

            Activated = () => false;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateState();
        }

        private void updateProgress(float progress) => progressBar.Progress = progress;

        private void updateState()
        {
            switch (state)
            {
                case ProgressNotificationState.Queued:
                    Light.Colour = new Color4(80, 200, 20, 255);
                    progressBar.Active = false;

                    spriteIcon.FadeOut(200);
                    loadingSpinner.Show();
                    break;

                case ProgressNotificationState.Active:
                    Light.Colour = new Color4(20, 20, 200, 255);
                    spriteIcon.FadeOut(200);
                    progressBar.Active = true;
                    loadingSpinner.Show();
                    break;

                case ProgressNotificationState.Completed:
                    loadingSpinner.Hide();
                    NotificationContent.MoveToY(-DrawSize.Y / 2, 200, Easing.OutQuint);
                    this.FadeOut(200).Finally(d => Completed());
                    break;

                case ProgressNotificationState.Cancelled:
                    Light.Colour = new Color4(200, 20, 20, 255);
                    progressBar.Active = false;
                    spriteIcon.FadeInFromZero(200);
                    spriteIcon.Icon = FontAwesome.Solid.Cross;
                    break;
            }
        }

        protected virtual Notification CreateCompletionNotification() =>
            new SimpleNotification(CompleteImportant)
            {
                Activated = CompletionClickAction,
                Text = CompletionText
            };

        protected virtual void Completed()
        {
            SendCompleteNotification?.Invoke(CreateCompletionNotification());
            base.Close();
        }

        private class ProgressBar : Container
        {
            private readonly Box box;

            public float Progress
            {
                get => progress;
                set
                {
                    if (progress == value) return;

                    progress = value;

                    box.ResizeTo(new Vector2(progress, 1), 100, Easing.OutQuad);
                }
            }

            private float progress;

            public bool Active
            {
                get => active;
                set
                {
                    active = value;
                    this.FadeColour(active ? new Color4(20, 20, 200, 256) : new Color4(128, 128, 128, 256), 100);
                }
            }

            private bool active;

            public ProgressBar()
            {
                Children = new Drawable[]
                {
                    box = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0
                    }
                };

                Colour = new Color4(128, 128, 128, 256);
                Height = 5;
            }
        }
    }

    public enum ProgressNotificationState
    {
        Queued,
        Active,
        Completed,
        Cancelled
    }
}
