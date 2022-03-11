using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AWBWApp.Game.UI.Notifications;
using AWBWApp.Game.UI.Replay;
using AWBWApp.Game.UI.Replay.Toolbar;
using AWBWApp.Game.Update;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Threading;

namespace AWBWApp.Game
{
    public class AWBWAppGame : AWBWAppGameBase
    {
        private ScreenStack screenStack;

        private UpdateManager updateManager;
        private NotificationOverlay notificationOverlay;
        private AWBWMenuBar menuBar;

        private DependencyContainer dependencies;
        private Task asyncLoadStream;

        private Clipboard clipboard;

        public AWBWAppGame()
        {
            forwardLoggedErrorsToNotifications();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Add your top-level game components here.
            // A screen stack and sample screen has been provided for convenience, but you can replace it if you don't want to use screens.
            Child = screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both };

            dependencies.CacheAs(this);

            //Todo: This is gross
            loadComponentAfterOtherComponents(
                menuBar = new AWBWMenuBar(new MenuItem[]
                {
                    new MenuItem("Settings")
                    {
                        Items = new[]
                        {
                            new ToggleMenuItem("Show Grid", LocalConfig.GetBindable<bool>(AWBWSetting.ReplayShowGridOverMap)),
                            new ToggleMenuItem("Show Hidden Units", LocalConfig.GetBindable<bool>(AWBWSetting.ReplayShowHiddenUnits)),
                            new ToggleMenuItem("Skip End Turn", LocalConfig.GetBindable<bool>(AWBWSetting.ReplaySkipEndTurn))
                        }
                    }
                }, notificationOverlay = new NotificationOverlay()), Add);

            dependencies.Cache(notificationOverlay);
            loadComponentAfterOtherComponents(updateManager = CreateUpdateManager(), Add, true);

            screenStack.ScreenExited += screenExited;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            loadComponentAfterOtherComponents(new MainScreen(), x => screenStack.Push(x));
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            clipboard = host.GetClipboard();
        }

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

        protected virtual UpdateManager CreateUpdateManager() => new UpdateManager();

        private void screenExited(IScreen lastScreen, IScreen newScreen)
        {
            if (newScreen == null)
                Exit();
        }

        private void forwardLoggedErrorsToNotifications()
        {
            int recentLogCount = 0;
            const double debounce = 60000;
            Exception innerAggregateException = null;

            Logger.NewEntry += entry =>
            {
                if (entry.Level < LogLevel.Important || entry.Target == null) return;

                const int shot_term_display_limit = 3;

                if (recentLogCount < shot_term_display_limit)
                {
                    if (entry.Exception != null)
                    {
                        if (entry.Exception is AggregateException)
                            innerAggregateException = entry.Exception.InnerException;
                        else if (entry.Exception == innerAggregateException)
                            return;

                        string message = "An error occured: ";
                        if (entry.Message == "An unobserved error has occurred." && entry.Exception?.InnerException != null)
                            message += entry.Exception.InnerException.Message;
                        else if (string.IsNullOrEmpty(entry.Message) && entry.Exception != null)
                            message += entry.Exception.Message;
                        else
                            message += entry.Message;

                        var truncatedMessage = !string.IsNullOrEmpty(message) ? (message.Length > 256 ? message[..256] : message) : "";
                        var appendedMessage = clipboard != null ? "\n\nPlease click this to copy the error and give that to the devs." : "";

                        Schedule(() => notificationOverlay.Post(new SimpleErrorNotification()
                        {
                            Text = truncatedMessage + appendedMessage,
                            Activated = () =>
                            {
                                if (entry.Exception != null)
                                    clipboard?.SetText(entry.Exception.ToString());

                                return false;
                            }
                        }));
                    }
                    else if (entry.Level != LogLevel.Important)
                    {
                        Schedule(() => notificationOverlay.Post(new SimpleErrorNotification()
                        {
                            Text = !string.IsNullOrEmpty(entry.Message) ? (entry.Message.Length > 256 ? entry.Message[..256] : entry.Message) : ""
                        }));
                    }
                    else
                    {
                        Schedule(() => notificationOverlay.Post(new SimpleNotification(true)
                        {
                            Text = !string.IsNullOrEmpty(entry.Message) ? (entry.Message.Length > 256 ? entry.Message[..256] : entry.Message) : ""
                        }));
                    }
                }
                else if (recentLogCount == shot_term_display_limit)
                {
                    string logFile = $@"{entry.Target.ToString().ToLowerInvariant()}.log";

                    Schedule(() => notificationOverlay.Post(new SimpleNotification(true)
                    {
                        Text = "Subsequent messages have been logged. Click to view log files.",
                        Activated = () =>
                        {
                            HostStorage.GetStorageForDirectory(@"logs").PresentFileExternally(logFile);
                            return true;
                        }
                    }));
                }

                Interlocked.Increment(ref recentLogCount);
                Scheduler.AddDelayed(() => Interlocked.Decrement(ref recentLogCount), debounce);
            };
        }
    }
}
