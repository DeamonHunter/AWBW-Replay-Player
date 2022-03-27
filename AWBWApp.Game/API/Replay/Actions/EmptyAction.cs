using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class EmptyAction : IReplayAction
    {
        public string GetReadibleName(ReplayController controller, bool shortName) => "Empty";

        public void SetupAndUpdate(ReplayController controller, ReplaySetupContext context) { }

        public IEnumerable<ReplayWait> PerformAction(ReplayController controller)
        {
            Logger.Log("Performing Empty Action.");
            yield break;
        }

        public void UndoAction(ReplayController controller)
        {
            Logger.Log("Undoing Empty Action.");
        }
    }
}
