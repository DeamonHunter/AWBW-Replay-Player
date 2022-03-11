using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AWBWApp.Game;
using AWBWApp.Game.Update;
using osu.Framework;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace AWBWApp.Desktop
{
    public class AWBWAppGameDesktop : AWBWAppGame
    {
        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            var desktopWindow = (SDL2DesktopWindow)host.Window;

            desktopWindow.Title = Name;
            desktopWindow.DragDrop += fileDrop;
        }

        protected override UpdateManager CreateUpdateManager()
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    Debug.Assert(OperatingSystem.IsWindows());
                    return new SquirrelUpdateManager();

                default:
                    return new UpdateManager();
            }
        }

        private readonly List<string> importableFiles = new List<string>();
        private ScheduledDelegate importSchedule;

        private void fileDrop(string filePath)
        {
            lock (importableFiles)
            {
                importableFiles.Add(filePath);
                Logger.Log($"Adding {filePath} for import");

                //Cancel previous task and also add a delay to a bit of debouncing in the case of numerous adds
                importSchedule?.Cancel();
                importSchedule = Scheduler.AddDelayed(handlePendingImports, 100);
            }
        }

        private void handlePendingImports()
        {
            lock (importableFiles)
            {
                Logger.Log($"Handling batch import of {importableFiles.Count} files");

                var paths = importableFiles.ToArray();
                importableFiles.Clear();

                Task.Factory.StartNew(() => ImportFiles(null, paths), TaskCreationOptions.LongRunning);
            }
        }
    }
}
