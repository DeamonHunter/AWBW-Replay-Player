using System;
using System.Collections.Generic;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Interrupts
{
    //Todo: Make distinct from Osu
    public abstract class BaseInterrupt : VisibilityContainer
    {
        public const float Enter_Duration = 500;
        public const float Exit_Duration = 200;

        private readonly float animationBaseXOffset = -50f;

        private readonly Container content;
        private readonly FillFlowContainer interactablesContainer;
        private readonly TextFlowContainer header;
        private readonly TextFlowContainer body;

        private string headerText;

        public string HeaderText
        {
            get => headerText;
            set
            {
                if (headerText == value)
                    return;
                headerText = value;
                header.Text = value;
            }
        }

        private string bodyText;

        public string BodyText
        {
            get => bodyText;
            set
            {
                if (bodyText == value)
                    return;
                bodyText = value;
                body.Text = value;
            }
        }

        private Vector2 baseSpacing;

        public Vector2 InteractablesSpacing
        {
            get => baseSpacing;
            set
            {
                baseSpacing = value;

                if (State.Value == Visibility.Visible)
                    interactablesContainer.Spacing = baseSpacing;
            }
        }

        private bool actionWasInvoked;

        protected override bool StartHidden => true;

        protected Action CancelAction;

        public BaseInterrupt()
        {
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0f,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            EdgeEffect = new EdgeEffectParameters
                            {
                                Type = EdgeEffectType.Shadow,
                                Colour = Color4.Black.Opacity(0.5f),
                                Radius = 8,
                            },
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4Extensions.FromHex(@"221a21"),
                                },
                            },
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.BottomCentre,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0f, 10f),
                            Padding = new MarginPadding { Bottom = 10 },
                            Position = new Vector2(0f, -0.1f),
                            RelativePositionAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                header = new TextFlowContainer(t => t.Font = t.Font.With(size: 25))
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    TextAnchor = Anchor.TopCentre,
                                },
                                body = new TextFlowContainer(t => t.Font = t.Font.With(size: 18))
                                {
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    TextAnchor = Anchor.TopCentre,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                },
                            },
                        },
                        interactablesContainer = new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.TopCentre,
                            RelativeSizeAxes = Axes.X,
                            Position = new Vector2(0f, -0.1f),
                            RelativePositionAxes = Axes.Y,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                        },
                    },
                },
            };

            Show();
        }

        protected void SetInteractables(IEnumerable<Drawable> range)
        {
            interactablesContainer.ChildrenEnumerable = range;
        }

        protected override void PopIn()
        {
            actionWasInvoked = false;

            if (content.Alpha == 0)
            {
                interactablesContainer.TransformSpacingTo(new Vector2(animationBaseXOffset, baseSpacing.Y));
                interactablesContainer.MoveToX(animationBaseXOffset);
            }

            content.FadeIn(Enter_Duration, Easing.OutQuint);
            this.MoveToX(animationBaseXOffset).MoveToX(0, Enter_Duration, Easing.OutQuint);

            interactablesContainer.TransformSpacingTo(baseSpacing, Enter_Duration, Easing.OutQuint);
            interactablesContainer.MoveToX(0, Enter_Duration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            if (!actionWasInvoked && content.IsPresent)
                CancelAction?.Invoke();

            content.FadeOut(Exit_Duration, Easing.InSine);
            this.MoveToX(0).MoveToX(animationBaseXOffset, Enter_Duration, Easing.OutQuint);

            interactablesContainer.TransformSpacingTo(new Vector2(animationBaseXOffset, baseSpacing.Y), Enter_Duration, Easing.OutQuint);
            interactablesContainer.MoveToX(animationBaseXOffset, Enter_Duration, Easing.OutQuint);
        }

        protected override bool OnClick(ClickEvent e)
        {
            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel();
                return true;
            }

            return base.OnKeyDown(e);
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
