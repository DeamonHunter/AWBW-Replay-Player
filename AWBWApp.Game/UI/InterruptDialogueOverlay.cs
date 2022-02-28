using System.Collections.Generic;
using AWBWApp.Game.UI.Interrupts;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace AWBWApp.Game.UI
{
    public class InterruptDialogueOverlay : OverlayContainer
    {
        public BaseInterrupt CurrentInterrupt { get; private set; }

        private readonly Container interruptHolder;
        private readonly Stack<BaseInterrupt> interrupts = new Stack<BaseInterrupt>();

        public override bool IsPresent => interruptHolder.Children.Count > 0;
        protected override bool BlockNonPositionalInput => true;

        public InterruptDialogueOverlay()
        {
            RelativeSizeAxes = Axes.Both;

            Child = interruptHolder = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };

            Width = 0.4f;
        }

        public void Push(BaseInterrupt interrupt, bool hidePreviousInterrupt = true)
        {
            if (interrupt == CurrentInterrupt || interrupt.State.Value != Visibility.Visible)
                return;

            if (hidePreviousInterrupt)
                CurrentInterrupt?.Hide();
            else
                interrupts.Push(CurrentInterrupt);

            CurrentInterrupt = interrupt;
            CurrentInterrupt.State.ValueChanged += state => onInterruptStateChange(interrupt, state.NewValue);

            interruptHolder.Add(CurrentInterrupt);

            Show();
        }

        private void onInterruptStateChange(VisibilityContainer dialogue, Visibility v)
        {
            if (v != Visibility.Hidden)
                return;

            dialogue.Delay(100).Expire();

            if (dialogue == CurrentInterrupt)
            {
                if (interrupts.Count > 0)
                {
                    CurrentInterrupt = interrupts.Pop();
                }
                else
                {
                    Hide();
                    CurrentInterrupt = null;
                }
            }
        }

        protected override void PopIn()
        {
            this.FadeIn(BaseInterrupt.Enter_Duration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            if (CurrentInterrupt?.State.Value == Visibility.Visible)
            {
                CurrentInterrupt.Hide();
                return;
            }
            this.FadeOut(BaseInterrupt.Exit_Duration, Easing.OutSine);
        }
    }
}
