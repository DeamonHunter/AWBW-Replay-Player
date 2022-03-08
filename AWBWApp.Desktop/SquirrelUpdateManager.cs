using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
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

        [BackgroundDependencyLoader]
        private void load()
        {
            SquirrelLocator.CurrentMutable.Register(() => squirrelLogger, typeof(ILogger));
        }

        protected override async Task<bool> PerformUpdateCheck() => await checkForUpdateAsync().ConfigureAwait(false);

        private async Task<bool> checkForUpdateAsync(bool useDeltaPatching = true)
        {
            bool scheduleRecheck = true;

            try
            {
                updateManager ??= new GithubUpdateManager(@"https://github.com/DeamonHunter/AWBW-Replay-Player");

                var info = await updateManager.CheckForUpdate(!useDeltaPatching).ConfigureAwait(false);

                if (info.ReleasesToApply.Count == 0)
                {
                    if (updatePending)
                    {
                        return true;
                    }

                    return false;
                }

                scheduleRecheck = false;

                try
                {
                    await updateManager.DownloadReleases(info.ReleasesToApply).ConfigureAwait(false);

                    await updateManager.ApplyReleases(info);

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
    }
}
