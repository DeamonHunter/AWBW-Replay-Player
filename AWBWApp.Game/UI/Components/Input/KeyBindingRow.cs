using System;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.Input;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK;

namespace AWBWApp.Game.UI.Components.Input
{
    public class KeyRebindRow : Container
    {
        private FillFlowContainer<KeybindButton> keyButtonContainer;

        public KeyRebindRow(AWBWGlobalAction setting, Action<AWBWGlobalAction> changeAction, Action<AWBWGlobalAction> deleteAction)
        {
            RelativeSizeAxes = Axes.X;
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;
            Size = new Vector2(0.95f, 35f);
            Masking = true;
            CornerRadius = 10;

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4Extensions.FromHex(@"221a21").Darken(0.35f),
                },
                new SpriteText()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(10, 0),
                    Text = setting.ToString().SpaceBeforeCaptials()
                },
                keyButtonContainer = new FillFlowContainer<KeybindButton>()
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    RelativeSizeAxes = Axes.Both,
                    Margin = new MarginPadding { Right = 10 },
                    Spacing = new Vector2(5, 0),
                    Children = new KeybindButton[]
                    {
                        new KeybindButton(setting, () => deleteAction(setting))
                        {
                            Action = () => changeAction(setting),
                            BackgroundColour = Color4Extensions.FromHex(@"221a21").Lighten(0.25f)
                        },
                    }
                }
            };
        }

        public void UpdateCombinations()
        {
            foreach (var keyButton in keyButtonContainer)
                keyButton.UpdateCombination();
        }

        private class KeybindButton : BasicButton
        {
            [Resolved]
            private GlobalActionContainer globalActionContainer { get; set; }

            [Resolved]
            private ReadableKeyCombinationProvider keyCombinationProvider { get; set; }

            private SpriteText keyCombinationText;
            private AWBWGlobalAction keybind;
            private Action deleteAction;

            public KeybindButton(AWBWGlobalAction keybind, Action deleteAction)
            {
                this.keybind = keybind;
                this.deleteAction = deleteAction;

                Anchor = Anchor.CentreRight;
                Origin = Anchor.CentreRight;

                AutoSizeAxes = Axes.X;
                Height = 35;

                AddRange(new Drawable[]
                {
                    new Container()
                    {
                        Size = new Vector2(90, 0)
                    },
                    keyCombinationText = new SpriteText()
                    {
                        Margin = new MarginPadding { Horizontal = 5 },
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    }
                });

                BackgroundColour = Color4Extensions.FromHex(@"221a21").Lighten(0.25f);
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (e.ShiftPressed)
                {
                    if (Enabled.Value)
                    {
                        Background.FlashColour(FlashColour, FlashDuration);
                        deleteAction?.Invoke();
                    }

                    return true;
                }

                return base.OnClick(e);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                UpdateCombination();
            }

            public void UpdateCombination()
            {
                var keybinding = globalActionContainer.GetKeyBindingForAction(keybind);

                keyCombinationText.Text = !keybinding.KeyCombination.Keys.IsDefaultOrEmpty ? keyCombinationProvider.GetReadableString(keybinding.KeyCombination) : "";
            }
        }
    }
}
