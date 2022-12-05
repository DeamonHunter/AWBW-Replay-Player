using osu.Framework.Allocation;
using osu.Framework.Platform;

namespace AWBWApp.Game.Tests.Visual
{
    public partial class TestSceneAWBWAppGame : AWBWAppTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        private AWBWAppGame game;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            game = new AWBWAppGame();
            game.SetHost(host);

            AddGame(game);
        }
    }
}
