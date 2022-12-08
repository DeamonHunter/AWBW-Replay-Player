using AWBWApp.Game.API;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Editor;
using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Toolbar;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
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
            Add(menuBar);
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

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.Cache(menuBar = new MainControlMenuBar(ScreenStack.Exit, notificationOverlay));
            return dependencies;
        }
    }
}
