using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Editor;
using AWBWApp.Game.Editor.History;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorGameMap : GameMap
    {
        private short[,] tiles;

        [Resolved]
        private Bindable<TerrainTile> selectedTile { get; set; }

        [Resolved]
        private Bindable<BuildingTile> selectedBuilding { get; set; }

        [Resolved]
        private HistoryManager historyManager { get; set; }

        private EditorTileCursor editorCursor;
        private EditorTileCursor symmetryEditorCursor;
        private SymmetryLineContainer symmetryContainer;
        private CaptureOverlayContainer captureContainer;
        private Vector2 lastCursorPosition;

        private TileChangeHistory tileChanges;

        public EditorGameMap()
            : base(null)
        {
            Add(symmetryContainer = new SymmetryLineContainer());
            Add(captureContainer = new CaptureOverlayContainer(this));
            Add(symmetryEditorCursor = new EditorTileCursor(true) { Alpha = 0 });
        }

        public void SetMap(ReplayMap map)
        {
            SetMapSize(map.Size);

            var shoalMap = ShoalGenerator.CreateCustomShoalVersion(map);

            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                    ChangeTile(new Vector2I(x, y), map.Ids[y * MapSize.X + x], shoalMap.Ids[y * MapSize.X + x], false);
            }

            AutoSizeAxes = Axes.Both;
            SetDrawableSize(new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, (MapSize.Y + 1) * DrawableTile.BASE_SIZE.Y));
            HasLoadedMap = true;

            Units = new Dictionary<long, DrawableUnit>();

            selectedTile.Value = TerrainTileStorage.GetTileByAWBWId(1);

            symmetryContainer.SymmetryCenter = new Vector2I(MapSize.X - 1, MapSize.Y - 1);
        }

        public void SetMapSize(Vector2I newMapSize)
        {
            //Todo: Probably should have a similar interface to the one I created for the hex editor
            tiles = new short[newMapSize.X, newMapSize.Y];
            TileGrid.ClearToSize(newMapSize);
            BuildingGrid.ClearToSize(newMapSize);
            MapSize = newMapSize;
        }

        public override TileCursor CreateTileCursor() => editorCursor = new EditorTileCursor(false) { Alpha = 0 };

        protected override void UpdateTileCursor(Vector2 mousePosition)
        {
            //Todo: May need to hande unit deselection

            if (GetUnitAndTileFromMousePosition(ToLocalSpace(mousePosition), out var tilePosition, out _, out _, out _) && IsHovered)
            {
                TileCursor.TilePosition = tilePosition;
                TileCursor.Show();
            }
            else
                TileCursor.Hide();

            InfoPopup.ShowDetails(selectedTile.Value, selectedBuilding.Value);

            //Todo: Symmetry conditions
            if (editorCursor.Alpha > 0 && symmetryContainer.SymmetryMode != SymmetryMode.None)
            {
                var newTile = SymmetryHelper.GetSymmetricalTile(new Vector2I(editorCursor.TilePosition.X * 2, editorCursor.TilePosition.Y * 2), symmetryContainer.SymmetryCenter, symmetryContainer.SymmetryDirection, symmetryContainer.SymmetryMode);
                newTile = new Vector2I(newTile.X / 2, newTile.Y / 2);

                symmetryEditorCursor.TilePosition = newTile;
                symmetryEditorCursor.Alpha = 0.66f;
            }
            else
                symmetryEditorCursor.Hide();
        }

        public short GetTileIDAtPosition(Vector2I tilePosition) => tiles[tilePosition.X, tilePosition.Y];

        public bool TryGetTileAtMousePosition(Vector2 screenMousePosition, out TerrainTile tile, out BuildingTile building)
        {
            tile = null;
            building = null;

            if (!GetUnitAndTileFromMousePosition(ToLocalSpace(screenMousePosition), out _, out var drawableTile, out var drawableBuilding, out _))
                return false;

            tile = drawableTile?.TerrainTile;
            building = drawableBuilding?.BuildingTile;

            return true;
        }

        public void ChangeTile(Vector2I position, short newTileID, short shoalTile = -1, bool recordChange = true)
        {
            if (tiles[position.X, position.Y] == newTileID)
            {
                if (BuildingGrid.TryGet(position, out _))
                    return; //Buildings do not have alts

                if (shoalTile == -1 || shoalTile == (TileGrid[position.X, position.Y]?.TerrainTile.AWBWID ?? -1))
                    return;
            }

            if (tileChanges != null && recordChange)
            {
                var tileChange = new TileChange
                {
                    TileBefore = tiles[position.X, position.Y],
                    TileAfter = newTileID,
                    AltBefore = (short)(TileGrid[position.X, position.Y]?.TerrainTile.AWBWID ?? shoalTile),
                    AltAfter = shoalTile
                };

                tileChanges.AddChange(position, tileChange);
            }

            tiles[position.X, position.Y] = newTileID;

            TileGrid.RemoveTile(position);
            BuildingGrid.RemoveTile(position);

            if (BuildingStorage.TryGetBuildingByAWBWId(newTileID, out var building))
            {
                var newBuilding = new DrawableBuilding(building, position, null, null);
                ScheduleAfterChildren(() => newBuilding.ScaleTo(1.125f).MoveToOffset(new Vector2(-1, -1)).ScaleTo(1, 250, Easing.Out).MoveToOffset(new Vector2(1, 1), 250, Easing.Out));
                BuildingGrid.AddTile(newBuilding, position);

                var newTile = new DrawableTile(TerrainTileStorage.GetTileByCode("Plain"));
                ScheduleAfterChildren(() => newTile.ScaleTo(1.125f).MoveToOffset(new Vector2(-1, -1)).ScaleTo(1, 250, Easing.Out).MoveToOffset(new Vector2(1, 1), 250, Easing.Out));
                TileGrid.AddTile(newTile, position);
            }
            else
            {
                var terrainTile = TerrainTileStorage.GetTileByAWBWId(shoalTile != -1 ? shoalTile : newTileID);
                var newTile = new DrawableTile(terrainTile);
                ScheduleAfterChildren(() => newTile.ScaleTo(1.125f).MoveToOffset(new Vector2(-1, -1)).ScaleTo(1, 250, Easing.Out).MoveToOffset(new Vector2(1, 1), 250, Easing.Out));
                TileGrid.AddTile(newTile, position);
            }

            if (shoalTile == -1)
                ShoalGenerator.UpdateEditorMapAtPosition(this, position);
        }

        protected void PlaceTilesBetweenPositions(Vector2 lastMousePosition, Vector2 currentMousePosition)
        {
            foreach (var tilePosition in getValidTilesBetweenCursorPoints(lastMousePosition, currentMousePosition))
            {
                var id = selectedBuilding.Value?.AWBWID ?? selectedTile.Value.AWBWID;

                //Symmetry first to make sure the current cursor has priority over the symmetry cursor
                if (symmetryContainer.SymmetryMode != SymmetryMode.None)
                {
                    var newTile = SymmetryHelper.GetSymmetricalTile(new Vector2I(tilePosition.X * 2, tilePosition.Y * 2), symmetryContainer.SymmetryCenter, symmetryContainer.SymmetryDirection, symmetryContainer.SymmetryMode);
                    newTile = new Vector2I(newTile.X / 2, newTile.Y / 2);

                    int newTileID;
                    if (selectedBuilding.Value != null)
                        newTileID = SymmetryHelper.GetBuildingTileForSymmetry(selectedBuilding.Value, symmetryContainer.SymmetryMode, symmetryContainer.SymmetryDirection);
                    else
                        newTileID = SymmetryHelper.GetTerrainTileForSymmetry(selectedTile.Value, symmetryContainer.SymmetryMode, symmetryContainer.SymmetryDirection);

                    if (isTilePositionInBounds(newTile))
                        ChangeTile(newTile, (short)newTileID);
                }

                ChangeTile(tilePosition, (short)id);
            }
        }

        private IEnumerable<Vector2I> getValidTilesBetweenCursorPoints(Vector2 from, Vector2 to)
        {
            var origin = new Vector2(from.X / DrawableTile.BASE_SIZE.X, (from.Y - DrawableTile.BASE_SIZE.Y) / DrawableTile.BASE_SIZE.Y);
            var end = new Vector2(to.X / DrawableTile.BASE_SIZE.X, (to.Y - DrawableTile.BASE_SIZE.Y) / DrawableTile.BASE_SIZE.Y);

            var direction = (end - origin);

            if (direction == Vector2.Zero)
            {
                var returnValue = new Vector2I((int)Math.Floor(origin.X), (int)Math.Floor(origin.Y));
                if (isTilePositionInBounds(returnValue))
                    yield return returnValue;
                yield break;
            }

            var radius = direction.Length;
            direction.Normalize();

            var xy = new Vector2I((int)Math.Floor(origin.X), (int)Math.Floor(origin.Y));
            var step = new Vector2I(Math.Sign(direction.X), Math.Sign(direction.Y));

            var tMax = new Vector2(wrapIntToBounds(origin.X, direction.X), wrapIntToBounds(origin.Y, direction.Y));
            var tDelta = new Vector2(direction.X == 0 ? float.MaxValue : step.X / direction.X, direction.Y == 0 ? float.MaxValue : step.Y / direction.Y);

            if (isTilePositionInBounds(xy))
                yield return xy;

            while (true)
            {
                if (tMax.X < tMax.Y)
                {
                    if (tMax.X > radius)
                        break;
                    xy.X += step.X;
                    tMax.X += tDelta.X;
                }
                else
                {
                    if (tMax.Y > radius)
                        break;
                    xy.Y += step.Y;
                    tMax.Y += tDelta.Y;
                }
                if (isTilePositionInBounds(xy))
                    yield return xy;
            }
        }

        private bool isTilePositionInBounds(Vector2I position) => position.X >= 0 && position.X < MapSize.X && position.Y >= 0 && position.Y < MapSize.Y;

        private float wrapIntToBounds(float s, float ds)
        {
            if (ds == 0)
                return float.MaxValue;

            if (ds > 0)
            {
                s = (s % 1 + 1) % 1;
                return (1 - s) / ds;
            }

            s = (-s % 1 + 1) % 1;
            //When we are exactly on the edge. We want it to be 1 not 0 when negative
            if (s <= 0)
                s = 1f;
            return (s - 1) / ds;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnMouseDown(e);

            tileChanges = new TileChangeHistory();

            PlaceTilesBetweenPositions(e.MousePosition, e.MousePosition);

            lastCursorPosition = e.MousePosition;
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (e.Button != MouseButton.Left)
                base.OnMouseUp(e);

            if (tileChanges != null && tileChanges.HasChanges)
                historyManager.RegisterHistory(tileChanges);
            tileChanges = null;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnDragStart(e);

            PlaceTilesBetweenPositions(lastCursorPosition, e.MousePosition);
            lastCursorPosition = e.MousePosition;
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (e.Button != MouseButton.Left)
            {
                base.OnDrag(e);
                return;
            }

            PlaceTilesBetweenPositions(lastCursorPosition, e.MousePosition);
            lastCursorPosition = e.MousePosition;
        }

        protected override string GetCurrentTeamVisibility()
        {
            return "";
        }

        public ReplayMap GenerateMap()
        {
            var replayMap = new ReplayMap();
            replayMap.Size = MapSize;
            replayMap.TerrainName = "Test Generated Map";
            replayMap.Ids = new short[MapSize.X * MapSize.Y];

            for (int y = 0; y < MapSize.Y; y++)
            {
                for (int x = 0; x < MapSize.X; x++)
                    replayMap.Ids[y * MapSize.X + x] = tiles[x, y];
            }

            return replayMap;
        }

        public bool CanUnitMoveToTile(UnitData unitData, Vector2I start, Vector2I destination, int availableMovementPoints, out int movementCost)
        {
            var visited = new HashSet<Vector2I>();
            var queue = new PriorityQueue<Vector2I, int>();
            queue.Enqueue(start, 0);

            void addTileToQueueIfPossible(Vector2I position, int currentCost)
            {
                if (visited.Contains(position))
                    return;

                if (!TryGetTerrainTypeAndMovementCostsForTile(position, out _, out var moveCosts))
                    return;

                if (!moveCosts.TryGetValue(unitData.MovementType, out var cost))
                    return;

                if (currentCost + cost <= availableMovementPoints)
                    queue.Enqueue(position, currentCost + cost);
            }

            while (queue.TryDequeue(out var tilePos, out var movement))
            {
                if (visited.Contains(tilePos))
                    continue;

                visited.Add(tilePos);

                if (tilePos == destination)
                {
                    movementCost = movement;
                    return true;
                }

                addTileToQueueIfPossible(tilePos + new Vector2I(1, 0), movement);
                addTileToQueueIfPossible(tilePos + new Vector2I(-1, 0), movement);
                addTileToQueueIfPossible(tilePos + new Vector2I(0, 1), movement);
                addTileToQueueIfPossible(tilePos + new Vector2I(0, -1), movement);
            }

            movementCost = int.MaxValue;
            return false;
        }
    }
}
