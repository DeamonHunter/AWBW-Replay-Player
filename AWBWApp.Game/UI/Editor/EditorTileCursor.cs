using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace AWBWApp.Game.UI.Editor
{
    public class EditorTileCursor : TileCursor
    {
        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        [Resolved]
        private IBindable<MapSkin> currentSkin { get; set; }

        private Sprite tileCursorSprite;

        public EditorTileCursor()
        {
            InternalChildren = new Drawable[]
            {
                tileCursorSprite = new Sprite()
                {
                    Alpha = 0.66f,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Position = new Vector2(-8, 8)
                },
                Cursor = new Sprite()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                }
            };
        }

        public void SetTile(TerrainTile tile)
        {
            //Todo: Do we need to store the tile for later reference?

            tileCursorSprite.Texture = textureStore.Get($"Map/{currentSkin.Value}/{tile.Textures[WeatherType.Clear]}");
            tileCursorSprite.Size = tileCursorSprite.Texture.Size;
        }
    }
}
