using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;

namespace AWBWApp.Game.API.Replay
{
    public interface IReplayAction
    {
        string ReadibleName { get; }

        IEnumerable<ReplayWait> PerformAction(ReplayController controller);
        void UndoAction(ReplayController controller, bool immediate);
    }
}
