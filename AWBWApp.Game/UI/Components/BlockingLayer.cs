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
    public class BlockingLayer : VisibilityContainer
    {
        public bool BlockKeyEvents = true;

        protected override Container<Drawable> Content => contents;

        private readonly Container contents;
        private Box backgroundLayer;

        public BlockingLayer()
        {
            RelativeSizeAxes = Axes.Both;

            AddRangeInternal(new Drawable[]
            {
                backgroundLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0
                },
                contents = new Container()
                {
                    RelativeSizeAxes = Axes.Both
                }
            });
        }

        protected override void PopIn()
        {
            backgroundLayer.FadeTo(0.5f, 1000, Easing.OutQuint);
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
