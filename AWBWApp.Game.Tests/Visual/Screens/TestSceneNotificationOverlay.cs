using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public class TestSceneNotificationOverlay : AWBWAppTestScene
    {
        private NotificationOverlay notificationOverlay { get; set; }

        private readonly List<ProgressNotification> progressingNotifications = new List<ProgressNotification>();

        [SetUp]
        public void Setup()
        {
            Schedule(() =>
            {
                Clear();

                Add(new FillFlowContainer()
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new AWBWMenuBar(new MenuItem[] { new MenuItem("Settings") }, notificationOverlay = new NotificationOverlay())
                    }
                });

                notificationOverlay.Show();
            });
        }

        [Test]
        public void TestBasicFlow()
        {
            AddStep("Notif #1", () => simpleNotification("This is a test!"));
            AddStep("Notif #2", () => simpleNotification("This is a second test!"));
            AddStep("Important Notif", () => importantNotification("This is important!"));
            AddStep("Progress Notif", () => progressNotification("Progress test.", "Progress complete."));
        }

        private void simpleNotification(string text)
        {
            notificationOverlay.Post(new SimpleNotification(false) { Text = text });
        }

        private void importantNotification(string text)
        {
            notificationOverlay.Post(new SimpleNotification(true) { Text = text });
        }

        private void progressNotification(string text, string completionText)
        {
            var notif = new ProgressNotification(false)
            {
                Text = text,
                CompletionText = completionText
            };

            progressingNotifications.Add(notif);
            notificationOverlay.Post(notif);
        }

        protected override void Update()
        {
            base.Update();

            progressingNotifications.RemoveAll(n => n.State == ProgressNotificationState.Completed);

            if (progressingNotifications.Count(n => n.State == ProgressNotificationState.Active) < 3)
            {
                var p = progressingNotifications.Find(n => n.State == ProgressNotificationState.Queued);

                if (p != null)
                    p.State = ProgressNotificationState.Active;
            }

            foreach (var n in progressingNotifications.FindAll(n => n.State == ProgressNotificationState.Active))
            {
                if (n.Progress < 1)
                    n.Progress += (float)(Time.Elapsed / 2000);
                else
                    n.State = ProgressNotificationState.Completed;
            }
        }
    }
}
