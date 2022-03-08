using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Unit;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Replay;
using AWBWApp.Game.UI.Replay.Toolbar;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Logic
{
    public class GameMap : Container
    {
        public Vector2I MapSize { get; private set; }

        public Bindable<Weather> CurrentWeather = new Bindable<Weather>();

        private readonly Container<DrawableTile> gameBoardDrawable;
        private DrawableTile[,] gameBoard;

        private readonly Container<DrawableBuilding> buildingsDrawable;
        private Dictionary<Vector2I, DrawableBuilding> buildings;

        private readonly Container<DrawableUnit> unitsDrawable;
        private Dictionary<long, DrawableUnit> units;

        [Resolved]
        private TerrainTileStorage terrainTileStorage { get; set; }

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private UnitStorage unitStorage { get; set; }

        [Resolved]
        private CountryStorage countryStorage { get; set; }

        private CustomShoalGenerator shoalGenerator { get; set; }

        private FogOfWarGenerator fogOfWarGenerator;

        private readonly EffectAnimationController effectAnimationController;

        private Dictionary<long, PlayerInfo> players;

        private readonly MovingGrid grid;

        private bool animatingMapStart;

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
                grid = new MovingGrid
                {
                    Position = new Vector2(-1, DrawableTile.BASE_SIZE.Y - 2),
                    Velocity = Vector2.Zero,
                    Spacing = new Vector2(16),
                    LineSize = new Vector2(2),
                    GridColor = new Color4(15, 15, 15, 255),
                },
                unitsDrawable = new Container<DrawableUnit>
                {
                    AutoSizeAxes = Axes.Both
                },
                effectAnimationController = new EffectAnimationController
                {
                    Origin = Anchor.TopLeft,
                    Anchor = Anchor.TopLeft
                }
            });
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs<IBindable<Weather>>(CurrentWeather);
            return dependencies;
        }

        public void ScheduleSetToLoading() => Schedule(setToLoading);

        [BackgroundDependencyLoader]
        private void load(ReplaySettings settings)
        {
            setToLoading();
            settings.ShowGridOverMap.BindValueChanged(x => grid.FadeTo(x.NewValue ? 1 : 0, 400, Easing.OutQuint), true);
        }

        private void setToLoading()
        {
            //Todo: Fix this hardcoded map
            var loadingMap = new ReplayMap
            {
                Size = new Vector2I(33, 11),
                TerrainName = "Loading",
                Ids = new short[]
                {
                    28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
                    28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
                    28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
                    28, 28, 28, 01, 28, 28, 28, 01, 01, 01, 28, 28, 01, 28, 28, 01, 01, 28, 28, 01, 01, 01, 28, 01, 01, 01, 28, 01, 01, 01, 28, 28, 28,
                    28, 28, 28, 01, 28, 28, 28, 01, 28, 01, 28, 01, 28, 01, 28, 01, 28, 01, 28, 28, 01, 28, 28, 01, 28, 01, 28, 01, 28, 28, 28, 28, 28,
                    28, 28, 28, 01, 28, 28, 28, 01, 28, 01, 28, 01, 01, 01, 28, 01, 28, 01, 28, 28, 01, 28, 28, 01, 28, 01, 28, 01, 28, 01, 28, 28, 28,
                    28, 28, 28, 01, 28, 28, 28, 01, 28, 01, 28, 01, 28, 01, 28, 01, 28, 01, 28, 28, 01, 28, 28, 01, 28, 01, 28, 01, 28, 01, 28, 28, 28,
                    28, 28, 28, 01, 01, 01, 28, 01, 01, 01, 28, 01, 28, 01, 28, 01, 01, 28, 28, 01, 01, 01, 28, 01, 28, 01, 28, 01, 01, 01, 28, 28, 28,
                    28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
                    28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
                    28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
                }
            };

            if (shoalGenerator == null)
                shoalGenerator = new CustomShoalGenerator(terrainTileStorage, buildingStorage);
            loadingMap = shoalGenerator.CreateCustomShoalVersion(loadingMap);

            MapSize = loadingMap.Size;

            gameBoardDrawable.Clear();
            buildingsDrawable.Clear();
            unitsDrawable.Clear();

            gameBoard = new DrawableTile[MapSize.X, MapSize.Y];
            buildings = new Dictionary<Vector2I, DrawableBuilding>();
            units = new Dictionary<long, DrawableUnit>();

            var mapIdx = 0;

            for (int y = 0; y < MapSize.Y; y++)
            {
                for (int x = 0; x < MapSize.X; x++)
                {
                    var terrainId = loadingMap.Ids[mapIdx++];

                    var terrainTile = terrainTileStorage.GetTileByAWBWId(terrainId);
                    var tile = new DrawableTile(terrainTile) { Position = new Vector2(x * DrawableTile.BASE_SIZE.X, y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                    gameBoard[x, y] = tile;
                    gameBoardDrawable.Add(tile);
                }
            }

            AutoSizeAxes = Axes.Both;
            effectAnimationController.Size = new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, MapSize.Y * DrawableTile.BASE_SIZE.Y);
            grid.Size = new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, MapSize.Y * DrawableTile.BASE_SIZE.Y);

            gameBoardDrawable.FadeOut().FadeIn(250);
            animateStart(4);
        }

        public void ScheduleInitialGameState(ReplayData gameState, ReplayMap map, Dictionary<long, PlayerInfo> players)
        {
            this.players = players;
            Schedule(() =>
            {
                setToInitialGameState(gameState, map);
            });
        }

        private void setToInitialGameState(ReplayData gameState, ReplayMap map)
        {
            if (shoalGenerator == null)
                shoalGenerator = new CustomShoalGenerator(terrainTileStorage, buildingStorage);

            map = shoalGenerator.CreateCustomShoalVersion(map);

            MapSize = map.Size;

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

                    TerrainTile terrainTile;
                    if (buildingStorage.ContainsBuildingWithAWBWId(terrainId) && replayBuildings.TryGetValue(new Vector2I(x, y), out _))
                        terrainTile = terrainTileStorage.GetTileByCode("Plain");
                    else
                        terrainTile = terrainTileStorage.GetTileByAWBWId(terrainId);
                    var tile = new DrawableTile(terrainTile) { Position = new Vector2(x * DrawableTile.BASE_SIZE.X, y * DrawableTile.BASE_SIZE.Y + DrawableTile.BASE_SIZE.Y - 1) };
                    gameBoard[x, y] = tile;
                    gameBoardDrawable.Add(tile);
                }
            }

            foreach (var awbwBuilding in replayBuildings)
            {
                if (!awbwBuilding.Value.TerrainID.HasValue)
                    throw new Exception("Invalid building encountered: Missing terrain id.");

                if (!buildingStorage.TryGetBuildingByAWBWId(awbwBuilding.Value.TerrainID.Value, out var building))
                {
                    //This is probably a terrain tile that get building properties. This can happen with pipes.
                    if (terrainTileStorage.TryGetTileByAWBWId(awbwBuilding.Value.TerrainID.Value, out _))
                        continue;

                    throw new Exception("Unknown Building ID: " + awbwBuilding.Value.TerrainID.Value);
                }
                var position = awbwBuilding.Value.Position;

                var drawableBuilding = new DrawableBuilding(building, getPlayerIDFromCountryID(building.CountryID), position);
                buildings.Add(position, drawableBuilding);
                buildingsDrawable.Add(drawableBuilding);
            }

            var replayUnits = gameState.TurnData[0].ReplayUnit;

            if (replayUnits != null)
            {
                foreach (var unit in replayUnits)
                {
                    var unitData = unitStorage.GetUnitByCode(unit.Value.UnitName);
                    var country = countryStorage.GetCountryByAWBWID(gameState.ReplayInfo.Players[unit.Value.PlayerID.Value].CountryId);
                    var drawableUnit = new DrawableUnit(unitData, unit.Value, country);
                    units.Add(unit.Value.ID, drawableUnit);
                    unitsDrawable.Add(drawableUnit);
                }
            }

            fogOfWarGenerator = new FogOfWarGenerator(this);
            fogOfWarGenerator.FogOfWar.BindValueChanged(x => updateFog(x.NewValue));

            AutoSizeAxes = Axes.Both;
            effectAnimationController.Size = new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, MapSize.Y * DrawableTile.BASE_SIZE.Y);
            grid.Size = new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, MapSize.Y * DrawableTile.BASE_SIZE.Y);

            CurrentWeather.Value = gameState.TurnData[0].StartWeather.Type;
            gameBoardDrawable.FadeIn();
            animateStart(1.5f);
        }

        private void animateStart(float speed)
        {
            animatingMapStart = true;

            var inverseSpeed = 1 / speed;

            var offsetPosition = new Vector2(DrawableTile.HALF_BASE_SIZE.X, -3 * DrawableTile.BASE_SIZE.Y);

            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    var tile = gameBoard[x, y];
                    var tilePos = tile.Position;

                    tile.FadeOut().Delay((x + y) * 40 * inverseSpeed).FadeIn().MoveToOffset(offsetPosition).MoveTo(tilePos, 275 * inverseSpeed, Easing.OutCubic);

                    var coord = new Vector2I(x, y);
                    if (buildings.TryGetValue(coord, out var building))
                        building.FadeOut().Delay(((x + y) * 40 + 25) * inverseSpeed).FadeIn().MoveToOffset(offsetPosition).MoveTo(building.Position, 275 * inverseSpeed, Easing.OutCubic);

                    if (TryGetDrawableUnit(coord, out var unit))
                    {
                        unit.UnitAnimatingIn = true;
                        unit.FadeOut().Delay(((x + y) * 40 + 50) * inverseSpeed).FadeInFromZero();
                        unit.Delay(((x + y) * 40 + 50) * inverseSpeed).MoveToOffset(offsetPosition).MoveTo(unit.Position, 275 * inverseSpeed, Easing.OutCubic).OnComplete(x => x.UnitAnimatingIn = false);
                    }
                }
            }
        }

        private void updateFog(bool[,] fogOfWar)
        {
            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    var foggy = !fogOfWar[x, y];
                    gameBoard[x, y].FogOfWarActive.Value = foggy;

                    var coord = new Vector2I(x, y);
                    if (buildings.TryGetValue(coord, out var building))
                        building.FogOfWarActive.Value = foggy;

                    if (TryGetDrawableUnit(coord, out var unit))
                        unit.FogOfWarActive.Value = foggy;
                }
            }
        }

        public static Vector2 GetDrawablePositionForTopOfTile(Vector2I tilePos) => new Vector2(tilePos.X * DrawableTile.BASE_SIZE.X, tilePos.Y * DrawableTile.BASE_SIZE.Y - 1);
        public static Vector2 GetDrawablePositionForBottomOfTile(Vector2I tilePos) => new Vector2(tilePos.X * DrawableTile.BASE_SIZE.X, (tilePos.Y + 1) * DrawableTile.BASE_SIZE.Y - 1);

        public void ScheduleUpdateToGameState(TurnData gameState, Action postUpdateAction)
        {
            if (animatingMapStart)
            {
                FinishTransforms(true);
                animatingMapStart = false;
            }

            Schedule(() =>
            {
                updateToGameState(gameState);
                postUpdateAction?.Invoke();
            });
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
                    var drawableUnit = new DrawableUnit(unitData, unit.Value, players[unit.Value.PlayerID.Value].Country.Value);
                    units.Add(unit.Value.ID, drawableUnit);
                    unitsDrawable.Add(drawableUnit);
                }
            }

            foreach (var unit in units.Where(x => !gameState.ReplayUnit.ContainsKey(x.Value.UnitID)))
            {
                units.Remove(unit.Key);
                unitsDrawable.Remove(unit.Value);
            }

            CurrentWeather.Value = gameState.StartWeather.Type;
        }

        public void ClearFog(bool makeFoggy, bool triggerChange) => fogOfWarGenerator.ClearFog(makeFoggy, triggerChange);
        public void UpdateFogOfWar(long playerId, int rangeIncrease, bool canSeeIntoHiddenTiles, bool resetFog = true) => fogOfWarGenerator.GenerateFogForPlayer(playerId, rangeIncrease, canSeeIntoHiddenTiles, resetFog);

        public DrawableUnit AddUnit(ReplayUnit unit)
        {
            var unitData = unitStorage.GetUnitByCode(unit.UnitName);
            var drawableUnit = new DrawableUnit(unitData, unit, players[unit.PlayerID.Value].Country.Value);
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

        public EffectAnimation PlayEffect(string animation, double duration, Vector2I mapPosition, double startDelay = 0, Action<EffectAnimation> onLoaded = null) => effectAnimationController.PlayAnimation(animation, duration, mapPosition, startDelay, onLoaded);

        public EffectAnimation PlaySelectionAnimation(DrawableUnit unit)
        {
            var effect = PlayEffect("Effects/Select", 100, unit.MapPosition, 0,
                x => x.FadeTo(0.5f).ScaleTo(0.5f)
                      .FadeTo(1, 150, Easing.In).ScaleTo(1, 300, Easing.OutBounce).Then().Expire());
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

        public IEnumerable<DrawableUnit> GetDrawableUnitsFromPlayer(long playerId)
        {
            return units.Values.Where(x => x.OwnerID.HasValue && x.OwnerID == playerId);
        }

        public IEnumerable<DrawableBuilding> GetDrawableBuildingsForPlayer(long playerId)
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

                if (buildingStorage.TryGetBuildingByAWBWId(awbwBuilding.TerrainID.Value, out var buildingTile))
                {
                    var drawableBuilding = new DrawableBuilding(buildingTile, getPlayerIDFromCountryID(buildingTile.CountryID), tilePosition);
                    buildings.Add(tilePosition, drawableBuilding);
                    buildingsDrawable.Add(drawableBuilding);
                    return;
                }

                if (terrainTileStorage.TryGetTileByAWBWId(awbwBuilding.TerrainID.Value, out _))
                    return;

                throw new Exception("Unknown Building ID: " + awbwBuilding.TerrainID.Value);
            }

            var comparisonTerrainId = awbwBuilding.TerrainID ?? 0;

            if (comparisonTerrainId != 0 && building.BuildingTile.AWBWID != comparisonTerrainId)
            {
                buildingsDrawable.Remove(building);
                buildings.Remove(tilePosition);

                if (awbwBuilding.TerrainID.HasValue && awbwBuilding.TerrainID != 0)
                {
                    var buildingTile = buildingStorage.GetBuildingByAWBWId(awbwBuilding.TerrainID.Value);
                    var drawableBuilding = new DrawableBuilding(buildingTile, getPlayerIDFromCountryID(buildingTile.CountryID), tilePosition);
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

        private long? getPlayerIDFromCountryID(int countryID) => players.FirstOrDefault(x => x.Value.Country.Value.AWBWID == countryID).Key;

        public UnitData GetUnitDataForUnitName(string unitName) => unitStorage.GetUnitByCode(unitName);
    }
}
