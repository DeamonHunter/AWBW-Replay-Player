using System.Collections.Generic;
using AWBWApp.Game.Editor;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.UI.Replay;
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

        private EditorTileCursor editorCursor;
        private EditorTileCursor symmetryEditorCursor;
        private SymmetryLineContainer symmetryContainer;

        public EditorGameMap()
            : base(null)
        {
            Add(symmetryContainer = new SymmetryLineContainer());
            Add(symmetryEditorCursor = new EditorTileCursor() { Alpha = 0 });
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

            editorCursor.SetTile(TerrainTileStorage.GetTileByAWBWId(29));
            symmetryEditorCursor.SetTile(TerrainTileStorage.GetTileByAWBWId(29));

            symmetryContainer.SymmetryCenter = new Vector2I(MapSize.X - 1, MapSize.Y - 1);
            symmetryContainer.SymmetryDirection = SymmetryDirection.DownwardsDiagonal;
            symmetryContainer.SymmetryMode = SymmetryMode.MirrorInverted;
        }

        public void SetMapSize(Vector2I newMapSize)
        {
            //Todo: Probably should have a similar interface to the one I created for the hex editor
            tiles = new short[newMapSize.X, newMapSize.Y];
            TileGrid.ClearToSize(newMapSize);
            MapSize = newMapSize;
        }

        public override TileCursor CreateTileCursor() => editorCursor = new EditorTileCursor() { Alpha = 0 };

        protected override void UpdateTileCursor(Vector2 mousePosition)
        {
            base.UpdateTileCursor(mousePosition);

            //Todo: Symmetry conditions
            if (editorCursor.Alpha > 0)
            {
                var symmetryPosition = new Vector2I(2 * editorCursor.TilePosition.X, 2 * editorCursor.TilePosition.Y);
                var newTile = SymmetryHelper.GetSymmetricalTile(symmetryPosition, symmetryContainer.SymmetryCenter, symmetryContainer.SymmetryDirection, symmetryContainer.SymmetryMode);

                symmetryEditorCursor.TilePosition = new Vector2I(newTile.X / 2, newTile.Y / 2);
                symmetryEditorCursor.Alpha = 0.66f;
            }
            else
                symmetryEditorCursor.Hide();
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

            ScheduleAfterChildren(() => newTile.ScaleTo(1.125f).MoveToOffset(new Vector2(-1, -1)).ScaleTo(1, 250, Easing.Out).MoveToOffset(new Vector2(1, 1), 250, Easing.Out));

            TileGrid.AddTile(newTile, position);

            if (shoalTile == -1)
                ShoalGenerator.UpdateEditorMapAtPosition(this, position);
        }

        protected void PlaceTileAtCursorPosition()
        {
            ChangeTile(editorCursor.TilePosition, 29);
            ChangeTile(symmetryEditorCursor.TilePosition, 29);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnMouseDown(e);

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out _, out _, out _, out _))
                PlaceTileAtCursorPosition();

            return true;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnDragStart(e);

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out _, out _, out _, out _))
                PlaceTileAtCursorPosition();

            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (e.Button != MouseButton.Left)
            {
                base.OnDrag(e);
                return;
            }

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out _, out _, out _, out _))
                PlaceTileAtCursorPosition();
        }
    }
}
