using System.Threading.Tasks;
using AWBWApp.Game.UI.Notifications;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;

namespace AWBWApp.Game.Update
{
    public class UpdateManager : CompositeDrawable
    {
        public bool CanCheckForUpdate => game.IsDeployedBuild && GetType() != typeof(UpdateManager);

        [Resolved]
        private AWBWConfigManager config { get; set; }

        [Resolved]
        private NotificationOverlay notificationOverlay { get; set; }

        [Resolved]
        private AWBWAppGameBase game { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Schedule(() => Task.Run(CheckForUpdateAsync));

            var version = game.Version;
            Logger.Log("[Version] " + version);

            string lastVersion = config.Get<string>(AWBWSetting.Version);

            if (game.IsDeployedBuild && version != lastVersion)
            {
                if (!string.IsNullOrEmpty(lastVersion))
                    notificationOverlay.Post(new SimpleNotification(true) { Text = "You are now running the latest version: " + version });
            }

            config.SetValue(AWBWSetting.Version, version);
        }

        private readonly object updateTaskLock = new object();
        private Task<bool> updateCheckTask;

        public async Task<bool> CheckForUpdateAsync()
        {
            if (!CanCheckForUpdate)
                return false;

            Task<bool> waitTask;

            lock (updateTaskLock)
                waitTask = (updateCheckTask ??= PerformUpdateCheck());

            bool hasUpdates = await waitTask.ConfigureAwait(false);

            lock (updateTaskLock)
                updateCheckTask = null;

            return hasUpdates;
        }

        protected virtual Task<bool> PerformUpdateCheck() => Task.FromResult(false);
    }
}
