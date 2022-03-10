using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using AWBWApp.Game;
using AWBWApp.Game.UI.Notifications;
using osu.Framework.Allocation;
using osu.Framework.Logging;
using Squirrel;
using Squirrel.SimpleSplat;

namespace AWBWApp.Desktop
{
    [SupportedOSPlatform("windows")]
    public class SquirrelUpdateManager : Game.Update.UpdateManager
    {
        private UpdateManager updateManager;

        public Task PrepareUpdateAsync() => UpdateManager.RestartAppWhenExited();

        private static readonly Logger logger = Logger.GetLogger("updater");

        private bool updatePending;

        private readonly SquirrelLogger squirrelLogger = new SquirrelLogger();

        private NotificationOverlay notificationOverlay;

        [BackgroundDependencyLoader]
        private void load(NotificationOverlay notificationOverlay)
        {
            this.notificationOverlay = notificationOverlay;

            SquirrelLocator.CurrentMutable.Register(() => squirrelLogger, typeof(ILogger));
        }

        protected override async Task<bool> PerformUpdateCheck() => await checkForUpdateAsync().ConfigureAwait(false);

        private async Task<bool> checkForUpdateAsync(bool useDeltaPatching = true, UpdateProgressNotification notification = null)
        {
            bool scheduleRecheck = true;

            try
            {
                updateManager ??= new GithubUpdateManager(@"https://github.com/DeamonHunter/AWBW-Replay-Player");
                Logger.Log("[Update] Checking for update.");

                var info = await updateManager.CheckForUpdate(!useDeltaPatching).ConfigureAwait(false);

                if (info.ReleasesToApply.Count == 0)
                {
                    Logger.Log("[Update] No Releases to Apply.");

                    if (updatePending)
                    {
                        notificationOverlay.Post(new UpdateCompleteNotification(this));
                        return true;
                    }

                    return false;
                }

                scheduleRecheck = false;

                if (notification == null)
                {
                    notification = new UpdateProgressNotification(this) { State = ProgressNotificationState.Active };
                    Schedule(() => notificationOverlay.Post(notification));
                }

                notification.Progress = 0;
                notification.Text = @"Downloading update...";

                try
                {
                    Logger.Log("[Update] Downloading Releases.");
                    await updateManager.DownloadReleases(info.ReleasesToApply).ConfigureAwait(false);

                    notification.Progress = 0;
                    notification.Text = @"Installing update...";

                    Logger.Log("[Update] Applying Releases.");
                    await updateManager.ApplyReleases(info);

                    Logger.Log("[Update] Finished applying Releases.");
                    notification.State = ProgressNotificationState.Completed;
                    updatePending = true;
                }
                catch (Exception e)
                {
                    if (useDeltaPatching)
                    {
                        logger.Add(@"Delta patching failed. Will attempt a full download.");

                        await checkForUpdateAsync(false).ConfigureAwait(false);
                    }
                    else
                    {
                        // In the case of an error, a separate notification will be displayed.
                        notification.State = ProgressNotificationState.Cancelled;
                        notification.Close();

                        Logger.Error(e, @"Update Failed.");
                    }
                }
            }
            catch (Exception)
            {
                scheduleRecheck = true;
            }
            finally
            {
                if (scheduleRecheck)
                {
                    //Check again in 30 mins.
                    Scheduler.AddDelayed(() => Task.Run(async () => await checkForUpdateAsync().ConfigureAwait(false)), 60000 * 30);
                }
            }

            return true;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            updateManager?.Dispose();
        }

        private class SquirrelLogger : ILogger, IDisposable
        {
            public Squirrel.SimpleSplat.LogLevel Level { get; set; } = Squirrel.SimpleSplat.LogLevel.Info;

            public void Write(string message, Squirrel.SimpleSplat.LogLevel logLevel)
            {
                if (logLevel < Level)
                    return;

                logger.Add(message);
            }

            public void Dispose()
            {
            }
        }

        private class UpdateCompleteNotification : SimpleNotification
        {
            [Resolved]
            private AWBWAppGame game { get; set; }

            public UpdateCompleteNotification(SquirrelUpdateManager updateManager)
                : base(true)
            {
                Text = @"Update is ready to install. Click this to restart!";

                Activated = () =>
                {
                    updateManager.PrepareUpdateAsync().ContinueWith(_ => updateManager.Schedule(() => game?.GracefullyExit()));
                    return true;
                };
            }
        }

        private class UpdateProgressNotification : ProgressNotification
        {
            private readonly SquirrelUpdateManager updateManager;

            public UpdateProgressNotification(SquirrelUpdateManager updateManager)
                : base(true)
            {
                this.updateManager = updateManager;
            }

            protected override Notification CreateCompletionNotification() => new UpdateCompleteNotification(updateManager);

            public override void Close()
            {
                switch (State)
                {
                    case ProgressNotificationState.Cancelled:
                        base.Close();
                        break;
                }
            }
        }
    }
}
