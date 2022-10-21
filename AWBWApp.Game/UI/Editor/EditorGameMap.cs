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
        private short[,] tiles;

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
                    ChangeTile(new Vector2I(x, y), 1, 1);
            }

            Units = new Dictionary<long, DrawableUnit>();

            AutoSizeAxes = Axes.Both;
            SetDrawableSize(new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, (MapSize.Y + 1) * DrawableTile.BASE_SIZE.Y));
            HasLoadedMap = true;
        }

        public void SetMapSize(Vector2I newMapSize)
        {
            //Todo: Probably should have a similar interface to the one I created for the hex editor
            tiles = new short[newMapSize.X, newMapSize.Y];
            TileGrid.ClearToSize(newMapSize);
            MapSize = newMapSize;
        }

        public short GetTileIDAtPosition(Vector2I tilePosition) => tiles[tilePosition.X, tilePosition.Y];

        public void ChangeTile(Vector2I position, short newTileID, short shoalTile = -1)
        {
            if (tiles[position.X, position.Y] == newTileID && (shoalTile == -1 || shoalTile == TileGrid[position.X, position.Y].TerrainTile.AWBWID))
                return;

            tiles[position.X, position.Y] = newTileID;

            TileGrid.RemoveTile(position);

            var terrainTile = TerrainTileStorage.GetTileByAWBWId(shoalTile != -1 ? shoalTile : newTileID);
            var newTile = new DrawableTile(terrainTile);
            TileGrid.AddTile(newTile, position);

            if (shoalTile == -1)
                ShoalGenerator.UpdateEditorMapAtPosition(this, position);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnMouseDown(e);

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out var tilePosition, out _, out _, out _))
                ChangeTile(tilePosition, 29);

            return true;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnDragStart(e);

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out var tilePosition, out _, out _, out _))
                ChangeTile(tilePosition, 29);

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (e.Button != MouseButton.Left)
            {
                base.OnDrag(e);
                return;
            }

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out var tilePosition, out _, out _, out _))
                ChangeTile(tilePosition, 29);
        }
    }
}
