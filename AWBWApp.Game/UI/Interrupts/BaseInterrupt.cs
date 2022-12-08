using System;
using osu.Framework.Graphics.Containers;

namespace AWBWApp.Game.UI.Interrupts
{
    public abstract partial class BaseInterrupt : VisibilityContainer
    {
        protected Action CancelAction;
        private bool actionWasInvoked;

        public virtual bool CloseWhenParentClicked => true;

        protected virtual bool ContentIsOpen()
        {
            return IsPresent;
        }

        protected override void PopIn()
        {
            actionWasInvoked = false;
        }

        protected override void PopOut()
        {
            if (!actionWasInvoked && ContentIsOpen())
                CancelAction?.Invoke();
        }

        protected void ActionInvoked()
        {
            actionWasInvoked = true;
        }

        public virtual void Close()
        {
            if (!actionWasInvoked)
                Cancel();
            else
                Hide();
        }

        protected virtual void Cancel()
        {
            ActionInvoked();
            Hide();
        }
    }
}
