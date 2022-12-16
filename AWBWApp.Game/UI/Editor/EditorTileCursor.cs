using AWBWApp.Game.Editor;
using AWBWApp.Game.Game.Building;
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
        private Bindable<TerrainTile> selectedTile { get; set; }

        [Resolved]
        private Bindable<BuildingTile> selectedBuilding { get; set; }

        [Resolved]
        private Bindable<SymmetryMode> symmetryMode { get; set; }

        [Resolved]
        private Bindable<SymmetryDirection> symmetryDirection { get; set; }

        [Resolved]
        private TerrainTileStorage tileStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        private readonly Sprite tileCursorSprite;
        private readonly bool showSymmetry;

        public EditorTileCursor(bool showSymmetry)
        {
            this.showSymmetry = showSymmetry;

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
            selectedTile.BindValueChanged(_ => updateVisual());
            selectedBuilding.BindValueChanged(_ => updateVisual());
            symmetryMode.BindValueChanged(_ => updateVisual());
            symmetryDirection.BindValueChanged(_ => updateVisual());
            updateVisual();
        }

        private void updateVisual()
        {
            if (selectedBuilding.Value != null)
            {
                var building = selectedBuilding.Value;

                if (showSymmetry)
                {
                    var symmetricalTile = SymmetryHelper.GetBuildingTileForSymmetry(building, symmetryMode.Value, symmetryDirection.Value);
                    if (building.AWBWID != symmetricalTile)
                        building = buildingStorage.GetBuildingByAWBWId(symmetricalTile);
                }

                tileCursorSprite.Show();
                tileCursorSprite.Texture = textureStore.Get($"Map/{currentSkin.Value}/{building.Textures[WeatherType.Clear]}-0");
                tileCursorSprite.Size = tileCursorSprite.Texture.Size;
            }
            else if (selectedTile.Value != null)
            {
                tileCursorSprite.Show();

                var tile = selectedTile.Value;

                if (showSymmetry)
                {
                    var symmetricalTile = SymmetryHelper.GetTerrainTileForSymmetry(tile, symmetryMode.Value, symmetryDirection.Value);
                    if (tile.AWBWID != symmetricalTile)
                        tile = tileStorage.GetTileByAWBWId(symmetricalTile);
                }

                tileCursorSprite.Texture = textureStore.Get($"Map/{currentSkin.Value}/{tile.Textures[WeatherType.Clear]}");
                tileCursorSprite.Size = tileCursorSprite.Texture.Size;
            }
            else
                tileCursorSprite.Hide();
        }
    }
}
