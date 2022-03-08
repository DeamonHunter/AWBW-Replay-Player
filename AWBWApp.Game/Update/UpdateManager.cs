using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;

namespace AWBWApp.Game.Update
{
    public class UpdateManager : CompositeDrawable
    {
        public bool CanCheckForUpdate => GetType() != typeof(UpdateManager);

        [Resolved]
        private AWBWConfigManager config { get; set; }

        [Resolved]
        private AWBWAppGameBase game { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Schedule(() => Task.Run(CheckForUpdateAsync));

            var version = game.Version;

            string lastVersion = config.Get<string>(AWBWSetting.Version);

            if (game.IsDeployedBuild && version != lastVersion)
            {
                if (!string.IsNullOrEmpty(lastVersion))
                    Logger.Log("Update Complete."); //Todo: Some sort of notification
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
