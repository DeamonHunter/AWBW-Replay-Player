using osu.Framework.Platform;
using osu.Framework;
using AWBWApp.Game;

namespace AWBWApp.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableHost(@"AWBWApp"))
            using (osu.Framework.Game game = new AWBWAppGame())
                host.Run(game);
        }
    }
}
