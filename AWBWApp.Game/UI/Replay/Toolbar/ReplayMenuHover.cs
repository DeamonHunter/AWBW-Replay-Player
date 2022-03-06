using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Threading;

namespace AWBWApp.Game.UI.Replay.Toolbar
{
    public class ReplayMenuHover : Drawable
    {
        public float HoverShowDelay = 250;

        private ReplayMenuBar menuBar;
        private ScheduledDelegate hoverShowEvent;

        public ReplayMenuHover(ReplayMenuBar menuBar)
        {
            this.menuBar = menuBar;
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateHoverShow();
            return base.OnHover(e);
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            updateHoverShow();
            return base.OnMouseMove(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            menuBar.KeepOpen = false;

            if (hoverShowEvent != null)
            {
                hoverShowEvent?.Cancel();
                hoverShowEvent = null;
            }

            base.OnHoverLost(e);
        }

        private void updateHoverShow()
        {
            hoverShowEvent?.Cancel();

            if (IsHovered && menuBar.MenuShown.Value != MenuState.Open)
                hoverShowEvent = Scheduler.AddDelayed(() =>
                {
                    menuBar.KeepOpen = true;
                    menuBar.MenuShown.Value = MenuState.Open;
                }, HoverShowDelay);
        }
    }
}
