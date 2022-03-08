using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AWBWApp.Game.Update;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Framework.Threading;

namespace AWBWApp.Game
{
    public class AWBWAppGame : AWBWAppGameBase
    {
        private ScreenStack screenStack;

        private UpdateManager updateManager;

        private DependencyContainer dependencies;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Add your top-level game components here.
            // A screen stack and sample screen has been provided for convenience, but you can replace it if you don't want to use screens.
            Child = screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both };

            loadComponentAfterOtherComponents(updateManager = CreateUpdateManager(), Add, true);

            screenStack.ScreenExited += screenExited;
        }

        private Task asyncLoadStream;

        private void loadComponentAfterOtherComponents<T>(T component, Action<T> loadCompleteAction, bool cache = false) where T : Drawable
        {
            if (cache)
                dependencies.Cache(component);

            Schedule(() =>
            {
                var previousTask = asyncLoadStream;

                asyncLoadStream = Task.Run(async () =>
                {
                    if (previousTask != null)
                        await previousTask.ConfigureAwait(false);

                    try
                    {
                        Task task = null;
                        var del = new ScheduledDelegate(() => task = LoadComponentAsync(component, loadCompleteAction));
                        Scheduler.Add(del);

                        while (!IsDisposed && !del.Completed)
                            await Task.Delay(10).ConfigureAwait(false);

                        if (IsDisposed)
                            return;

                        Debug.Assert(task != null);

                        await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            });
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        protected override void LoadComplete()
        {
            base.LoadComplete();

            loadComponentAfterOtherComponents(new MainScreen(), x => screenStack.Push(x));
        }

        private void screenExited(IScreen lastScreen, IScreen newScreen)
        {
            if (newScreen == null)
                Exit();
        }

        protected virtual UpdateManager CreateUpdateManager() => new UpdateManager();
    }
}
