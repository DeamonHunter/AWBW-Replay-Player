using AWBWApp.Game.UI.Toolbar;

namespace AWBWApp.Game.UI.Components
{
    public partial class AWBWNonRelativeContextMenuContainer : NonRelativeContextMenuContainer
    {
        protected override osu.Framework.Graphics.UserInterface.Menu CreateMenu() =>
            new AWBWSubMenu(null, null)
            {
                HideSubMenuIfUnHovered = false,
                HideIfUnHovered = false
            };
    }
}
