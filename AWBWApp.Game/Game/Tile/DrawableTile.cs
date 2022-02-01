using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace AWBWApp.Game.Game.Tile
{
    public class DrawableTile : CompositeDrawable
    {
        public static readonly Vector2I BASE_SIZE = new Vector2I(16);
        public static readonly Vector2I HALF_BASE_SIZE = new Vector2I(8);

        public readonly TerrainTile TerrainTile;

        private Sprite texture;

        private Texture baseTexture;
        private Texture fogOfWarTexture;

        public DrawableTile(TerrainTile terrainTile)
        {
            TerrainTile = terrainTile;
            Size = BASE_SIZE;

            InternalChild = texture = new Sprite()
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft
            };
        }

        [BackgroundDependencyLoader]
        private void load(NearestNeighbourTextureStore store)
        {
            baseTexture = store.Get(TerrainTile.BaseTexture);

            texture.Texture = baseTexture;
            texture.Size = baseTexture.Size;
        }
    }
}
