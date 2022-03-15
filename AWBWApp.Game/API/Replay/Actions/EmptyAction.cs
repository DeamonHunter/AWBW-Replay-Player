using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class EmptyAction : IReplayAction
    {
        public string ReadibleName => "Empty";

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Empty action performed");
            yield break;
        }

        public void UndoAction(ReplayController controller, bool immediate) { }
    }
}
