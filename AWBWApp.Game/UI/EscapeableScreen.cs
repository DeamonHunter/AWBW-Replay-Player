using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK.Input;

namespace AWBWApp.Game.UI
{
    public partial class EscapeableScreen : Screen
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

        public override void OnEntering(ScreenTransitionEvent e)
        {
            this.FadeInFromZero(250, Easing.OutQuint);
            base.OnEntering(e);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            this.FadeIn(250, Easing.OutQuint);
            base.OnResuming(e);
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            this.FadeOut(250, Easing.In);
            base.OnSuspending(e);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            this.FadeOut(125, Easing.Out);
            return base.OnExiting(e);
        }
    }
}
