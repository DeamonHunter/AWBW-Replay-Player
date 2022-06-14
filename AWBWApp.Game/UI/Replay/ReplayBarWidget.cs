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
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayBarWidget : Container, IHasContextMenu
    {
        private readonly ReplayController replayController;

        private readonly ReplayIconButton prevTurnButton;
        private readonly ReplayIconButton prevButton;
        private readonly ReplayIconButton nextButton;
        private readonly ReplayIconButton nextTurnButton;
        private readonly ReplayBarWidgetDropdown dropdown;
        private readonly Container sliderBarContainer;

        private Bindable<float> replayBarScale;
        private Bindable<float> replayBarOffsetX;
        private Bindable<float> replayBarOffsetY;

        private MenuItem[] contextMenuItems;

        public ReplayBarWidget(ReplayController replayController)
        {
            this.replayController = replayController;

            Padding = new MarginPadding { Bottom = 10 };
            AutoSizeAxes = Axes.X;
            Height = 55;

            Origin = Anchor.BottomCentre;
            Anchor = Anchor.BottomCentre;

            dropdown = new ReplayBarWidgetDropdown()
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.BottomCentre,
                Width = 300,
                OffsetHeight = Height - Padding.Bottom
            };

            var dropDownHeader = dropdown.GetDetachedHeader();
            SliderBar<float> sliderBar;
            Children = new Drawable[]
            {
                dropdown,
                sliderBarContainer = new Container()
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
                        sliderBar = new KnobSliderBar<float>()
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
                                prevTurnButton = new ReplayIconButton(AWBWGlobalAction.PreviousTurn)
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
                                prevButton = new ReplayIconButton(AWBWGlobalAction.PreviousAction, replayController.GetPreviousActionName)
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
                                nextButton = new ReplayIconButton(AWBWGlobalAction.NextAction, replayController.GetNextActionName)
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
                                nextTurnButton = new ReplayIconButton(AWBWGlobalAction.NextTurn)
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

            replayController.CurrentTurnIndex.BindValueChanged(_ => updateTurnText());
            dropdown.Current.ValueChanged += x => changeTurn(x.NewValue);
            sliderBar.Current.BindTo(replayController.AutoAdvanceDelay);
            SetSliderVisibility(false);
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

        public void UpdateTurns(List<TurnData> turns)
        {
            var items = new Turn[turns.Count];

            for (int i = 0; i < turns.Count; i++)
            {
                var turn = turns[i];
                items[i] = new Turn
                {
                    Day = turn.Day,
                    Player = replayController.Players[turn.ActivePlayerID].Username,
                    TurnIndex = i
                };
            }

            dropdown.Items = items;
        }

        public void UpdateActions()
        {
            prevTurnButton.Enabled.Value = replayController.HasPreviousTurn();
            prevButton.Enabled.Value = replayController.HasPreviousAction();
            nextButton.Enabled.Value = replayController.HasNextAction();
            nextTurnButton.Enabled.Value = replayController.HasNextTurn();
        }

        public void CancelAutoAdvance(AWBWGlobalAction button)
        {
            switch (button)
            {
                case AWBWGlobalAction.PreviousTurn:
                    prevTurnButton.CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.PreviousAction:
                    prevButton.CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.NextAction:
                    nextButton.CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.NextTurn:
                    nextTurnButton.CancelAutoAdvance();
                    break;
            }
        }

        public void SetSliderVisibility(bool visible)
        {
            sliderBarContainer.ScaleTo(visible ? Vector2.One : new Vector2(0, 0.1f), 250, Easing.OutQuint);
        }

        public void SetSizeToLargestPlayerName(Dictionary<long, PlayerInfo> players) => dropdown.SetSizeToLargestPlayerName(players);

        private void updateTurnText()
        {
            if (!replayController.HasLoadedReplay)
                return;

            dropdown.Current.Value = new Turn()
            {
                TurnIndex = replayController.CurrentTurnIndex.Value,
                Day = replayController.CurrentDay,
                Player = replayController.ActivePlayer.Username
            };
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

        private void changeTurn(Turn turn)
        {
            if (replayController.CurrentTurnIndex.Value == turn.TurnIndex)
                return;

            replayController.GoToTurn(turn.TurnIndex);
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

        private class ReplayIconButton : IconButton, IKeyBindingHandler<AWBWGlobalAction>, IHasTooltip
        {
            public Action<AWBWGlobalAction> ToggleAutoAdvanceAction;

            private readonly AWBWGlobalAction triggerAction;
            private readonly Func<string> getToolTip;

            private const float auto_advance_timer = 400;
            private Color4 autoAdvanceIconColour = new Color4(16, 147, 49, 255);
            private bool autoAdvancing;
            private bool confirmingAutoAdvance;

            private bool mouseDown;
            private bool keyDown;

            public LocalisableString TooltipText => getToolTip?.Invoke();

            public ReplayIconButton(AWBWGlobalAction triggerAction, Func<string> getToolTip = null)
            {
                AutoSizeAxes = Axes.Both;
                this.triggerAction = triggerAction;
                this.getToolTip = getToolTip;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // works with AutoSizeAxes above to make buttons autosize with the scale animation.
                Content.AutoSizeAxes = Axes.None;
                Content.Size = new Vector2(DEFAULT_BUTTON_SIZE);

                Enabled.BindValueChanged(x =>
                {
                    if (x.NewValue)
                        return;

                    mouseDown = false;
                    keyDown = false;
                    triggerUp();
                });
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                mouseDown = true;
                triggerDown();
                return true;
            }

            protected override bool OnClick(ClickEvent e)
            {
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                if (!e.HasAnyButtonPressed)
                {
                    mouseDown = false;
                    triggerUp();
                }
            }

            public bool OnPressed(KeyBindingPressEvent<AWBWGlobalAction> e)
            {
                if (e.Repeat || e.Action != triggerAction)
                    return false;

                keyDown = true;
                triggerDown();
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<AWBWGlobalAction> e)
            {
                if (e.Action != triggerAction)
                    return;

                keyDown = false;
                triggerUp();
            }

            private void triggerDown()
            {
                if (autoAdvancing)
                {
                    Content.ScaleTo(0.9f).ScaleTo(1, 1000, Easing.OutElastic);
                    CancelAutoAdvance();
                    ToggleAutoAdvanceAction?.Invoke(triggerAction);
                    return;
                }

                Content.ScaleTo(0.85f, 2000, Easing.OutQuint);

                if (!Enabled.Value)
                    return;

                beginAutoAdvance();
                Action?.Invoke();
            }

            private void triggerUp()
            {
                if (mouseDown || keyDown)
                    return;

                Content.ScaleTo(1, 1000, Easing.OutElastic);
                if (confirmingAutoAdvance)
                    CancelAutoAdvance();
            }

            private void beginAutoAdvance()
            {
                if (confirmingAutoAdvance)
                    return;

                this.TransformTo("IconColour", autoAdvanceIconColour, auto_advance_timer, Easing.Out).OnComplete(_ => autoAdvance());
                confirmingAutoAdvance = true;
            }

            private void autoAdvance()
            {
                confirmingAutoAdvance = false;
                autoAdvancing = true;
                ToggleAutoAdvanceAction?.Invoke(triggerAction);
            }

            public void CancelAutoAdvance()
            {
                confirmingAutoAdvance = false;
                autoAdvancing = false;
                this.TransformTo("IconColour", Color4.White, 75, Easing.InQuint);
            }
        }
    }
}
