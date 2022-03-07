using osu.Framework;
using osu.Framework.Platform;

namespace AWBWApp.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"AWBWApp"))
            {
                using (osu.Framework.Game game = new AWBWAppGameDesktop())
                    host.Run(game);
            }
        }
    }
}
