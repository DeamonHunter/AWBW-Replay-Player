using System;
using System.Reflection;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;

namespace AWBWApp.Game.UI.Components
{
    public abstract partial class HeaderDetachedDropdown<T> : Dropdown<T>
    {
        private DropdownHeader originalHeader;

        protected HeaderDetachedDropdown()
        {
            originalHeader = Header;
            originalHeader.Hide();
            Header = null;
        }

        protected override DropdownHeader CreateHeader() => new EmptyHeaderDropdownHeader();

        public virtual DropdownHeader GetDetachedHeader()
        {
            if (Header != null)
                return Header;

            Header = CreateDetachedHeader();
            Header.Action = Menu.Toggle;
            Header.ChangeSelection += GetChangeSelection();
            return Header;
        }

        protected abstract DropdownHeader CreateDetachedHeader();

        //Todo: This is ugly. Maybe just create our own dropdown to avoid this mess
        protected Action<DropdownHeader.DropdownSelectionAction> GetChangeSelection()
        {
            var type = GetType().BaseType;

            while (type != null)
            {
                var selection = type.GetMethod("selectionKeyPressed", BindingFlags.Instance | BindingFlags.NonPublic);

                if (selection == null)
                {
                    type = type.BaseType;
                    continue;
                }

                return x => selection.Invoke(this, new object[] { x });
            }

            throw new Exception("Unable to find setup steps");
        }

        private partial class EmptyHeaderDropdownHeader : DropdownHeader
        {
            protected override LocalisableString Label { get; set; }
        }
    }
}
