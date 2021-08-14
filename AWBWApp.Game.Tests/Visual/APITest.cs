using AWBWApp.Game.API;
using osu.Framework.Logging;

namespace AWBWApp.Game.Tests.Visual
{
    public class APITest : AWBWAppTestScene
    {
        public APITest()
        {
            Logger.Log("Loaded");
            Schedule(() => GetResponseFromServer());
        }

        public async void GetResponseFromServer()
        {
            var turn = ReplayTurnRequest.CreateRequest(361160, 1, 10, 968947, true);
            await turn.PerformAsync().ConfigureAwait(false);

            Logger.Log("Got Response: " + turn.GetResponseString());
        }
    }
}
