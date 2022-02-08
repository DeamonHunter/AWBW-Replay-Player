using System;
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
using AWBWApp.Game.UI.Replay;
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

        private FogOfWarDrawable fogOfWarDrawable;
        private FogOfWarGenerator fogOfWarGenerator;

        private EffectAnimationController effectAnimationController;

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
                effectAnimationController = new EffectAnimationController
                {
                    Origin = Anchor.TopLeft,
                    Anchor = Anchor.TopLeft
                }
            });
        }

        public void ScheduleInitialGameState(ReplayData gameState, ReplayMap map, Action postUpdateAction)
        {
            Schedule(() =>
            {
                SetToInitialGameState(gameState, map);
                postUpdateAction?.Invoke();
            });
        }

        void SetToInitialGameState(ReplayData gameState, ReplayMap map)
        {
            MapSize = map.Size;

            Players.Clear();
            foreach (var player in gameState.ReplayInfo.Players)
                Players.Add(player.Key, new PlayerInfo(player.Value));

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
                if (!awbwBuilding.Value.TerrainID.HasValue)
                    throw new Exception("Invalid building encountered: Missing terrain id.");

                var building = buildingStorage.GetBuildingByAWBWId(awbwBuilding.Value.TerrainID.Value);
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
                    var drawableUnit = new DrawableUnit(unitData, unit.Value, gameState.ReplayInfo.Players[unit.Value.PlayerID.Value].CountryCode());
                    units.Add(unit.Value.ID, drawableUnit);
                    unitsDrawable.Add(drawableUnit);
                }
            }

            fogOfWarGenerator = new FogOfWarGenerator(this);
            fogOfWarDrawable.NewStart(this, fogOfWarGenerator);

            AutoSizeAxes = Axes.Both;
            effectAnimationController.Size = new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, MapSize.Y * DrawableTile.BASE_SIZE.Y);
        }

        public long? GetPlayerIDFromCountryID(int countryID)
        {
            foreach (var player in Players)
            {
                if (player.Value.CountryID != countryID)
                    continue;

                return player.Key;
            }

            return null;
        }

        public static Vector2 GetDrawablePositionForTopOfTile(Vector2I tilePos) => new Vector2(tilePos.X * DrawableTile.BASE_SIZE.X, tilePos.Y * DrawableTile.BASE_SIZE.Y - 1);
        public static Vector2 GetDrawablePositionForBottomOfTile(Vector2I tilePos) => new Vector2(tilePos.X * DrawableTile.BASE_SIZE.X, (tilePos.Y + 1) * DrawableTile.BASE_SIZE.Y - 1);

        public void ScheduleUpdateToGameState(TurnData gameState, Action postUpdateAction)
        {
            Schedule(() =>
            {
                updateToGameState(gameState);
                postUpdateAction?.Invoke();
            });
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
        }

        public void ClearFog(bool makeFoggy, bool triggerChange) => fogOfWarGenerator.ClearFog(makeFoggy, triggerChange);
        public void UpdateFogOfWar(int playerId, int rangeIncrease, bool canSeeIntoHiddenTiles, bool resetFog = true) => fogOfWarGenerator.GenerateFogForPlayer(playerId, rangeIncrease, canSeeIntoHiddenTiles, resetFog);

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

        public void DeleteUnit(long unitId, bool explode)
        {
            if (!units.Remove(unitId, out DrawableUnit unit))
                return;

            if (explode)
                playExplosion(unit.UnitData.MovementType, unit.MapPosition);

            unitsDrawable.Remove(unit);

            if (unit.Cargo != null)
            {
                foreach (var cargoId in unit.Cargo)
                {
                    if (!units.Remove(cargoId, out DrawableUnit cargo))
                        continue;

                    unitsDrawable.Remove(cargo);
                }
            }
        }

        private void playExplosion(MovementType type, Vector2I unitPosition)
        {
            switch (type)
            {
                case MovementType.Air:
                    PlayEffect("Effects/Explosion/Explosion-Air", 450, unitPosition);
                    break;

                case MovementType.Sea:
                case MovementType.Lander:
                    PlayEffect("Effects/Explosion/Explosion-Sea", 350, unitPosition);
                    break;

                default:
                    PlayEffect("Effects/Explosion/Explosion-Land", 500, unitPosition);
                    break;
            }
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

        public EffectAnimation PlayEffect(string animation, double duration, Vector2I mapPosition, double startDelay = 0, float rotation = 0) => effectAnimationController.PlayAnimation(animation, duration, mapPosition, startDelay, rotation);

        public EffectAnimation PlaySelectionAnimation(DrawableUnit unit)
        {
            var effect = PlayEffect("Effects/Select", 100, unit.MapPosition);
            effect.DelayUntilTransformsFinished().AddDelayDependingOnDifferenceBetweenEndTimes(effect, unit)
                  .FadeTo(0.5f).ScaleTo(0.5f)
                  .FadeTo(1, 150, Easing.In).ScaleTo(1, 300, Easing.OutBounce).Then().Expire();
            return effect;
        }

        public DrawableUnit GetDrawableUnit(Vector2I unitPosition)
        {
            //Todo: query by position rather than iterate over everything
            foreach (var unit in units)
            {
                if (unit.Value.MapPosition == unitPosition && !unit.Value.BeingCarried.Value)
                    return unit.Value;
            }

            throw new Exception("Unable to find unit at position: " + unitPosition);
        }

        public bool TryGetDrawableUnit(Vector2I unitPosition, out DrawableUnit unit)
        {
            //Todo: query by position rather than iterate over everything
            foreach (var checkUnit in units)
            {
                if (checkUnit.Value.MapPosition == unitPosition && !checkUnit.Value.BeingCarried.Value)
                {
                    unit = checkUnit.Value;
                    return true;
                }
            }

            unit = null;
            return false;
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
                if (!awbwBuilding.TerrainID.HasValue)
                    throw new Exception("Tried to update a missing building. But it didn't have a terrain id.");

                var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.TerrainID.Value);
                var drawableBuilding = new DrawableBuilding(buildingTile, GetPlayerIDFromCountryID(buildingTile.CountryID), tilePosition);
                buildings.Add(tilePosition, drawableBuilding);
                buildingsDrawable.Add(drawableBuilding);
                return;
            }

            var comparisonTerrainId = awbwBuilding.TerrainID ?? 0;

            if (comparisonTerrainId != 0 && building.BuildingTile.AWBWId != comparisonTerrainId)
            {
                buildingsDrawable.Remove(building);
                buildings.Remove(tilePosition);

                if (awbwBuilding.TerrainID.HasValue && awbwBuilding.TerrainID != 0)
                {
                    var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.TerrainID.Value);
                    var drawableBuilding = new DrawableBuilding(buildingTile, GetPlayerIDFromCountryID(buildingTile.CountryID), tilePosition);
                    buildings.Add(tilePosition, drawableBuilding);
                    buildingsDrawable.Add(drawableBuilding);
                }
            }

            //Todo: Is this always the case
            if (!newTurn)
                building.HasDoneAction.Value = false;

            if (TryGetDrawableUnit(awbwBuilding.Position, out var unit))
                unit.IsCapturing.Value = awbwBuilding.Capture != 20;
        }
    }
}
