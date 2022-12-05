using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public partial class NotificationOverlay : OverlayContainer
    {
        private readonly FillFlowContainer<Notification> notificationContainer;

        public readonly BindableInt UnreadCount = new BindableInt();

        public NotificationOverlay()
        {
            Anchor = Anchor.TopRight;
            Origin = Anchor.TopRight;

            Width = 300;
            RelativeSizeAxes = Axes.Y;
            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(40, 40, 40, 255)
                },
                new BasicScrollContainer()
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Both,

                    Children = new Drawable[]
                    {
                        notificationContainer = new FillFlowContainer<Notification>()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2)
                        }
                    }
                }
            };
        }

        private readonly Scheduler postScheduler = new Scheduler();

        public override bool IsPresent => base.IsPresent || postScheduler.HasPendingTasks;

        private int runningDepth;

        public void Post(Notification notification)
        {
            postScheduler.Add(() =>
            {
                ++runningDepth;

                Logger.Log($"[Notification] Adding new Notification: {notification.Text}");

                if (notification is ICanBeCompleted completed)
                    completed.SendCompleteNotification = Post;

                notification.Closed += updateCounts;

                notificationContainer.Insert(notification.DisplayOnTop ? -runningDepth : runningDepth, notification);

                if (notification.IsImportant)
                    Show();

                updateCounts();
            });
        }

        protected override void Update()
        {
            base.Update();

            postScheduler.Update();
        }

        private void updateCounts()
        {
            UnreadCount.Value = notificationContainer.Count(n => !n.WasClosed && !n.Read);
        }

        private void markAllRead()
        {
            notificationContainer.ForEach(n => n.Read = true);

            updateCounts();
        }

        protected override void PopIn()
        {
            this.FadeIn(600, Easing.OutQuint);
            this.ResizeHeightTo(1, 600, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(600, Easing.OutQuint);
            this.ResizeHeightTo(0, 600, Easing.OutQuint);

            markAllRead();
        }
    }
}
