using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Input;
using AWBWApp.Game.UI.Components;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public abstract class ReplayBarWidget : Container
    {
        protected ReplayIconButton PrevTurnButton;
        protected ReplayIconButton PrevButton;
        protected ReplayIconButton NextButton;
        protected ReplayIconButton NextTurnButton;
        protected ReplayBarWidgetDropdown TurnSelectDropdown;

        protected Container SliderBarContainer;
        protected KnobSliderBar<float> SliderBar;

        protected readonly ReplayController ReplayController;

        protected float MaxScale = 1;

        public ReplayBarWidget(ReplayController replayController)
        {
            ReplayController = replayController;
            replayController.CurrentTurnIndex.BindValueChanged(_ => updateTurnText());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            TurnSelectDropdown.Current.ValueChanged += x => changeTurn(x.NewValue);
            SliderBar.Current.BindTo(ReplayController.AutoAdvanceDelay);
            SetSliderVisibility(false);
        }

        public virtual void UpdateTurns(List<TurnData> turns, int activeTurn)
        {
            if (turns.Count < 1)
                throw new ArgumentException("There should always be at least 1 turn", nameof(turns));

            var items = new Turn[turns.Count];

            for (int i = 0; i < turns.Count; i++)
            {
                var turn = turns[i];
                items[i] = new Turn
                {
                    Day = turn.Day,
                    Player = ReplayController.Players[turn.ActivePlayerID].Username,
                    TurnIndex = i
                };
            }

            TurnSelectDropdown.Current.Value = items[activeTurn];
            TurnSelectDropdown.Items = items;
        }

        public void UpdateActions()
        {
            PrevTurnButton.Enabled.Value = ReplayController.HasPreviousTurn();
            PrevButton.Enabled.Value = ReplayController.HasPreviousAction();
            NextButton.Enabled.Value = ReplayController.HasNextAction();
            NextTurnButton.Enabled.Value = ReplayController.HasNextTurn();
        }

        public void CancelAutoAdvance(AWBWGlobalAction button)
        {
            switch (button)
            {
                case AWBWGlobalAction.PreviousTurn:
                    PrevTurnButton.CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.PreviousAction:
                    PrevButton.CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.NextAction:
                    NextButton.CancelAutoAdvance();
                    break;

                case AWBWGlobalAction.NextTurn:
                    NextTurnButton.CancelAutoAdvance();
                    break;
            }
        }

        public void SetSliderVisibility(bool visible)
        {
            SliderBarContainer.ScaleTo(visible ? Vector2.One : new Vector2(0, 0.1f), 250, Easing.OutQuint);
        }

        public void StartAutoAdvance(AWBWGlobalAction action)
        {
            SetSliderVisibility(true);

            switch (action)
            {
                case AWBWGlobalAction.PreviousTurn:
                    PrevTurnButton.SetAutoAdvancing();
                    break;

                case AWBWGlobalAction.PreviousAction:
                    PrevButton.SetAutoAdvancing();
                    break;

                case AWBWGlobalAction.NextAction:
                    NextButton.SetAutoAdvancing();
                    break;

                case AWBWGlobalAction.NextTurn:
                    NextTurnButton.SetAutoAdvancing();
                    break;
            }
        }

        private void updateTurnText()
        {
            if (!ReplayController.HasLoadedReplay)
                return;

            TurnSelectDropdown.Current.Value = new Turn()
            {
                TurnIndex = ReplayController.CurrentTurnIndex.Value,
                Day = ReplayController.CurrentDay,
                Player = ReplayController.ActivePlayer.Username
            };
        }

        private void changeTurn(Turn turn)
        {
            if (ReplayController.CurrentTurnIndex.Value == turn.TurnIndex)
                return;

            ReplayController.GoToTurn(turn.TurnIndex);
        }

        public void AnimateShow()
        {
            Show();
            this.ScaleTo(MaxScale, 75, Easing.InQuint);
        }

        public void AnimateHide()
        {
            Show();
            this.ScaleTo(new Vector2(0.85f * MaxScale, 0f), 75, Easing.OutQuint);
        }

        protected class ReplayIconButton : IconButton, IKeyBindingHandler<AWBWGlobalAction>, IHasTooltip
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

            public void SetAutoAdvancing()
            {
                this.TransformTo("IconColour", autoAdvanceIconColour, auto_advance_timer * 0.5f, Easing.Out);
                autoAdvancing = true;
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
