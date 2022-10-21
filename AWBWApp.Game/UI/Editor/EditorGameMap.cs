using System.Collections.Generic;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor
{
    public class EditorGameMap : GameMap
    {
        public EditorGameMap()
            : base(null)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            SetMapSize(new Vector2I(16, 16));

            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    var terrainTile = TerrainTileStorage.GetTileByAWBWId(1);
                    var tile = new DrawableTile(terrainTile);
                    TileGrid.AddTile(tile, new Vector2I(x, y));
                }
            }

            Units = new Dictionary<long, DrawableUnit>();

            AutoSizeAxes = Axes.Both;
            SetDrawableSize(new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, (MapSize.Y + 1) * DrawableTile.BASE_SIZE.Y));
            HasLoadedMap = true;
        }

        public void SetMapSize(Vector2I newMapSize)
        {
            //Todo: Probably should have a similar interface to the one I created for the hex editor

            TileGrid.ClearToSize(newMapSize);
            MapSize = newMapSize;
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (GetUnitAndTileFromMousePosition(e.MousePosition, out var tilePosition, out var tile, out var _, out var _))
                {
                    TileGrid.RemoveTile(tilePosition);

                    var terrainTile = TerrainTileStorage.GetTileByAWBWId(21);
                    var newTile = new DrawableTile(terrainTile);
                    TileGrid.AddTile(newTile, tilePosition);
                }
            }

            return base.OnClick(e);
        }
    }
}
