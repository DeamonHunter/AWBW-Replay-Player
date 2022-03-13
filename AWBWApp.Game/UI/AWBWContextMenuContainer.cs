using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;

namespace AWBWApp.Game.UI
{
    public class AWBWContextMenuContainer : ContextMenuContainer
    {
        protected override Menu CreateMenu() => new AWBWSubMenu(null, null);
    }
}
