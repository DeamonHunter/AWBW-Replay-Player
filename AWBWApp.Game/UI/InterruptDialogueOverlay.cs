using System.Collections.Generic;
using AWBWApp.Game.UI.Interrupts;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace AWBWApp.Game.UI
{
    public partial class InterruptDialogueOverlay : OverlayContainer
    {
        public BaseInterrupt CurrentInterrupt { get; private set; }

        private readonly Container interruptHolder;
        private readonly Stack<BaseInterrupt> interrupts = new Stack<BaseInterrupt>();

        public override bool IsPresent => interruptHolder.Children.Count > 0;
        protected override bool BlockNonPositionalInput => true;

        public InterruptDialogueOverlay()
        {
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                new ClickableContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    Action = () =>
                    {
                        if (CurrentInterrupt == null)
                            PopAll();
                        else if (CurrentInterrupt.CloseWhenParentClicked)
                            CurrentInterrupt?.Close();
                    },
                    Child = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(40, 40, 40, 100)
                    }
                },
                interruptHolder = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.4f,
                }
            };
        }

        public void Push(BaseInterrupt interrupt, bool hidePreviousInterrupt = true)
        {
            if (interrupt == CurrentInterrupt || interrupt.State.Value != Visibility.Visible)
                return;

            if (hidePreviousInterrupt)
                CurrentInterrupt?.Close();
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

        public void PopAll()
        {
            while (interrupts.Count > 0)
            {
                CurrentInterrupt.Delay(100).Expire();
                CurrentInterrupt = interrupts.Pop();
            }
            CurrentInterrupt.Delay(100).Expire();
            CurrentInterrupt = null;
            Hide();
        }

        protected override void PopIn()
        {
            this.FadeIn(SideInterupt.ENTER_DURATION, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            if (CurrentInterrupt?.State.Value == Visibility.Visible)
                CurrentInterrupt.Close();

            this.FadeOut(SideInterupt.EXIT_DURATION, Easing.OutSine);
        }
    }
}
