using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;

namespace AWBWApp.Game.API.Replay
{
    public interface IReplayAction
    {
        string ReadibleName { get; }

        void SetupAndUpdate(ReplayController controller, ReplaySetupContext context);

        IEnumerable<ReplayWait> PerformAction(ReplayController controller);
        void UndoAction(ReplayController controller);
    }
}
