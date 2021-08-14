using osu.Framework.Testing;

namespace AWBWApp.Game.Tests.Visual
{
    public class AWBWAppTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new AWBWAppTestSceneTestRunner();

        private class AWBWAppTestSceneTestRunner : AWBWAppGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}
