using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Components
{
    /// <summary>
    /// This is a container to use to block input of <see cref="Drawable"/>s behind it, similar to <see cref="LoadingLayer"/>.
    /// </summary>
    public partial class BlockingLayer : VisibilityContainer
    {
        public bool BlockKeyEvents = true;

        protected override Container<Drawable> Content => contents;

        private readonly Container contents;
        private Box backgroundLayer;
        private float alpha;

        public BlockingLayer(bool startVisible = false, float alpha = 0.5f)
        {
            RelativeSizeAxes = Axes.Both;

            this.alpha = alpha;

            AddRangeInternal(new Drawable[]
            {
                backgroundLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(20, 20, 20, 255),
                    Alpha = 0
                },
                contents = new Container()
                {
                    RelativeSizeAxes = Axes.Both
                }
            });

            if (startVisible)
                State.Value = Visibility.Visible;
        }

        protected override void PopIn()
        {
            backgroundLayer.FadeTo(alpha, 1000, Easing.OutQuint);
            contents.FadeIn(500, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            backgroundLayer.FadeOut(500, Easing.OutQuint);
            contents.FadeOut(500, Easing.OutQuint);
        }

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case TouchEvent _:
                    return false;

                case KeyDownEvent _:
                case KeyUpEvent _:
                    return BlockKeyEvents;
            }

            return true;
        }
    }
}
