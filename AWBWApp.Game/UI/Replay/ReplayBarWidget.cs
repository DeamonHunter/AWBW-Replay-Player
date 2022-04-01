using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Input;
using AWBWApp.Game.UI.Components;
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
        private readonly ReplayBarWidgetDropdown dropdown;

        public ReplayBarWidget(ReplayController replayController)
        {
            this.replayController = replayController;

            Padding = new MarginPadding { Bottom = 10 };
            Width = 300;
            Height = 55;
            //RelativeSizeAxes = Axes.X;
            Origin = Anchor.BottomCentre;
            Anchor = Anchor.BottomCentre;

            dropdown = new ReplayBarWidgetDropdown()
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.BottomCentre,
                Width = 300
            };

            var dropDownHeader = dropdown.GetDetachedHeader();

            Children = new Drawable[]
            {
                dropdown,
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
                                dropDownHeader,
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
            dropdown.Current.ValueChanged += x => changeTurn(x.NewValue);
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
            lastTurnButton.Enabled.Value = replayController.HasPreviousTurn();
            prevButton.Enabled.Value = replayController.HasPreviousAction();
            nextButton.Enabled.Value = replayController.HasNextAction();
            nextTurnButton.Enabled.Value = replayController.HasNextTurn();
        }

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

        private void changeTurn(Turn turn)
        {
            if (replayController.CurrentTurnIndex.Value == turn.TurnIndex)
                return;

            replayController.GoToTurn(turn.TurnIndex);
        }

        protected override ITooltip CreateTooltip() => new TextToolTip();

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
    }
}
