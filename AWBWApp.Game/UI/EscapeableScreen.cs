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
    }
}
