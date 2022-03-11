using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK.Input;

namespace AWBWApp.Game.UI
{
    public class EscapeableScreen : Screen
    {
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                this.Exit();
                return true;
            }

            return base.OnKeyDown(e);
        }

        public override void OnEntering(IScreen last)
        {
            this.FadeInFromZero(250, Easing.OutQuint);
            base.OnEntering(last);
        }

        public override void OnResuming(IScreen last)
        {
            this.FadeIn(250, Easing.OutQuint);
            base.OnResuming(last);
        }

        public override void OnSuspending(IScreen next)
        {
            this.FadeOut(250, Easing.In);
            base.OnSuspending(next);
        }

        public override bool OnExiting(IScreen next)
        {
            this.FadeOut(125, Easing.Out);
            return base.OnExiting(next);
        }
    }
}
