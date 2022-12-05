using osu.Framework.Input.Events;

namespace AWBWApp.Game.UI.Replay
{
    /// <summary>
    /// Custom loading layer that will block clicks and scrolls
    /// </summary>
    public partial class ReplayLoadingLayer : LoadingLayer
    {
        public ReplayLoadingLayer()
            : base(true)
        {
        }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case TouchEvent _:
                    return false;
            }

            return true;
        }
    }
}
