using System;
using System.Collections.Generic;
using AWBWApp.Game.API;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace AWBWApp.Game.Game.Logic
{
    public class GameMap : Container
    {
        public Vector2I MapSize { get; private set; }

        private Container<DrawableTile> gameBoardDrawable;
        private DrawableTile[,] gameBoard;

        private Container<DrawableBuilding> buildingsDrawable;
        private Dictionary<Vector2I, DrawableBuilding> buildings;

        private Container<DrawableUnit> unitsDrawable;
        private Dictionary<long, DrawableUnit> units;
        private Dictionary<Vector2I, DrawableUnit> unitsByPosition;

        [Resolved]
        private TerrainTileStorage terrainTileStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private UnitStorage unitStorage { get; set; }

        public GameMap()
        {
            AddRange(new Drawable[]
            {
                gameBoardDrawable = new Container<DrawableTile>
                {
                    AutoSizeAxes = Axes.Both
                },
                buildingsDrawable = new Container<DrawableBuilding>
                {
                    AutoSizeAxes = Axes.Both
                },
                unitsDrawable = new Container<DrawableUnit>
                {
                    AutoSizeAxes = Axes.Both
                }
            });
        }

        public void ScheduleInitialGameState(AWBWGameState gameState)
        {
            Schedule(() => SetToInitialGameState(gameState));
        }

        void SetToInitialGameState(AWBWGameState gameState)
        {
            var mapSize = new Vector2I();

            //Calculate the map size as this isn't given by the api
            //Todo: Check buildings
            foreach (var column in gameState.Terrain)
            {
                if (mapSize.X < column.Key + 1)
                    mapSize.X = column.Key + 1;

                foreach (var tile in column.Value)
                {
                    if (mapSize.Y < tile.Key + 1)
                        mapSize.Y = tile.Key + 1;
                }
            }
            Size = Vec2IHelper.ScalarMultiply(mapSize + new Vector2I(0, 1), DrawableTile.BASE_SIZE);

            gameBoardDrawable.Clear();
            buildingsDrawable.Clear();
            unitsDrawable.Clear();

            MapSize = mapSize;
            gameBoard = new DrawableTile[mapSize.X, mapSize.Y];
            buildings = new Dictionary<Vector2I, DrawableBuilding>();
            units = new Dictionary<long, DrawableUnit>();

            for (int x = 0; x < mapSize.X; x++)
            {
                for (int y = 0; y < mapSize.Y; y++)
                {
                    TerrainTile terrainTile;

                    if (gameState.Terrain.TryGetValue(x, out var row))
                    {
                        if (row.TryGetValue(y, out var awbwTile))
                            terrainTile = terrainTileStorage.GetTileByAWBWId(awbwTile.Terrain_Id);
                        else
                            terrainTile = terrainTileStorage.GetTileByAWBWId(1); //Todo: Make this a const
                    }
                    else
                        terrainTile = terrainTileStorage.GetTileByAWBWId(1); //Todo: Make this a const

                    var tile = new DrawableTile(terrainTile) { Position = new Vector2(x * DrawableTile.BASE_SIZE.X, y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                    gameBoard[x, y] = tile;
                    gameBoardDrawable.Add(tile);
                }
            }

            for (int x = 0; x < mapSize.X; x++)
            {
                for (int y = 0; y < mapSize.Y; y++)
                {
                    if (gameState.Buildings.TryGetValue(x, out var buildingRow))
                    {
                        if (buildingRow.TryGetValue(y, out var awbwBuilding))
                        {
                            var building = buildingStorage.GetBuildingByAWBWId(awbwBuilding.Terrain_Id);
                            var drawableBuilding = new DrawableBuilding(building) { Position = new Vector2(x * DrawableTile.BASE_SIZE.X, y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                            buildings.Add(new Vector2I(x, y), drawableBuilding);
                            buildingsDrawable.Add(drawableBuilding);
                        }
                    }
                }
            }

            foreach (var unit in gameState.Units)
            {
                var unitData = unitStorage.GetUnitByCode(unit.Value.UnitCode);
                var drawableUnit = new DrawableUnit(unitData, unit.Value);
                units.Add(unit.Key, drawableUnit);
                unitsDrawable.Add(drawableUnit);
            }

            AutoSizeAxes = Axes.Both;
        }

        public void ScheduleUpdateToGameState(AWBWGameState gameState)
        {
            Schedule(() => updateToGameState(gameState));
        }

        void updateToGameState(AWBWGameState gameState)
        {
            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    TerrainTile terrainTile;

                    if (gameState.Terrain.TryGetValue(x, out var row))
                    {
                        if (row.TryGetValue(y, out var awbwTile))
                            terrainTile = terrainTileStorage.GetTileByAWBWId(awbwTile.Terrain_Id);
                        else
                            terrainTile = terrainTileStorage.GetTileByAWBWId(1); //Todo: Make this a const
                    }
                    else
                        terrainTile = terrainTileStorage.GetTileByAWBWId(1); //Todo: Make this a const

                    if (gameBoard[x, y].TerrainTile != terrainTile)
                        throw new NotImplementedException("Changing terrain tiles is not implemented.");
                }
            }

            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    if (gameState.Buildings.TryGetValue(x, out var buildingRow))
                    {
                        if (buildingRow.TryGetValue(y, out var awbwBuilding))
                            UpdateBuilding(awbwBuilding, true);
                    }
                }
            }

            foreach (var unit in gameState.Units)
            {
                var unitData = unitStorage.GetUnitByCode(unit.Value.UnitCode);

                if (units.TryGetValue(unit.Key, out DrawableUnit existingUnit))
                {
                    existingUnit.MoveToPosition(new Vector2I(unit.Value.X, unit.Value.Y));
                }
                else
                {
                    var drawableUnit = new DrawableUnit(unitData, unit.Value);
                    units.Add(unit.Key, drawableUnit);
                    unitsDrawable.Add(drawableUnit);
                }
            }

            AutoSizeAxes = Axes.Both;
        }

        public void AddUnit(AWBWUnit unit)
        {
            var unitData = unitStorage.GetUnitByCode(unit.UnitCode);
            var drawableUnit = new DrawableUnit(unitData, unit);
            units.Add(unit.ID, drawableUnit);
            Schedule(() => unitsDrawable.Add(drawableUnit));
        }

        public DrawableUnit GetDrawableUnit(long unitId)
        {
            return units[unitId];
        }

        public DrawableUnit GetDrawableUnit(Vector2I unitPosition)
        {
            //Todo: query by position rather than iterate over everything
            foreach (var unit in units)
            {
                if (unit.Value.MapPosition == unitPosition)
                    return unit.Value;
            }

            return null;
        }

        public void UpdateBuilding(AWBWBuilding awbwBuilding, bool newTurn)
        {
            var tilePosition = new Vector2I(awbwBuilding.X, awbwBuilding.Y);

            if (!buildings.TryGetValue(tilePosition, out DrawableBuilding building))
            {
                var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.Terrain_Id);
                var drawableBuilding = new DrawableBuilding(buildingTile) { Position = new Vector2(awbwBuilding.X * DrawableTile.BASE_SIZE.X, awbwBuilding.Y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                buildings.Add(tilePosition, drawableBuilding);
                buildingsDrawable.Add(drawableBuilding);
                return;
            }

            if ((newTurn || (awbwBuilding.BuildingHP < 0 || awbwBuilding.ID != 0)) && building.BuildingTile.AWBWId != awbwBuilding.Terrain_Id)
            {
                Remove(building);
                buildings.Remove(tilePosition);

                if (awbwBuilding.Terrain_Id != 0)
                {
                    var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.Terrain_Id);
                    var drawableBuilding = new DrawableBuilding(buildingTile) { Position = new Vector2(awbwBuilding.X * DrawableTile.BASE_SIZE.X, awbwBuilding.Y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                    buildings.Add(tilePosition, drawableBuilding);
                    buildingsDrawable.Add(drawableBuilding);
                }
                return;
            }

            //Todo: Update Building
        }
    }
}
