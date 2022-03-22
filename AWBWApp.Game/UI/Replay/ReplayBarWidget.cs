using System;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Input;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayBarWidget : TooltipContainer
    {
        private readonly ReplayController replayController;

        private readonly IconButton lastTurnButton;
        private readonly IconButton prevButton;
        private readonly IconButton nextButton;
        private readonly IconButton nextTurnButton;
        private readonly SpriteText currentDayText;
        private readonly SpriteText currentPlayerText;

        public ReplayBarWidget(ReplayController replayController)
        {
            this.replayController = replayController;

            Padding = new MarginPadding { Bottom = 10 };
            Width = 300;
            Height = 55;
            //RelativeSizeAxes = Axes.X;
            Origin = Anchor.BottomCentre;
            Anchor = Anchor.BottomCentre;

            Children = new Drawable[]
            {
                new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 5,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Colour = Color4.Black.Opacity(40),
                        Radius = 5,
                    },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = Vector2.One,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = new Color4(25, 25, 25, 180),
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                lastTurnButton = new ReplayIconButton(AWBWGlobalAction.PreviousTurn)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Action = () => replayController.GoToPreviousTurn(),
                                    Icon = FontAwesome.Solid.AngleDoubleLeft
                                },
                                prevButton = new ReplayIconButton(AWBWGlobalAction.PreviousAction, replayController.GetPreviousActionName)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Action = () => this.replayController.UndoAction(),
                                    Icon = FontAwesome.Solid.AngleLeft
                                },
                                new Container()
                                {
                                    Masking = true,
                                    CornerRadius = 6,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    AutoSizeAxes = Axes.Both,
                                    AutoSizeEasing = Easing.OutQuint,
                                    AutoSizeDuration = 300,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Black.Opacity(0.4f)
                                        },
                                        new Container() //Spacer to set minimum Size
                                        {
                                            Anchor = Anchor.TopCentre,
                                            Origin = Anchor.TopCentre,
                                            Size = new Vector2(125, 35)
                                        },
                                        new FillFlowContainer()
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Vertical,
                                            Padding = new MarginPadding { Horizontal = 5 },
                                            Children = new Drawable[]
                                            {
                                                currentDayText = new SpriteText()
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                },
                                                currentPlayerText = new SpriteText()
                                                {
                                                    Anchor = Anchor.TopCentre,
                                                    Origin = Anchor.TopCentre,
                                                },
                                            }
                                        }
                                    }
                                },

                                nextButton = new ReplayIconButton(AWBWGlobalAction.NextAction, replayController.GetNextActionName)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Action = () => replayController.GoToNextAction(),
                                    Icon = FontAwesome.Solid.AngleRight
                                },
                                nextTurnButton = new ReplayIconButton(AWBWGlobalAction.NextTurn)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Action = () => replayController.GoToNextTurn(),
                                    Icon = FontAwesome.Solid.AngleDoubleRight
                                },
                            }
                        },
                    }
                }
            };

            replayController.CurrentTurnIndex.BindValueChanged(_ => updateTurnText());
        }

        public void UpdateActions()
        {
            lastTurnButton.Enabled.Value = replayController.HasPreviousTurn();
            prevButton.Enabled.Value = replayController.HasPreviousAction();
            nextButton.Enabled.Value = replayController.HasNextAction();
            nextTurnButton.Enabled.Value = replayController.HasNextTurn();
        }

        private void updateTurnText()
        {
            if (!replayController.HasLoadedReplay)
                return;

            currentDayText.Text = $"Day: {replayController.CurrentDay}";
            currentPlayerText.Text = replayController.ActivePlayer.Username;
        }

        protected override ITooltip CreateTooltip() => new ReplayTooltip();

        private class ReplayIconButton : IconButton, IKeyBindingHandler<AWBWGlobalAction>, IHasTooltip
        {
            private readonly AWBWGlobalAction triggerAction;
            private Func<string> getToolTip;

            public ReplayIconButton(AWBWGlobalAction triggerAction, Func<string> getToolTip = null)
            {
                AutoSizeAxes = Axes.Both;
                this.triggerAction = triggerAction;
                this.getToolTip = getToolTip;
            }

            public LocalisableString TooltipText => getToolTip?.Invoke();

            public bool OnPressed(KeyBindingPressEvent<AWBWGlobalAction> e)
            {
                if (e.Repeat)
                    return false;

                if (e.Action == triggerAction)
                {
                    Action?.Invoke();
                    Content.ScaleTo(0.85f, 2000, Easing.OutQuint);
                    return true;
                }

                return false;
            }

            public void OnReleased(KeyBindingReleaseEvent<AWBWGlobalAction> e)
            {
                if (e.Action == triggerAction)
                    Content.ScaleTo(1, 1000, Easing.OutElastic);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // works with AutoSizeAxes above to make buttons autosize with the scale animation.
                Content.AutoSizeAxes = Axes.None;
                Content.Size = new Vector2(DEFAULT_BUTTON_SIZE);
            }
        }

        /// <summary>
        /// Recreation of <see cref="TooltipContainer.Tooltip"/> which sets the tooltip to our colours
        /// </summary>
        private class ReplayTooltip : VisibilityContainer, ITooltip<LocalisableString>
        {
            private readonly SpriteText text;

            public virtual string TooltipText
            {
                set => SetContent(value);
            }

            public virtual void SetContent(LocalisableString content) => text.Text = content;

            private const float text_size = 16;

            public ReplayTooltip()
            {
                Alpha = 0;
                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(40, 40, 40, 255),
                    },
                    text = new SpriteText
                    {
                        Font = FrameworkFont.Regular.With(size: text_size),
                        Padding = new MarginPadding(5),
                    }
                };
            }

            public virtual void Refresh() { }

            /// <summary>
            /// Called whenever the tooltip appears. When overriding do not forget to fade in.
            /// </summary>
            protected override void PopIn() => this.FadeIn();

            /// <summary>
            /// Called whenever the tooltip disappears. When overriding do not forget to fade out.
            /// </summary>
            protected override void PopOut() => this.FadeOut();

            /// <summary>
            /// Called whenever the position of the tooltip changes. Can be overridden to customize
            /// easing.
            /// </summary>
            /// <param name="pos">The new position of the tooltip.</param>
            public virtual void Move(Vector2 pos) => Position = pos;
        }
    }
}
