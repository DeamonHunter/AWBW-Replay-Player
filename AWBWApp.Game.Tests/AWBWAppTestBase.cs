using osu.Framework.Allocation;

namespace AWBWApp.Game.Tests
{
    namespace AWBWApp.Game.Tests
    {
        /// <summary>
        /// A class for adding dependencies needed for the Test Browser and the Test Runner
        /// </summary>
        public partial class AWBWAppTestBase : AWBWAppGameBase
        {
            private TestReplayDecoder testReplayDecoder;
            private DependencyContainer dependencies;

            [BackgroundDependencyLoader]
            private void load()
            {
                Add(testReplayDecoder = new TestReplayDecoder());
                dependencies.Cache(testReplayDecoder);
            }

            protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
        }
    }
}
