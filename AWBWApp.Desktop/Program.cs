using System;
using System.Runtime.Versioning;
using osu.Framework;
using osu.Framework.Platform;
using Squirrel;

namespace AWBWApp.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            if (OperatingSystem.IsWindows())
                setupSquirrel();

            using (GameHost host = Host.GetSuitableDesktopHost(@"AWBWReplayPlayer"))
            {
                using (osu.Framework.Game game = new AWBWAppGameDesktop())
                    host.Run(game);
            }
        }

        [SupportedOSPlatform("windows")]
        private static void setupSquirrel()
        {
            SquirrelAwareApp.HandleEvents(
                onInitialInstall: (version, tools) =>
                {
                    tools.CreateShortcutForThisExe();
                    tools.CreateUninstallerRegistryEntry();
                },
                onAppUninstall: (version, tools) =>
                {
                    tools.RemoveShortcutForThisExe();
                    tools.RemoveUninstallerRegistryEntry();
                },
                onEveryRun: (version, tools, firstRun) =>
                {
                    tools.SetProcessAppUserModelId();
                });
        }
    }
}
