using AWBWApp.Game.UI.Replay;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class EditorHotkeyButton : EditorSpriteButton
    {
        private Key keyToReactTo;

        public Key KeyToReactTo
        {
            get => keyToReactTo;
            set
            {
                keyToReactTo = value;

                var fullString = keyToReactTo.ToString();
                numberText.Text = fullString.Substring(fullString.Length - 1); //Todo: Less hacky way of doing this
            }
        }

        private SpriteText numberText;

        public EditorHotkeyButton()
        {
            Size = new Vector2(50, 50);
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            Masking = true;
            CornerRadius = 6;

            AddInternal(
                numberText = new TextureSpriteText("UI/Healthv2")
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Position = new Vector2(-2, -2),
                    Font = new FontUsage(size: 4f),
                });
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == keyToReactTo)
            {
                Action?.Invoke(Tile, Building);
                return true;
            }

            return base.OnKeyDown(e);
        }
    }
}
