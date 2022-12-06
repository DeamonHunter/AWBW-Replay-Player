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
    public partial class EditorTileCursor : TileCursor
    {
        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        [Resolved]
        private IBindable<MapSkin> currentSkin { get; set; }

        [Resolved]
        private Bindable<TerrainTile> currentTile { get; set; }

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

        protected override void LoadComplete()
        {
            base.LoadComplete();
            currentSkin.BindValueChanged(_ => updateVisual());
            currentTile.BindValueChanged(_ => updateVisual());
            updateVisual();
        }

        private void updateVisual()
        {
            if (currentTile.Value == null)
            {
                tileCursorSprite.Hide();
                return;
            }

            tileCursorSprite.Show();
            tileCursorSprite.Texture = textureStore.Get($"Map/{currentSkin.Value}/{currentTile.Value.Textures[WeatherType.Clear]}");
            tileCursorSprite.Size = tileCursorSprite.Texture.Size;
        }
    }
}
