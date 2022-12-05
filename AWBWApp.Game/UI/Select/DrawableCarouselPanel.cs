using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public partial class DrawableCarouselPanel : Container
    {
        public Container BorderContainer;

        public readonly Bindable<CarouselItemState> State = new Bindable<CarouselItemState>(CarouselItemState.NotSelected);

        private readonly HoverLayer hoverLayer;

        protected override Container<Drawable> Content { get; } = new Container { RelativeSizeAxes = Axes.Both };

        private const float corner_radius = 10;
        private const float border_thickness = 2.5f;

        public DrawableCarouselPanel()
        {
            RelativeSizeAxes = Axes.X;
            Height = DrawableCarouselItem.MAX_HEIGHT;

            InternalChild = BorderContainer = new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = corner_radius,
                BorderColour = new Color4(221, 255, 255, 255),
                Children = new Drawable[]
                {
                    Content,
                    hoverLayer = new HoverLayer()
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            State.BindValueChanged(updateState, true);
        }

        private void updateState(ValueChangedEvent<CarouselItemState> state)
        {
            switch (state.NewValue)
            {
                case CarouselItemState.Collapsed:
                case CarouselItemState.NotSelected:
                    hoverLayer.InsetForBorder = false;
                    BorderContainer.BorderThickness = 0;
                    BorderContainer.EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Offset = new Vector2(1),
                        Radius = 5,
                        Colour = Color4.Black.Opacity(100)
                    };
                    break;

                case CarouselItemState.Selected:
                    hoverLayer.InsetForBorder = true;
                    BorderContainer.BorderThickness = border_thickness;
                    BorderContainer.EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Glow,
                        Colour = new Color4(130, 204, 255, 65),
                        Radius = 5,
                        Roundness = 0,
                    };
                    break;
            }
        }

        public partial class HoverLayer : CompositeDrawable
        {
            private Box box;

            public HoverLayer()
            {
                RelativeSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChild = box = new Box
                {
                    Colour = Color4Extensions.FromHex("334931"),
                    Alpha = 0,
                    Blending = BlendingParameters.Additive,
                    RelativeSizeAxes = Axes.Both
                };
            }

            public bool InsetForBorder
            {
                set
                {
                    if (value)
                    {
                        Masking = true;
                        CornerRadius = corner_radius;
                        BorderThickness = border_thickness;
                    }
                    else
                    {
                        CornerRadius = 0;
                        BorderThickness = 0;
                        Masking = false;
                    }
                }
            }

            protected override bool OnHover(HoverEvent e)
            {
                box.FadeIn(100, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                box.FadeOut(100, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
