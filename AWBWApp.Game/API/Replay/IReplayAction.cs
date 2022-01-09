using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Graphics.Transforms;

namespace AWBWApp.Game.API.Replay
{
    public interface IReplayAction
    {
        List<Transformable> PerformAction(ReplayController controller);
        void UndoAction(ReplayController controller, bool immediate);
    }
}
