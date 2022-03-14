using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Input;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayBarWidget : Container
    {
        private ReplayController replayController;

        private IconButton lastTurnButton;
        private IconButton prevButton;
        private IconButton nextButton;
        private IconButton nextTurnButton;

        public ReplayBarWidget(ReplayController replayController)
        {
            this.replayController = replayController;
            CornerRadius = 20;

            Padding = new MarginPadding { Bottom = 10 };
            Width = 300;
            Height = 55;
            //RelativeSizeAxes = Axes.X;
            Origin = Anchor.BottomCentre;
            Anchor = Anchor.BottomCentre;

            Children = new Drawable[]
            {
                new Container
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
                            Colour = Color4.Black.Opacity(125),
                        },
                        new FillFlowContainer<IconButton>
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Children = new[]
                            {
                                lastTurnButton = new ReplayIconButton(AWBWGlobalAction.PreviousTurn)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Action = () => replayController.GoToPreviousTurn(),
                                    Icon = FontAwesome.Solid.AngleDoubleLeft
                                },
                                prevButton = new ReplayIconButton(AWBWGlobalAction.PreviousAction)
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    //Action = () => musicController.TogglePause(),
                                    Icon = FontAwesome.Solid.AngleLeft
                                },
                                nextButton = new ReplayIconButton(AWBWGlobalAction.NextAction)
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
        }

        public void UpdateActions()
        {
            lastTurnButton.Enabled.Value = replayController.HasPreviousTurn();
            prevButton.Enabled.Value = false; //replayController.HasPreviousAction(); //Todo: Implement undoing
            nextButton.Enabled.Value = replayController.HasNextAction();
            nextTurnButton.Enabled.Value = replayController.HasNextTurn();
        }

        private class ReplayIconButton : IconButton, IKeyBindingHandler<AWBWGlobalAction>
        {
            private readonly AWBWGlobalAction triggerAction;

            public ReplayIconButton(AWBWGlobalAction triggerAction)
            {
                AutoSizeAxes = Axes.Both;
                this.triggerAction = triggerAction;
            }

            public bool OnPressed(KeyBindingPressEvent<AWBWGlobalAction> e)
            {
                if (e.Repeat)
                    return false;

                if (e.Action == triggerAction)
                {
                    Action?.Invoke();
                    Content.ScaleTo(0.75f, 2000, Easing.OutQuint);
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
    }
}
