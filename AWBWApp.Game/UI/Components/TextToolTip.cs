using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    /// <summary>
    /// Recreation of <see cref="TooltipContainer.Tooltip"/> which sets the tooltip to our colours
    /// </summary>
    public class TextToolTip : VisibilityContainer, ITooltip<LocalisableString>
    {
        private readonly SpriteText text;

        public virtual void SetContent(LocalisableString content) => text.Text = content;

        private const float text_size = 16;

        public TextToolTip()
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
