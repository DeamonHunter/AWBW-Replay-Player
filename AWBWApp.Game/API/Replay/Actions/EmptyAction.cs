using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Logging;

namespace AWBWApp.Game.API.Replay.Actions
{
    public class EmptyAction : IReplayAction
    {
        public List<Transformable> PerformAction(ReplayController controller)
        {
            Logger.Log("Empty action performed");
            return null;
        }

        public void UndoAction(ReplayController controller, bool immediate)
        {
            throw new System.NotImplementedException();
        }
    }
}
