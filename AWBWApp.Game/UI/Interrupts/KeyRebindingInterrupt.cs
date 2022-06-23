using System.Collections.Generic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.Input;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Input;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace AWBWApp.Game.UI.Interrupts
{
    public class KeyRebindingInterrupt : BaseInterrupt
    {
        private KeybindingOverlay keybindingOverlay;

        private readonly List<KeyRebindRow> keyRebindRows = new List<KeyRebindRow>();

        public KeyRebindingInterrupt()
        {
            HeaderText = "Key Rebinding";
            BodyText = "You can hold shift while clicking to delete a keybind.";

            InteractablesSpacing = new Vector2(0, 7);

            Add(keybindingOverlay = new KeybindingOverlay(this)
            {
                RelativeSizeAxes = Axes.Both,
            });

            SetInteractables(new Drawable[]
            {
                createKeyRebindRow(AWBWGlobalAction.PreviousTurn),
                createKeyRebindRow(AWBWGlobalAction.PreviousAction),
                createKeyRebindRow(AWBWGlobalAction.NextAction),
                createKeyRebindRow(AWBWGlobalAction.NextTurn),
                createKeyRebindRow(AWBWGlobalAction.ShowGridLines),
                createKeyRebindRow(AWBWGlobalAction.ShowUnitsInFog),
                new Container()
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        new InterruptButton
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = "Reset To Default",
                            BackgroundColour = Color4Extensions.FromHex(@"681d1f"),
                            HoverColour = Color4Extensions.FromHex(@"681d1f").Lighten(0.2f),
                            Action = keybindingOverlay.ResetToDefault,
                            Width = 0.4f,
                            RelativePositionAxes = Axes.X,
                            Position = new Vector2(0.05f, 0)
                        },
                        new InterruptButton
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Text = "Close Key Bindings",
                            BackgroundColour = Color4Extensions.FromHex(@"1d681e"),
                            HoverColour = Color4Extensions.FromHex(@"1d681e").Lighten(0.2f),
                            Action = Close,
                            Width = 0.4f,
                            RelativePositionAxes = Axes.X,
                            Position = new Vector2(-0.05f, 0)
                        }
                    }
                }
            });
        }

        private KeyRebindRow createKeyRebindRow(AWBWGlobalAction action)
        {
            var row = new KeyRebindRow(action, activateKeyBindingOverlay, deleteKeyBinding);
            keyRebindRows.Add(row);
            return row;
        }

        private void activateKeyBindingOverlay(AWBWGlobalAction action)
        {
            keybindingOverlay.ChangeCombination(action);
        }

        private void deleteKeyBinding(AWBWGlobalAction action)
        {
            keybindingOverlay.DeleteCombination(action);
        }

        public void UpdateAllKeyBindings()
        {
            foreach (var row in keyRebindRows)
                row.UpdateCombinations();
        }

        private class InterruptButton : BasicButton
        {
            public InterruptButton()
            {
                Height = 50;
                RelativeSizeAxes = Axes.X;
                Width = 0.45f;
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

                Margin = new MarginPadding { Top = 5 };
                BackgroundColour = Color4Extensions.FromHex(@"150e14");
                SpriteText.Font.With(size: 18);
            }
        }

        private class KeybindingOverlay : BlockingLayer
        {
            private SpriteText rebindText;
            private SpriteText combinationText;

            [Resolved]
            private ReadableKeyCombinationProvider keyCombinationProvider { get; set; }

            [Resolved]
            private GlobalActionContainer globalActionContainer { get; set; }

            private AWBWGlobalAction currentAction;
            private KeyCombination currentCombination;

            private KeyRebindingInterrupt interrupt;

            public KeybindingOverlay(KeyRebindingInterrupt interrupt)
            {
                this.interrupt = interrupt;

                Children = new Drawable[]
                {
                    new Container()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(300, 130),
                        Masking = true,
                        CornerRadius = 10,
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4Extensions.FromHex(@"221a21").Lighten(0.35f)
                            },
                            rebindText = new SpriteText()
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Position = new Vector2(0, 5),
                                Font = new FontUsage("Roboto", weight: "Bold", size: 25f)
                            },
                            combinationText = new SpriteText()
                            {
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.BottomCentre,
                                Position = new Vector2(0, -55),
                                Font = new FontUsage("Roboto", weight: "Bold", size: 30f)
                            },
                            new SpriteText()
                            {
                                Anchor = Anchor.BottomCentre,
                                Origin = Anchor.BottomCentre,
                                Position = new Vector2(0, -10),
                                Text = "Press Escape to cancel."
                            }
                        }
                    }
                };
            }

            public void ChangeCombination(AWBWGlobalAction action)
            {
                currentAction = action;

                currentCombination = new KeyCombination();
                rebindText.Text = $"Rebinding '{action.ToString().SpaceBeforeCaptials()}'";
                combinationText.Text = "[Press Any Key]";
                Show();
            }

            public void ResetToDefault()
            {
                globalActionContainer.ResetToDefault();
                interrupt.UpdateAllKeyBindings();
            }

            public void DeleteCombination(AWBWGlobalAction action)
            {
                var keybinding = globalActionContainer.GetKeyBindingForAction(action);
                keybinding.KeyCombination = new KeyCombination("");

                interrupt.UpdateAllKeyBindings();
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (State.Value != Visibility.Visible)
                    return base.OnKeyDown(e);

                if (e.Key == Key.Escape)
                {
                    Hide();
                    return true;
                }

                currentCombination = KeyCombination.FromInputState(e.CurrentState);
                combinationText.Text = keyCombinationProvider.GetReadableString(currentCombination);

                if (!isModifierKey(e.Key))
                    finalise();

                return true;
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
                if (isModifierKey(e.Key))
                {
                    currentCombination = KeyCombination.FromInputState(e.CurrentState);
                    combinationText.Text = keyCombinationProvider.GetReadableString(currentCombination);
                }

                base.OnKeyUp(e);
            }

            private void finalise()
            {
                globalActionContainer.ClearKeyBindingsWithKeyCombination(currentCombination);

                var keybinding = globalActionContainer.GetKeyBindingForAction(currentAction);
                keybinding.KeyCombination = currentCombination;

                interrupt.UpdateAllKeyBindings();
                Hide();
            }

            private bool isModifierKey(Key key) => key < Key.F1;

            protected override bool OnClick(ClickEvent e)
            {
                if (e.Button == MouseButton.Left || e.Button == MouseButton.Right || e.Button == MouseButton.Middle)
                    return true;

                return true;
            }
        }
    }
}
