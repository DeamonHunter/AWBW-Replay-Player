using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Input;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Menu;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Replay
{
    public class MoveableReplayBarWidget : ReplayBarWidget, IHasContextMenu
    {
        private MenuItem[] contextMenuItems;

        private Bindable<float> replayBarScale;
        private Bindable<float> replayBarOffsetX;
        private Bindable<float> replayBarOffsetY;

        public MoveableReplayBarWidget(ReplayController replayController)
            : base(replayController)
        {
            Padding = new MarginPadding { Bottom = 10 };
            AutoSizeAxes = Axes.X;
            Height = 55;

            Origin = Anchor.BottomCentre;
            Anchor = Anchor.BottomCentre;

            TurnSelectDropdown = new ReplayBarWidgetDropdown()
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.BottomCentre,
                Width = 300,
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
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
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
                            Padding = new MarginPadding { Horizontal = 10 },
                            Spacing = new Vector2(5),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
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
                                dropDownHeader,
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
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            replayBarScale = configManager.GetBindable<float>(AWBWSetting.ReplayBarControlScale);
            replayBarScale.BindValueChanged(x =>
            {
                this.ScaleTo(x.NewValue, 150, Easing.OutQuint);
                moveBarToOffset(Position);
            }, true);

            replayBarOffsetX = configManager.GetBindable<float>(AWBWSetting.ReplayBarControlPositionX);
            replayBarOffsetY = configManager.GetBindable<float>(AWBWSetting.ReplayBarControlPositionY);
            contextMenuItems = new[]
            {
                new MenuItem("Scale")
                {
                    Items = createPlayerListScaleItems(configManager)
                },
                new MenuItem("Reset Position", () => moveBarToOffset(Vector2.Zero))
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            moveBarToOffset(new Vector2(replayBarOffsetX.Value, replayBarOffsetY.Value));
        }

        public override void UpdateTurns(List<TurnData> turns, int activeTurn)
        {
            base.UpdateTurns(turns, activeTurn);
            TurnSelectDropdown.SetSizeToLargestPlayerName(ReplayController.Players);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnDragStart(e);

            moveBarToOffset(Position + e.Delta);

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            base.OnDrag(e);
            moveBarToOffset(Position + e.Delta);
        }

        protected override void UpdateAfterAutoSize()
        {
            base.UpdateAfterAutoSize();
            moveBarToOffset(Position);
        }

        private void moveBarToOffset(Vector2 offset)
        {
            if (Parent == null)
                return;

            var drawSize = Parent.DrawSize;
            if (drawSize.X < 0)
                drawSize.X = 0;
            if (drawSize.Y < 0)
                drawSize.Y = 0;

            var xOffsetMax = Math.Abs((Parent.DrawSize.X - DrawSize.X * Scale.X) * 0.5f);

            var newPosition = new Vector2(
                Math.Clamp(offset.X, -xOffsetMax, xOffsetMax),
                Math.Clamp(offset.Y, Math.Min(-Parent.DrawSize.Y + DrawSize.Y * Scale.Y, 0), 0)
            );

            if (Precision.AlmostEquals(newPosition, Position))
                return;

            Position = newPosition;
            replayBarOffsetX.Value = Position.X;
            replayBarOffsetY.Value = Position.Y;
        }

        public MenuItem[] ContextMenuItems => contextMenuItems;

        private MenuItem[] createPlayerListScaleItems(AWBWConfigManager configManager)
        {
            var playerListScale = configManager.GetBindable<float>(AWBWSetting.ReplayBarControlScale);
            var genericBindable = new Bindable<object>(1f);

            playerListScale.BindValueChanged(x =>
            {
                genericBindable.Value = x.NewValue;
            }, true);

            genericBindable.BindValueChanged(x =>
            {
                playerListScale.Value = (float)x.NewValue;
            });

            return new MenuItem[]
            {
                new StatefulMenuItem("0.75x", genericBindable, 0.75f),
                new StatefulMenuItem("0.8x", genericBindable, 0.8f),
                new StatefulMenuItem("0.85x", genericBindable, 0.85f),
                new StatefulMenuItem("0.9x", genericBindable, 0.9f),
                new StatefulMenuItem("0.95x", genericBindable, 0.95f),
                new StatefulMenuItem("1.0x", genericBindable, 1f),
                new StatefulMenuItem("1.05x", genericBindable, 1.05f),
                new StatefulMenuItem("1.1x", genericBindable, 1.1f),
                new StatefulMenuItem("1.15x", genericBindable, 1.15f),
                new StatefulMenuItem("1.2x", genericBindable, 1.2f),
                new StatefulMenuItem("1.25x", genericBindable, 1.25f),
            };
        }
    }
}
