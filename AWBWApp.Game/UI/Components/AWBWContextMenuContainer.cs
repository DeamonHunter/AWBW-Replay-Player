using AWBWApp.Game.UI.Toolbar;
using osu.Framework.Graphics.Cursor;

namespace AWBWApp.Game.UI.Components
{
    public class AWBWContextMenuContainer : ContextMenuContainer
    {
        protected override osu.Framework.Graphics.UserInterface.Menu CreateMenu() =>
            new AWBWSubMenu(null, null)
            {
                HideSubMenuIfUnHovered = false,
                HideIfUnHovered = false
            };
    }
}
