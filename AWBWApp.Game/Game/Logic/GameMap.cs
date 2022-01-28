using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI;
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

        public TargetReticule TargetReticule;

        [Resolved]
        private TerrainTileStorage terrainTileStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private UnitStorage unitStorage { get; set; }

        private FogOfWarDrawable fogOfWarDrawable;
        private FogOfWarGenerator fogOfWarGenerator;

        public Dictionary<int, PlayerInfo> Players { get; private set; } = new Dictionary<int, PlayerInfo>();

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
                },
                fogOfWarDrawable = new FogOfWarDrawable(),
                TargetReticule = new TargetReticule()
            });
        }

        public void ScheduleInitialGameState(ReplayData gameState, ReplayMap map)
        {
            Schedule(() => SetToInitialGameState(gameState, map));
        }

        void SetToInitialGameState(ReplayData gameState, ReplayMap map)
        {
            MapSize = map.Size;

            Players.Clear();

            for (int i = 0; i < gameState.ReplayInfo.Players.Length; i++)
            {
                var player = gameState.ReplayInfo.Players[i];
                Players.Add(player.ID, new PlayerInfo(player));
            }

            //Calculate the map size as this isn't given by the api
            //Todo: Check buildings
            if (AutoSizeAxes == Axes.None)
                Size = Vec2IHelper.ScalarMultiply(MapSize + new Vector2I(0, 1), DrawableTile.BASE_SIZE);

            gameBoardDrawable.Clear();
            buildingsDrawable.Clear();
            unitsDrawable.Clear();

            gameBoard = new DrawableTile[MapSize.X, MapSize.Y];
            buildings = new Dictionary<Vector2I, DrawableBuilding>();
            units = new Dictionary<long, DrawableUnit>();

            var mapIdx = 0;

            var replayBuildings = gameState.TurnData[0].Buildings;

            for (int y = 0; y < MapSize.Y; y++)
            {
                for (int x = 0; x < MapSize.X; x++)
                {
                    var terrainId = map.Ids[mapIdx++];
                    if (buildingStorage.ContainsBuildingWithAWBWId(terrainId) && replayBuildings.TryGetValue(new Vector2I(x, y), out _))
                        continue;

                    var terrainTile = terrainTileStorage.GetTileByAWBWId(terrainId);
                    var tile = new DrawableTile(terrainTile) { Position = new Vector2(x * DrawableTile.BASE_SIZE.X, y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                    gameBoard[x, y] = tile;
                    gameBoardDrawable.Add(tile);
                }
            }

            foreach (var awbwBuilding in replayBuildings)
            {
                var building = buildingStorage.GetBuildingByAWBWId(awbwBuilding.Value.TerrainID);
                var position = awbwBuilding.Value.Position;

                var drawableBuilding = new DrawableBuilding(building, GetPlayerIDFromCountryID(building.CountryID), position);
                buildings.Add(position, drawableBuilding);
                buildingsDrawable.Add(drawableBuilding);
            }

            var replayUnits = gameState.TurnData[0].ReplayUnit;

            if (replayUnits != null)
            {
                foreach (var unit in replayUnits)
                {
                    var unitData = unitStorage.GetUnitByCode(unit.Value.UnitName);
                    var drawableUnit = new DrawableUnit(unitData, unit.Value, gameState.ReplayInfo.Players[gameState.ReplayInfo.PlayerIds[unit.Value.PlayerID.Value]].CountryCode());
                    units.Add(unit.Value.ID, drawableUnit);
                    unitsDrawable.Add(drawableUnit);
                }
            }

            fogOfWarGenerator = new FogOfWarGenerator(this);
            fogOfWarDrawable.NewStart(this, fogOfWarGenerator);

            AutoSizeAxes = Axes.Both;

            UpdateFogOfWar(gameState.TurnData[0].ActivePlayerID, 0, false); //Todo: CO range increases
        }

        private long? GetPlayerIDFromCountryID(int countryID)
        {
            foreach (var player in Players)
            {
                if (player.Value.CountryID != countryID)
                    continue;

                return player.Key;
            }

            return null;
        }

        public static Vector2 GetDrawablePositionForTilePosition(Vector2I tilePos) => new Vector2(tilePos.X * DrawableTile.BASE_SIZE.X, (tilePos.Y + 1) * DrawableTile.BASE_SIZE.Y - 1);

        public void ScheduleUpdateToGameState(TurnData gameState)
        {
            Schedule(() => updateToGameState(gameState));
        }

        Vector2I GetTerrainSize(Dictionary<int, Dictionary<int, AWBWTile>> tiles)
        {
            var mapSize = new Vector2I();

            foreach (var column in tiles)
            {
                if (mapSize.X < column.Key + 1)
                    mapSize.X = column.Key + 1;

                foreach (var tile in column.Value)
                {
                    if (mapSize.Y < tile.Key + 1)
                        mapSize.Y = tile.Key + 1;
                }
            }
            return mapSize;
        }

        //Todo: Save this data
        void updateToGameState(TurnData gameState)
        {
            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    var position = new Vector2I(x, y);

                    if (gameState.Buildings.TryGetValue(position, out var building))
                    {
                        UpdateBuilding(building, true);
                    }
                    else if (buildings.Remove(new Vector2I(x, y), out var drawableBuilding))
                    {
                        buildingsDrawable.Remove(drawableBuilding);
                    }
                }
            }

            foreach (var unit in gameState.ReplayUnit)
            {
                if (units.TryGetValue(unit.Value.ID, out DrawableUnit existingUnit))
                {
                    existingUnit.UpdateUnit(unit.Value);
                }
                else
                {
                    var unitData = unitStorage.GetUnitByCode(unit.Value.UnitName);
                    var drawableUnit = new DrawableUnit(unitData, unit.Value, Players[unit.Value.PlayerID.Value].CountryCode);
                    units.Add(unit.Value.ID, drawableUnit);
                    unitsDrawable.Add(drawableUnit);
                }
            }

            foreach (var unit in units.Where(x => !gameState.ReplayUnit.ContainsKey(x.Value.UnitID)))
            {
                units.Remove(unit.Key);
                unitsDrawable.Remove(unit.Value);
            }

            UpdateFogOfWar(gameState.ActivePlayerID, 0, false); //Todo: CO Range Increases
        }

        public void UpdateFogOfWar(int playerId, int rangeIncrease, bool canSeeIntoHiddenTiles) => fogOfWarGenerator.GenerateFogForPlayer(playerId, rangeIncrease, canSeeIntoHiddenTiles);

        public DrawableUnit AddUnit(ReplayUnit unit)
        {
            var unitData = unitStorage.GetUnitByCode(unit.UnitName);
            var drawableUnit = new DrawableUnit(unitData, unit, Players[unit.PlayerID.Value].CountryCode);
            units.Add(unit.ID, drawableUnit);
            Schedule(() => unitsDrawable.Add(drawableUnit));
            return drawableUnit;
        }

        public bool TryGetDrawableUnit(long unitId, out DrawableUnit drawableUnit) => units.TryGetValue(unitId, out drawableUnit);

        public DrawableUnit GetDrawableUnit(long unitId) => units[unitId];

        public bool TryGetDrawableBuilding(Vector2I position, out DrawableBuilding drawableBuilding) => buildings.TryGetValue(position, out drawableBuilding);

        public void DestroyUnit(long unitId, bool playExplosion = true, bool immediate = false)
        {
            if (!units.Remove(unitId, out DrawableUnit unit))
                return;

            if (immediate)
                removeUnit(unit, playExplosion);
            else
                unit.DelayUntilTransformsFinished().Finally(x => removeUnit(x, playExplosion));
        }

        public List<DrawableUnit> GetUnitsWithDistance(Vector2I position, int distance)
        {
            var unitsWithRange = new List<DrawableUnit>();

            foreach (var unit in units)
            {
                if ((unit.Value.MapPosition - position).ManhattonDistance() > distance)
                    continue;

                unitsWithRange.Add(unit.Value);
            }
            return unitsWithRange;
        }

        void removeUnit(DrawableUnit unit, bool playExplosion)
        {
            unitsDrawable.Remove(unit);
            if (!playExplosion)
                return;
            //Todo: Add explosion drawable
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

        public IEnumerable<DrawableUnit> GetDrawableUnitsFromPlayer(int playerId)
        {
            return units.Values.Where(x => x.OwnerID.HasValue && x.OwnerID == playerId);
        }

        public IEnumerable<DrawableBuilding> GetDrawableBuildingsForPlayer(int playerId)
        {
            foreach (var building in buildings)
            {
                if (building.Value.OwnerID.HasValue && building.Value.OwnerID == playerId)
                    yield return building.Value;
            }
        }

        public DrawableTile GetDrawableTile(Vector2I position) => gameBoard[position.X, position.Y];

        public void UpdateBuilding(ReplayBuilding awbwBuilding, bool newTurn)
        {
            var tilePosition = awbwBuilding.Position;

            if (!buildings.TryGetValue(tilePosition, out DrawableBuilding building))
            {
                var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.TerrainID);
                var drawableBuilding = new DrawableBuilding(buildingTile, GetPlayerIDFromCountryID(buildingTile.CountryID), tilePosition);
                buildings.Add(tilePosition, drawableBuilding);
                buildingsDrawable.Add(drawableBuilding);
                return;
            }

            if ((newTurn || (awbwBuilding.Capture < 0 || awbwBuilding.TerrainID != 0)) && building.BuildingTile.AWBWId != awbwBuilding.TerrainID)
            {
                buildingsDrawable.Remove(building);
                buildings.Remove(tilePosition);

                if (awbwBuilding.TerrainID != 0)
                {
                    var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.TerrainID);
                    var drawableBuilding = new DrawableBuilding(buildingTile, GetPlayerIDFromCountryID(buildingTile.CountryID), tilePosition);
                    buildings.Add(tilePosition, drawableBuilding);
                    buildingsDrawable.Add(drawableBuilding);
                }
                return;
            }

            //Todo: Update Building
            building.HasDoneAction.Value = false;
        }
    }
}
