using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Input;
using AWBWApp.Game.UI.Components;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayPlayerListControlWidget : ReplayBarWidget
    {
        public ReplayPlayerListControlWidget(ReplayController replayController)
            : base(replayController)
        {
            Padding = new MarginPadding { Bottom = 4 };
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            Origin = Anchor.BottomCentre;
            Anchor = Anchor.BottomCentre;

            TurnSelectDropdown = new ConstantWidthReplayBarWidgetDropdown()
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.BottomCentre,
                OffsetHeight = Height - Padding.Bottom
            };

            var dropDownHeader = TurnSelectDropdown.GetDetachedHeader();

            Children = new Drawable[]
            {
                TurnSelectDropdown,
                SliderBarContainer = new Container()
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.BottomCentre,
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
                        SliderBar = new KnobSliderBar<float>()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(180, 30),
                            AccentColour = new Color4(16, 147, 49, 255),
                            BackgroundColour = Color4.DarkGray,
                            Suffix = " seconds delay"
                        }
                    }
                },
                new Container()
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
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
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                dropDownHeader,
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.X,
                                    Height = 35,
                                    Direction = FillDirection.Horizontal,
                                    Padding = new MarginPadding { Horizontal = 10 },
                                    Spacing = new Vector2(5),
                                    Origin = Anchor.TopCentre,
                                    Anchor = Anchor.TopCentre,
                                    Position = new Vector2(0, 0),
                                    Children = new Drawable[]
                                    {
                                        PrevTurnButton = new ReplayIconButton(AWBWGlobalAction.PreviousTurn)
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Action = () =>
                                            {
                                                replayController.CancelAutoAdvance();
                                                replayController.GoToPreviousTurn();
                                            },
                                            ToggleAutoAdvanceAction = replayController.ToggleAutoAdvance,
                                            Icon = FontAwesome.Solid.AngleDoubleLeft
                                        },
                                        PrevButton = new ReplayIconButton(AWBWGlobalAction.PreviousAction, replayController.GetPreviousActionName)
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Action = () =>
                                            {
                                                replayController.CancelAutoAdvance();
                                                replayController.GoToPreviousAction();
                                            },
                                            ToggleAutoAdvanceAction = replayController.ToggleAutoAdvance,
                                            Icon = FontAwesome.Solid.AngleLeft
                                        },
                                        NextButton = new ReplayIconButton(AWBWGlobalAction.NextAction, replayController.GetNextActionName)
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Action = () =>
                                            {
                                                replayController.CancelAutoAdvance();
                                                replayController.GoToNextAction();
                                            },
                                            ToggleAutoAdvanceAction = replayController.ToggleAutoAdvance,
                                            Icon = FontAwesome.Solid.AngleRight
                                        },
                                        NextTurnButton = new ReplayIconButton(AWBWGlobalAction.NextTurn)
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Action = () =>
                                            {
                                                replayController.CancelAutoAdvance();
                                                replayController.GoToNextTurn();
                                            },
                                            ToggleAutoAdvanceAction = replayController.ToggleAutoAdvance,
                                            Icon = FontAwesome.Solid.AngleDoubleRight
                                        },
                                    }
                                },
                            }
                        },
                    }
                }
            };
        }

        private class ConstantWidthReplayBarWidgetDropdown : ReplayBarWidgetDropdown
        {
            public ConstantWidthReplayBarWidgetDropdown()
            {
                RelativeSizeAxes = Axes.X;
            }

            protected override DropdownHeader CreateDetachedHeader() => Header = new ConstantWidthReplayBarDownHeader();

            protected override DropdownMenu CreateMenu()
            {
                var menu = base.CreateMenu();
                menu.BackgroundColour = new Color4(40, 40, 40, 235);
                return menu;
            }

            private class ConstantWidthReplayBarDownHeader : ReplayBarDownHeader
            {
                public ConstantWidthReplayBarDownHeader()
                {
                    Anchor = Anchor.TopCentre;
                    Origin = Anchor.TopCentre;
                    AutoSizeAxes = Axes.Y;
                    RelativeSizeAxes = Axes.X;
                    Margin = new MarginPadding { Top = 4 };
                    Width = 0.95f;

                    TextContainer.RelativeSizeAxes = Axes.X;
                    BackgroundColour = new Color4(20, 20, 20, 255);

                    Foreground.AutoSizeAxes = Axes.Y;
                    Foreground.RelativeSizeAxes = Axes.X;
                    UsernameText.Truncate = true;
                }

                protected override void UpdateAfterAutoSize()
                {
                    base.UpdateAfterAutoSize();
                    UsernameText.MaxWidth = TextContainer.DrawWidth;
                }
            }
        }
    }
}
