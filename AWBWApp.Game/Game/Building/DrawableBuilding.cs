using System;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;

namespace AWBWApp.Game.Game.Building
{
    public class DrawableBuilding : CompositeDrawable
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);

        public readonly BuildingTile BuildingTile;

        private TextureAnimation textureAnimation;

        public DrawableBuilding(BuildingTile buildingTile)
        {
            BuildingTile = buildingTile;
            Size = BASE_SIZE;
            InternalChild = textureAnimation = new TextureAnimation()
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft
            };
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            var texture = store.Get($"{BuildingTile.BaseTexture}-0");
            if (texture == null)
                throw new Exception($"Improperly configured BuildingTile. Base image missing: {BuildingTile.BaseTexture}-0");
            textureAnimation.Size = texture.Size;

            if (BuildingTile.Frames == null)
            {
                textureAnimation.AddFrame(texture);
                return;
            }

            for (var i = 1; i < BuildingTile.Frames.Length; i++)
            {
                texture = store.Get($"{BuildingTile.BaseTexture}-{i}");
                if (texture == null)
                    throw new Exception($"Improperly configured BuildingTile. Animation count wrong or image missing: {BuildingTile.BaseTexture}-{i}");
                textureAnimation.AddFrame(texture, BuildingTile.Frames[i]);
            }
            textureAnimation.Seek(BuildingTile.FrameOffset);
        }
    }
}
