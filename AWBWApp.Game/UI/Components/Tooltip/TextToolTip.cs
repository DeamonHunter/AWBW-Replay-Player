using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components.Tooltip
{
    /// <summary>
    /// Recreation of <see cref="TooltipContainer.Tooltip"/> which sets the tooltip to our colours
    /// </summary>
    public partial class TextToolTip : VisibilityContainer, ITooltip<LocalisableString>
    {
        private TextFlowContainer text;
        private LocalisableString prev;

        public virtual void SetContent(LocalisableString content)
        {
            //Text flow container takes a frame to update. So we can't swap this every frame
            if (prev == content)
                return;

            text?.Expire();
            prev = content;
            text = createContainer(content);
            Add(text);
        }

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
            };
        }

        private TextFlowContainer createContainer(LocalisableString text)
        {
            return new TextFlowContainer(x =>
            {
                x.Font = FrameworkFont.Regular.With(size: text_size);
            })
            {
                AutoSizeAxes = Axes.Both,
                Text = text,
                Padding = new MarginPadding(5),
                MaximumSize = new Vector2(300, float.MaxValue)
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
