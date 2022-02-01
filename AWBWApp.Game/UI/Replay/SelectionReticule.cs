using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;

namespace AWBWApp.Game.UI.Replay
{
    public class SelectionReticule : CompositeDrawable
    {
        private Sprite texture;

        public SelectionReticule()
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

        public TransformSequence<SelectionReticule> PlaySelectAnimation(DrawableUnit selectedUnit)
        {
            return this.DelayUntilTransformsFinished().AddDelayDependingOnDifferenceBetweenEndTimes(this, selectedUnit)
                       .FadeTo(0.5f).MoveTo(GameMap.GetDrawablePositionForBottomOfTile(selectedUnit.MapPosition) + DrawableTile.HALF_BASE_SIZE).ScaleTo(0.5f)
                       .FadeTo(1, 150, Easing.In).ScaleTo(1, 300, Easing.OutBounce)
                       .Then().FadeTo(0);
        }

        [BackgroundDependencyLoader]
        private void Load(NearestNeighbourTextureStore store)
        {
            texture.Texture = store.Get("UI/unit_select");
            texture.Size = texture.Texture.Size;
            Size = texture.Size;
        }
    }
}
