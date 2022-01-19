using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osuTK;

namespace AWBWApp.Game.Game.Units
{
    public class TargetReticule : CompositeDrawable
    {
        private Sprite texture;

        public TargetReticule()
        {
            Anchor = Anchor.TopLeft;
            Origin = Anchor.Centre;
            Alpha = 0;

            texture = new Sprite();

            InternalChild = texture = new Sprite()
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft
            };
        }

        public TransformSequence<TargetReticule> PlayAttackAnimation(Vector2I start, Vector2I end, DrawableUnit attacker)
        {
            return this.DelayUntilTransformsFinished().AddDelayDependingOnDifferenceBetweenEndTimes(this, attacker)
                       .FadeTo(0.5f).MoveTo(new Vector2((start.X + 0.5f) * DrawableTile.BASE_SIZE.X, (start.Y + 0.5f) * DrawableTile.BASE_SIZE.Y + Size.Y / 2)).RotateTo(0).ScaleTo(0.5f)
                       .FadeTo(1, 0.25, Easing.In).MoveTo(new Vector2((end.X + 0.5f) * DrawableTile.BASE_SIZE.X, (end.Y + 0.5f) * DrawableTile.BASE_SIZE.Y + Size.Y / 2), 400, Easing.In).ScaleTo(1, 600, Easing.OutBounce).RotateTo(180, 400f)
                       .Then().FadeTo(0);
        }

        [BackgroundDependencyLoader]
        private void Load(NearestNeighbourTextureStore store)
        {
            texture.Texture = store.Get("Target");
            texture.Size = texture.Texture.Size;
            Size = texture.Size;
        }
    }
}
