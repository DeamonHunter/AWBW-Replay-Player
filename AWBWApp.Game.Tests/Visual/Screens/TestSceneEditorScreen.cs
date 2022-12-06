using System;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Editor;
using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Toolbar;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Logging;
using osu.Framework.Screens;

namespace AWBWApp.Game.Tests.Visual.Screens
{
    [TestFixture]
    public partial class TestSceneEditorScreen : AWBWAppTestScene
    {
        [Resolved]
        private AWBWSessionHandler sessionHandler { get; set; }

        [Resolved]
        private InterruptDialogueOverlay interruptOverlay { get; set; }

        protected EditorScreen EditorScreen;

        protected ScreenStack ScreenStack;

        [Cached]
        private NotificationOverlay notificationOverlay { get; set; } = new NotificationOverlay();

        private AWBWMenuBar menuBar { get; set; }

        public TestSceneEditorScreen()
        {
            Add(ScreenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(menuBar = new MainControlMenuBar(ScreenStack.Exit, notificationOverlay));
            ScreenStack.Push(EditorScreen = new EditorScreen());
        }

        [Test]
        public void EmptyTest()
        {
            AddStep("Nothing", () => { });
        }
        /*
        [Test]
        public void SendMapTest()
        {
            AddStep("Login", () => interruptOverlay.Push(new LoginInterrupt(new TaskCompletionSource<bool>())));
            AddStep("Send map to upload", () => uploadMap());
        }
        */

        private void uploadMap()
        {
            Task.Run(async () =>
            {
                var map = new ReplayMap();
                map.Size = new Vector2I(20, 20);
                map.Ids = new short[400];

                Array.Fill(map.Ids, (short)28);

                var uploadRequest = new MapUploadWebRequest(118126, map);
                uploadRequest.AddHeader("Cookie", sessionHandler.SessionID);

                await uploadRequest.PerformAsync().ConfigureAwait(false);
                Logger.Log(uploadRequest.GetResponseString());
            });
        }
    }
}
