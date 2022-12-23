using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.API.Replay.Actions;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.Input;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Replay;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.Game.Logic
{
    public partial class GameMap : Container, IKeyBindingHandler<AWBWGlobalAction>
    {
        public Vector2I MapSize { get; protected set; }

        [Resolved]
        protected TerrainTileStorage TerrainTileStorage { get; private set; }

        [Resolved]
        protected BuildingStorage BuildingStorage { get; private set; }

        [Resolved]
        protected UnitStorage UnitStorage { get; private set; }

        [Resolved]
        protected CountryStorage CountryStorage { get; private set; }

        [Resolved]
        protected COStorage COStorage { get; private set; }

        protected CustomShoalGenerator ShoalGenerator { get; set; }

        public Bindable<WeatherType> CurrentWeather = new Bindable<WeatherType>();

        protected readonly TileGridContainer<DrawableTile> TileGrid;
        public readonly TileGridContainer<DrawableBuilding> BuildingGrid;
        protected bool HasLoadedMap;

        protected readonly Container<DrawableUnit> UnitsDrawable;
        protected Dictionary<long, DrawableUnit> Units;

        private readonly UnitRangeIndicator rangeIndicator;
        protected TileCursor TileCursor;

        private FogOfWarGenerator fogOfWarGenerator;

        private readonly EffectAnimationController effectAnimationController;

        protected ReplayController ReplayController;

        private readonly MovingGrid grid;

        private bool animatingMapStart;

        private Bindable<bool> revealUnknownInformation;

        public IBindable<bool> RevealUnknownInformation => revealUnknownInformation;

        private Bindable<bool> showGridlines;
        private Bindable<bool> showTileCursor;

        protected DetailedInformationPopup InfoPopup;

        private const int unit_deselect_delay = 500;
        private ScheduledDelegate unitDeselectDelegate;
        private bool hasShownMapOutdatedWarning;

        [Resolved]
        private AWBWAppUserInputManager inputManager { get; set; }

        public GameMap(ReplayController controller)
        {
            ReplayController = controller;

            AddRange(new Drawable[]
            {
                TileGrid = new TileGridContainer<DrawableTile>(DrawableTile.BASE_SIZE)
                {
                    Position = new Vector2(0, DrawableTile.BASE_SIZE.Y)
                },
                BuildingGrid = new TileGridContainer<DrawableBuilding>(DrawableTile.BASE_SIZE)
                {
                    Position = new Vector2(0, DrawableTile.BASE_SIZE.Y)
                },
                grid = new MovingGrid
                {
                    Position = new Vector2(-1, DrawableTile.BASE_SIZE.Y - 2),
                    Velocity = Vector2.Zero,
                    Spacing = new Vector2(16),
                    LineSize = new Vector2(2),
                    GridColor = new Color4(15, 15, 15, 255),
                },
                UnitsDrawable = new Container<DrawableUnit>(),
                TileCursor = CreateTileCursor(),
                rangeIndicator = new UnitRangeIndicator(),
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
            dependencies.CacheAs<IBindable<WeatherType>>(CurrentWeather);
            return dependencies;
        }

        public void ScheduleSetToLoading() => Schedule(setToLoading);

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager settings)
        {
            revealUnknownInformation = settings.GetBindable<bool>(AWBWSetting.ReplayOnlyShownKnownInfo);
            revealUnknownInformation.BindValueChanged(x => UpdateDiscoveredBuildings());
            showGridlines = settings.GetBindable<bool>(AWBWSetting.ReplayShowGridOverMap);
            showGridlines.BindValueChanged(x => grid.FadeTo(x.NewValue ? 1 : 0, 400, Easing.OutQuint), true);
            showTileCursor = settings.GetBindable<bool>(AWBWSetting.ShowTileCursor);
            ShoalGenerator = new CustomShoalGenerator(TerrainTileStorage, BuildingStorage);
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

            loadingMap = ShoalGenerator.CreateCustomShoalVersion(loadingMap);

            MapSize = loadingMap.Size;

            TileGrid.ClearToSize(MapSize);
            BuildingGrid.ClearToSize(MapSize);
            UnitsDrawable.Clear();

            Units = new Dictionary<long, DrawableUnit>();

            var mapIdx = 0;

            for (int y = 0; y < MapSize.Y; y++)
            {
                for (int x = 0; x < MapSize.X; x++)
                {
                    var terrainId = loadingMap.Ids[mapIdx++];

                    var terrainTile = TerrainTileStorage.GetTileByAWBWId(terrainId);
                    var tile = new DrawableTile(terrainTile);
                    TileGrid.AddTile(tile, new Vector2I(x, y));
                }
            }

            AutoSizeAxes = Axes.Both;
            SetDrawableSize(new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, (MapSize.Y + 1) * DrawableTile.BASE_SIZE.Y));

            TileGrid.FadeOut().FadeIn(250);
            animateStart(4);
        }

        protected void SetDrawableSize(Vector2 size)
        {
            grid.Size = size;
            TileGrid.Size = size;
            BuildingGrid.Size = size;
            UnitsDrawable.Size = size;
            effectAnimationController.Size = size;
        }

        public void SetInfoPopup(DetailedInformationPopup popup)
        {
            InfoPopup = popup;
        }

        public void SetToInitialGameState(ReplayData gameState, ReplayMap map)
        {
            Assert.IsTrue(ThreadSafety.IsUpdateThread, "SetToInitialGameState was called off update thread.");
            HasLoadedMap = false;

            map = ShoalGenerator.CreateCustomShoalVersion(map);

            MapSize = map.Size;

            //Calculate the map size as this isn't given by the api
            //Todo: Check buildings
            if (AutoSizeAxes == Axes.None)
                Size = Vec2IHelper.ScalarMultiply(MapSize + new Vector2I(0, 1), DrawableTile.BASE_SIZE);

            TileGrid.ClearToSize(MapSize);
            BuildingGrid.ClearToSize(MapSize);
            UnitsDrawable.Clear();

            Units = new Dictionary<long, DrawableUnit>();

            var replayBuildings = gameState.TurnData[0].Buildings;

            for (int y = 0; y < MapSize.Y; y++)
            {
                for (int x = 0; x < MapSize.X; x++)
                {
                    var terrainId = map.Ids[y * MapSize.X + x];

                    TerrainTile terrainTile;

                    if (BuildingStorage.ContainsBuildingWithAWBWId(terrainId))
                    {
                        terrainTile = TerrainTileStorage.GetTileByCode("Plain");

                        if (!replayBuildings.TryGetValue(new Vector2I(x, y), out _))
                        {
                            if (!hasShownMapOutdatedWarning)
                            {
                                ReplayController.ShowError(new Exception("Buildings do not match replay due to a map update. This can cause further errors, proceed with caution."), false);
                                hasShownMapOutdatedWarning = true;
                            }
                        }
                    }
                    else
                        terrainTile = TerrainTileStorage.GetTileByAWBWId(terrainId);
                    var tile = new DrawableTile(terrainTile);
                    TileGrid.AddTile(tile, new Vector2I(x, y));
                }
            }

            foreach (var awbwBuilding in replayBuildings)
            {
                if (!awbwBuilding.Value.TerrainID.HasValue)
                    throw new Exception("Invalid building encountered: Missing terrain id.");

                if (!BuildingStorage.TryGetBuildingByAWBWId(awbwBuilding.Value.TerrainID.Value, out var building))
                {
                    //This is probably a terrain tile that get building properties. This can happen with pipes.
                    if (TerrainTileStorage.TryGetTileByAWBWId(awbwBuilding.Value.TerrainID.Value, out _))
                        continue;

                    throw new Exception("Unknown Building ID: " + awbwBuilding.Value.TerrainID.Value);
                }
                var position = awbwBuilding.Value.Position;

                var playerID = getPlayerIDFromCountryID(building.CountryID);
                var country = playerID.HasValue ? ReplayController.Players[playerID.Value].Country : null;
                var drawableBuilding = new DrawableBuilding(building, position, playerID, country);

                foreach (var player in gameState.ReplayInfo.Players)
                {
                    if (drawableBuilding.TeamToTile.ContainsKey(player.Value.TeamName))
                        continue;

                    drawableBuilding.TeamToTile[player.Value.TeamName] = drawableBuilding.BuildingTile;
                }

                //For testing purposes. Likely not used in any game.
                if (awbwBuilding.Value.Capture.HasValue && awbwBuilding.Value.Capture != 0)
                    drawableBuilding.CaptureHealth.Value = awbwBuilding.Value.Capture.Value;

                BuildingGrid.AddTile(drawableBuilding, position);
            }

            var replayUnits = gameState.TurnData[0].ReplayUnit;

            if (replayUnits != null)
            {
                foreach (var unit in replayUnits)
                {
                    var drawableUnit = AddUnit(unit.Value, false);

                    //For testing purposes. Likely not used in any game.
                    if (!drawableUnit.BeingCarried.Value && TryGetDrawableBuilding(drawableUnit.MapPosition, out var awbwBuilding))
                        drawableUnit.IsCapturing.Value = awbwBuilding.CaptureHealth.Value != 20 && awbwBuilding.CaptureHealth.Value != 0;
                }
            }

            fogOfWarGenerator = new FogOfWarGenerator(this);
            fogOfWarGenerator.FogOfWar.BindValueChanged(x => updateFog(x.NewValue));

            AutoSizeAxes = Axes.Both;
            SetDrawableSize(new Vector2(MapSize.X * DrawableTile.BASE_SIZE.X, (MapSize.Y + 1) * DrawableTile.BASE_SIZE.Y));

            CurrentWeather.Value = gameState.TurnData[0].StartWeather.Type;
            TileGrid.FadeIn();
            HasLoadedMap = true;
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
                    var tile = TileGrid[x, y];
                    var tilePos = tile.Position;

                    tile.FadeOut().Delay((x + y) * 40 * inverseSpeed).FadeIn().MoveToOffset(offsetPosition).MoveTo(tilePos, 275 * inverseSpeed, Easing.OutCubic);

                    var coord = new Vector2I(x, y);

                    if (BuildingGrid.TryGet(coord, out var building))
                    {
                        building.FadeOut().Delay(((x + y) * 40 + 25) * inverseSpeed).FadeIn().MoveToOffset(offsetPosition)
                                .MoveTo(building.Position, 275 * inverseSpeed, Easing.OutCubic);
                    }

                    if (TryGetDrawableUnit(coord, out var unit))
                    {
                        unit.UnitAnimatingIn = true;
                        unit.FadeOut().Delay(((x + y) * 40 + 50) * inverseSpeed).FadeInFromZero().MoveToOffset(offsetPosition)
                            .MoveTo(unit.Position, 275 * inverseSpeed, Easing.OutCubic).OnComplete(x => x.UnitAnimatingIn = false);
                    }
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!HasLoadedMap)
                return;

            var cursor = inputManager.CurrentState.Mouse.Position;
            UpdateTileCursor(cursor);
        }

        protected virtual void UpdateTileCursor(Vector2 mousePosition)
        {
            if (GetUnitAndTileFromMousePosition(ToLocalSpace(mousePosition), out var tilePosition, out var tile, out var building, out var unit) && IsHovered)
            {
                InfoPopup.ShowDetails(tile, building, unit);

                TileCursor.TilePosition = tilePosition;
                if (showTileCursor.Value)
                    TileCursor.Show();
                else
                    TileCursor.Hide();
            }
            else
            {
                InfoPopup.ShowDetails(null, null, null);
                TileCursor.Hide();
            }

            if (unit != selectedUnit)
            {
                if (unitDeselectDelegate == null)
                    unitDeselectDelegate = Scheduler.AddDelayed(() => SetUnitAsSelected(null), unit_deselect_delay);
            }
            else if (unitDeselectDelegate != null)
            {
                unitDeselectDelegate.Cancel();
                unitDeselectDelegate = null;
            }
        }

        private void updateFog(FogOfWarState[,] fogOfWar)
        {
            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    var fogState = fogOfWar[x, y];
                    TileGrid[x, y].FogOfWarActive.Value = fogState != FogOfWarState.AllVisible;

                    var coord = new Vector2I(x, y);
                    if (BuildingGrid.TryGet(coord, out var building))
                        building.FogOfWarActive.Value = fogState != FogOfWarState.AllVisible;

                    //Replays without pipe attack actions can occasionally lead to having 2 units on the same tile.
                    foreach (var unit in GetAllDrawableUnitsOnTile(coord))
                        unit.FogOfWarActive.Value = unit.UnitData.MovementType == MovementType.Air ? fogState == FogOfWarState.Hidden : fogState != FogOfWarState.AllVisible;
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

        private void updateToGameState(TurnData gameState)
        {
            //Do units first, then buildings as buildings need to set capture status on units.

            foreach (var unit in gameState.ReplayUnit)
            {
                if (Units.TryGetValue(unit.Value.ID, out DrawableUnit existingUnit))
                {
                    existingUnit.UpdateUnit(unit.Value);
                }
                else
                {
                    var unitData = UnitStorage.GetUnitByCode(unit.Value.UnitName);

                    var player = ReplayController.Players[unit.Value.PlayerID!.Value];
                    var drawableUnit = new DrawableUnit(unitData, unit.Value, player.Country, player.UnitFaceDirection);
                    drawableUnit.FogOfWarActive.Value = IsTileFoggy(drawableUnit.MapPosition, drawableUnit.UnitData.MovementType == MovementType.Air);
                    Units.Add(unit.Value.ID, drawableUnit);
                    UnitsDrawable.Add(drawableUnit);
                }
            }

            foreach (var unit in Units.Where(x => !gameState.ReplayUnit.ContainsKey(x.Value.UnitID)))
            {
                Units.Remove(unit.Key);
                UnitsDrawable.Remove(unit.Value, true);
            }

            for (int x = 0; x < MapSize.X; x++)
            {
                for (int y = 0; y < MapSize.Y; y++)
                {
                    var position = new Vector2I(x, y);

                    if (gameState.Buildings.TryGetValue(position, out var building))
                    {
                        UpdateBuilding(building, true);
                    }
                    else
                        BuildingGrid.RemoveTile(new Vector2I(x, y));
                }
            }

            CurrentWeather.Value = gameState.StartWeather.Type;
        }

        public void ClearFog(bool makeFoggy, bool triggerChange) => fogOfWarGenerator?.ClearFog(makeFoggy, triggerChange);
        public void UpdateFogOfWar(long playerId, int rangeIncrease, bool canSeeIntoHiddenTiles, bool resetFog = true) => fogOfWarGenerator.GenerateFogForPlayer(playerId, rangeIncrease, canSeeIntoHiddenTiles, resetFog);

        public bool IsTileFoggy(Vector2I position, bool forAirUnit)
        {
            if (fogOfWarGenerator.FogOfWar.Value == null)
                return false;

            var fogState = fogOfWarGenerator.FogOfWar.Value[position.X, position.Y];

            return forAirUnit ? (fogState == FogOfWarState.Hidden) : (fogState != FogOfWarState.AllVisible);
        }

        public DrawableUnit AddUnit(ReplayUnit unit, bool schedule = true)
        {
            var unitData = UnitStorage.GetUnitByCode(unit.UnitName);
            var player = ReplayController.Players[unit.PlayerID!.Value];
            var drawableUnit = new DrawableUnit(unitData, unit, player.Country, player.UnitFaceDirection);
            Units.Add(unit.ID, drawableUnit);

            if (schedule)
                Schedule(() => UnitsDrawable.Add(drawableUnit));
            else
                UnitsDrawable.Add(drawableUnit);

            ReplayController.Players[unit.PlayerID!.Value].UnitCount.Value++;
            return drawableUnit;
        }

        public bool TryGetDrawableUnit(long unitId, out DrawableUnit drawableUnit) => Units.TryGetValue(unitId, out drawableUnit);

        public DrawableUnit GetDrawableUnit(long unitId) => Units[unitId];

        public bool TryGetDrawableBuilding(Vector2I position, out DrawableBuilding drawableBuilding) => BuildingGrid.TryGet(position, out drawableBuilding);

        public DrawableUnit DeleteUnit(long unitId, bool explode)
        {
            if (!Units.Remove(unitId, out DrawableUnit unit))
                return null;

            if (explode && !ReplayController.ShouldPlayerActionBeHidden(unit.MapPosition, unit.UnitData.MovementType == MovementType.Air))
                playExplosion(unit.UnitData.MovementType, unit.MapPosition);

            UnitsDrawable.Remove(unit, true);
            if (unit.OwnerID.HasValue)
                ReplayController.Players[unit.OwnerID.Value].UnitCount.Value--;

            if (unit.Cargo != null)
            {
                foreach (var cargoId in unit.Cargo)
                {
                    if (cargoId == -1)
                        continue;

                    DeleteUnit(cargoId, false);
                }
            }

            return unit;
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

            foreach (var unit in Units)
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

        public EffectAnimation PlaySelectionAnimation(DrawableBuilding building)
        {
            var effect = PlayEffect("Effects/Select", 100, building.MapPosition, 0,
                x => x.FadeTo(0.5f).ScaleTo(0.5f)
                      .FadeTo(1, 150, Easing.In).ScaleTo(1, 300, Easing.OutBounce).Then().Expire());
            return effect;
        }

        public void ClearAllEffects()
        {
            effectAnimationController.Clear();
        }

        public DrawableUnit GetDrawableUnit(Vector2I unitPosition)
        {
            //Todo: query by position rather than iterate over everything
            foreach (var unit in Units)
            {
                if (unit.Value.MapPosition == unitPosition && !unit.Value.BeingCarried.Value)
                    return unit.Value;
            }

            throw new Exception("Unable to find unit at position: " + unitPosition);
        }

        public bool TryGetDrawableUnit(Vector2I unitPosition, out DrawableUnit unit)
        {
            //Todo: query by position rather than iterate over everything
            foreach (var checkUnit in Units)
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

        public IEnumerable<DrawableUnit> GetAllDrawableUnitsOnTile(Vector2I unitPosition)
        {
            foreach (var checkUnit in Units)
            {
                if (checkUnit.Value.MapPosition == unitPosition && !checkUnit.Value.BeingCarried.Value)
                    yield return checkUnit.Value;
            }
        }

        public IEnumerable<DrawableUnit> GetDrawableUnitsFromPlayer(long playerId)
        {
            return Units.Values.Where(x => x.OwnerID.HasValue && x.OwnerID == playerId);
        }

        public IEnumerable<DrawableBuilding> GetDrawableBuildingsForPlayer(long playerId)
        {
            foreach (var building in BuildingGrid)
            {
                if (building.OwnerID.HasValue && building.OwnerID == playerId)
                    yield return building;
            }
        }

        public DrawableTile GetDrawableTile(Vector2I position) => TileGrid[position.X, position.Y];

        public void UpdateBuilding(ReplayBuilding awbwBuilding, bool setBuildingToReady)
        {
            var tilePosition = awbwBuilding.Position;

            if (!BuildingGrid.TryGet(tilePosition, out DrawableBuilding building))
            {
                if (!awbwBuilding.TerrainID.HasValue)
                    throw new Exception("Tried to update a missing building. But it didn't have a terrain id.");

                if (BuildingStorage.TryGetBuildingByAWBWId(awbwBuilding.TerrainID.Value, out var buildingTile))
                {
                    var playerID = getPlayerIDFromCountryID(buildingTile.CountryID);
                    var country = playerID.HasValue ? ReplayController.Players[playerID.Value].Country : null;
                    var drawableBuilding = new DrawableBuilding(buildingTile, tilePosition, playerID, country);
                    drawableBuilding.FogOfWarActive.Value = IsTileFoggy(awbwBuilding.Position, false);
                    BuildingGrid.AddTile(drawableBuilding, tilePosition);
                    return;
                }

                if (TerrainTileStorage.TryGetTileByAWBWId(awbwBuilding.TerrainID.Value, out _))
                    return;

                throw new Exception("Unknown Building ID: " + awbwBuilding.TerrainID.Value);
            }

            var comparisonTerrainId = awbwBuilding.TerrainID ?? 0;

            if (comparisonTerrainId != 0 && building.BuildingTile.AWBWID != comparisonTerrainId)
            {
                BuildingGrid.RemoveTile(tilePosition);

                if (awbwBuilding.TerrainID.HasValue && awbwBuilding.TerrainID != 0)
                {
                    if (BuildingStorage.TryGetBuildingByAWBWId(awbwBuilding.TerrainID.Value, out var buildingTile))
                    {
                        var playerID = getPlayerIDFromCountryID(buildingTile.CountryID);
                        var country = playerID.HasValue ? ReplayController.Players[playerID.Value].Country : null;
                        var newBuilding = new DrawableBuilding(buildingTile, tilePosition, playerID, country);
                        transferDiscovery(building, newBuilding);
                        newBuilding.FogOfWarActive.Value = IsTileFoggy(awbwBuilding.Position, false);
                        BuildingGrid.AddTile(newBuilding, tilePosition);
                        building = newBuilding;
                    }
                    else if (TerrainTileStorage.TryGetTileByAWBWId(awbwBuilding.TerrainID.Value, out var terrainTile))
                    {
                        //Likely a blown up pipe. May need to change the tile underneath

                        var tile = TileGrid[tilePosition.X, tilePosition.Y];

                        if (tile.TerrainTile != terrainTile)
                        {
                            var newTile = new DrawableTile(terrainTile);

                            TileGrid.AddTile(newTile, tilePosition);
                            newTile.FogOfWarActive.Value = IsTileFoggy(tilePosition, false);
                        }
                    }
                }
            }

            //Todo: Is this always the case
            if (setBuildingToReady)
                building.HasDoneAction.Value = false;
            building.CaptureHealth.Value = awbwBuilding.Capture ?? 20;

            if (TryGetDrawableUnit(awbwBuilding.Position, out var unit))
                unit.IsCapturing.Value = awbwBuilding.Capture != awbwBuilding.LastCapture && awbwBuilding.Capture != 20 && awbwBuilding.Capture != 0;
        }

        public void RegisterDiscovery(DiscoveryCollection collection)
        {
            var team = GetCurrentTeamVisibility();

            foreach (var id in collection.DiscoveryByID)
            {
                foreach (var discovered in id.Value.DiscoveredBuildings)
                {
                    if (!TryGetDrawableBuilding(discovered.Key, out var discBuilding))
                        continue;

                    if (BuildingStorage.TryGetBuildingByAWBWId(discovered.Value.TerrainID!.Value, out var building))
                    {
                        discBuilding.TeamToTile[id.Key] = building;
                        discBuilding.UpdateFogOfWarBuilding(revealUnknownInformation.Value, team);
                    }
                    else
                    {
                        if (discovered.Value.TerrainID!.Value != 115 && discovered.Value.TerrainID!.Value != 116)
                            throw new Exception("A building was turned into a terrain tile, and it was not a pipe?");

                        //If a building is changed from a Pipe to a Pipe Seam, this will trigger.
                        //These actions are seen by everyone, even through fog.
                        //Todo: Does this cause other issues?

                        UpdateBuilding(discovered.Value, false);
                    }
                }
            }
        }

        public void UndoDiscovery(DiscoveryCollection collection)
        {
            var team = GetCurrentTeamVisibility();

            foreach (var building in collection.OriginalDiscovery)
            {
                if (!TryGetDrawableBuilding(building.Key, out var discBuilding))
                    continue;

                discBuilding.TeamToTile.SetTo(building.Value);
                discBuilding.UpdateFogOfWarBuilding(revealUnknownInformation.Value, team);
            }
        }

        public void UpdateDiscoveredBuildings()
        {
            var team = GetCurrentTeamVisibility();
            foreach (var building in BuildingGrid)
                building.UpdateFogOfWarBuilding(revealUnknownInformation.Value, team);
        }

        private void transferDiscovery(DrawableBuilding originalBuilding, DrawableBuilding newBuilding)
        {
            newBuilding.TeamToTile.SetTo(originalBuilding.TeamToTile);

            if (originalBuilding.OwnerID.HasValue)
                newBuilding.TeamToTile[ReplayController.Players[originalBuilding.OwnerID.Value].Team] = newBuilding.BuildingTile;
            if (newBuilding.OwnerID.HasValue)
                newBuilding.TeamToTile[ReplayController.Players[newBuilding.OwnerID.Value].Team] = newBuilding.BuildingTile;
            newBuilding.UpdateFogOfWarBuilding(revealUnknownInformation.Value, GetCurrentTeamVisibility());
        }

        protected virtual string GetCurrentTeamVisibility()
        {
            if (ReplayController.CurrentFogView.Value is long id)
                return ReplayController.Players[id].Team;

            var team = ReplayController.CurrentFogView.Value as string;
            return team.IsNullOrEmpty() ? ReplayController.ActivePlayer.Team : team;
        }

        public virtual bool OnPressed(KeyBindingPressEvent<AWBWGlobalAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case AWBWGlobalAction.ShowUnitsAndBuildingsInFog:
                    revealUnknownInformation.Value = !revealUnknownInformation.Value;
                    return true;

                case AWBWGlobalAction.ShowGridLines:
                    showGridlines.Value = !showGridlines.Value;
                    return true;

                case AWBWGlobalAction.ShowTileCursor:
                    showTileCursor.Value = !showTileCursor.Value;
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<AWBWGlobalAction> e)
        {
        }

        private DrawableUnit selectedUnit;
        private int drawMode;

        public bool SetUnitAsSelected(DrawableUnit unit)
        {
            if (unit == null)
            {
                drawMode = 0;
                selectedUnit = null;
                rangeIndicator.FadeOut(500, Easing.OutQuint);
                return false;
            }

            if (unit.BeingCarried.Value)
                return false;
            if (!revealUnknownInformation.Value && unit.FogOfWarActive.Value)
                return false;

            if (unit != selectedUnit)
            {
                drawMode = 0;
                selectedUnit = unit;
            }
            else if (drawMode >= 2)
            {
                drawMode = 0;
                selectedUnit = null;
                rangeIndicator.FadeOut(500, Easing.OutCubic);
                return false;
            }
            else
                drawMode++;

            if (drawMode == 1 && unit.UnitData.AttackRange == Vector2I.Zero)
                drawMode++;

            var tileList = new List<Vector2I>();

            Color4 colour;
            Color4 outlineColour;

            switch (drawMode)
            {
                case 0:
                {
                    GetMovementTiles(unit, tileList);
                    colour = new Color4(50, 200, 50, 100);
                    outlineColour = new Color4(100, 150, 100, 255);
                    break;
                }

                case 1:
                {
                    var range = unit.AttackRange.Value;

                    var action = ReplayController.GetActivePowerForPlayer(unit.OwnerID!.Value);
                    range.Y += action?.COPower.PowerIncreases?.FirstOrDefault(x => x.AffectedUnits.Contains("all") || x.AffectedUnits.Contains(unit.UnitData.Name))?.RangeIncrease ?? 0;

                    var dayToDay = ReplayController.Players[unit.OwnerID!.Value].ActiveCO.Value.CO.DayToDayPower;
                    range.Y += dayToDay.PowerIncreases?.FirstOrDefault(x => x.AffectedUnits.Contains("all") || x.AffectedUnits.Contains(unit.UnitData.Name))?.RangeIncrease ?? 0;

                    if (unit.UnitData.AttackRange != Vector2I.One)
                    {
                        for (int i = range.X; i <= range.Y; i++)
                        {
                            foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(unit.MapPosition, i))
                            {
                                if (tile.X < 0 || tile.Y < 0 || tile.X >= MapSize.X || tile.Y >= MapSize.Y)
                                    continue;

                                tileList.Add(tile);
                            }
                        }
                    }
                    else
                        getPossibleAttackRange(unit, tileList, range);

                    colour = new Color4(200, 90, 90, 70);
                    outlineColour = new Color4(160, 82, 51, 255);
                    break;
                }

                case 2:
                {
                    var dayToDayPower = ReplayController.Players[unit.OwnerID!.Value].ActiveCO.Value.CO.DayToDayPower;
                    var action = ReplayController.GetActivePowerForPlayer(unit.OwnerID!.Value);
                    var sightRangeModifier = dayToDayPower.SightIncrease + (action?.SightRangeIncrease ?? 0);
                    sightRangeModifier += unit.UnitData.MovementType != MovementType.Air ? TileGrid[unit.MapPosition.X, unit.MapPosition.Y].TerrainTile.SightDistanceIncrease : 0;

                    if (CurrentWeather.Value == WeatherType.Rain)
                        sightRangeModifier -= 1;

                    var vision = Math.Max(1, unit.UnitData.Vision + sightRangeModifier);

                    for (int i = 0; i <= vision; i++)
                    {
                        foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(unit.MapPosition, i))
                        {
                            if (tile.X < 0 || tile.Y < 0 || tile.X >= MapSize.X || tile.Y >= MapSize.Y)
                                continue;

                            var distance = TileGrid[tile.X, tile.Y].TerrainTile.LimitFogOfWarSightDistance;
                            if (distance > 0 && distance < i)
                                continue;

                            tileList.Add(tile);
                        }
                    }

                    colour = new Color4(50, 50, 200, 100);
                    outlineColour = new Color4(100, 100, 150, 255);
                    break;
                }

                default:
                    throw new ArgumentException("Out of range", nameof(drawMode));
            }

            rangeIndicator.ShowNewRange(tileList, unit.MapPosition, colour, outlineColour);

            return true;
        }

        private void getPossibleAttackRange(DrawableUnit unit, List<Vector2I> tileList, Vector2I range)
        {
            var movementList = new List<Vector2I>();

            GetMovementTiles(unit, movementList);

            var tileSet = new HashSet<Vector2I>();

            foreach (var moveTile in movementList)
            {
                for (int i = range.X; i <= range.Y; i++)
                {
                    foreach (var tile in Vec2IHelper.GetAllTilesWithDistance(moveTile, i))
                    {
                        if (tile.X < 0 || tile.Y < 0 || tile.X >= MapSize.X || tile.Y >= MapSize.Y)
                            continue;

                        tileSet.Add(tile);
                    }
                }
            }

            tileList.AddRange(tileSet);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!HasLoadedMap)
                return base.OnClick(e);

            if (GetUnitAndTileFromMousePosition(e.MousePosition, out _, out _, out _, out var unit) && unit != null)
                return SetUnitAsSelected(unit);

            return base.OnClick(e);
        }

        protected bool GetUnitAndTileFromMousePosition(Vector2 cursor, out Vector2I tilePosition, out DrawableTile tile, out DrawableBuilding building, out DrawableUnit unit)
        {
            tile = null;
            building = null;
            unit = null;
            tilePosition = Vector2I.Zero;

            if (cursor.X < 0 || cursor.X >= DrawSize.X)
                return false;
            if (cursor.Y < DrawableTile.BASE_SIZE.Y || cursor.Y >= DrawSize.Y)
                return false;

            cursor.Y -= DrawableTile.BASE_SIZE.Y;

            //Doubly make sure that we aren't trying to get a tile outside of what we have.
            tilePosition = new Vector2I((int)(cursor.X / DrawableTile.BASE_SIZE.X), (int)(cursor.Y / DrawableTile.BASE_SIZE.Y));
            if (tilePosition.X < 0 || tilePosition.X >= MapSize.X || tilePosition.Y < 0 || tilePosition.Y >= MapSize.Y)
                return false;

            TryGetDrawableUnit(tilePosition, out unit);
            BuildingGrid.TryGet(tilePosition, out building);
            tile = TileGrid[tilePosition.X, tilePosition.Y];
            Debug.Assert(tile != null);

            return true;
        }

        public void GetMovementTiles(DrawableUnit unit, List<Vector2I> positions)
        {
            var visited = new HashSet<Vector2I>();
            var queue = new PriorityQueue<Vector2I, int>();

            queue.Enqueue(unit.MapPosition, 0);

            var movementRange = unit.MovementRange.Value;

            var action = ReplayController?.GetActivePowerForPlayer(unit.OwnerID!.Value);
            var dayToDay = ReplayController?.Players[unit.OwnerID!.Value].ActiveCO.Value.CO.DayToDayPower;
            // If there's no replayController, assume Andy
            dayToDay = dayToDay ?? COStorage.GetCOByName("Andy").DayToDayPower;

            movementRange += action?.MovementRangeIncrease ?? 0;

            void addTileIfCanMoveTo(Vector2I position, int movement)
            {
                Dictionary<MovementType, int> moveCosts;

                TerrainType terrainType;

                if (TryGetDrawableBuilding(position, out var building))
                {
                    moveCosts = building.BuildingTile.MovementCostsPerType;
                    terrainType = TerrainType.Building;
                }
                else
                {
                    var tile = TileGrid[position.X, position.Y].TerrainTile;
                    moveCosts = tile.MovementCostsPerType;
                    terrainType = tile.TerrainType;
                }

                if (moveCosts.TryGetValue(unit.UnitData.MovementType, out var cost))
                {
                    if (dayToDay.MoveCostPerTile != null && CurrentWeather.Value != WeatherType.Snow)
                        cost = dayToDay.MoveCostPerTile.Value;
                    else if (CurrentWeather.Value != WeatherType.Clear)
                        cost = movementForWeather(unit.UnitData.MovementType, dayToDay.WeatherWithNoMovementAffect, dayToDay.WeatherWithAdditionalMovementAffect, terrainType, cost);

                    if (movement + cost <= movementRange)
                        queue.Enqueue(position, movement + cost);
                }
            }

            while (queue.TryDequeue(out var tilePos, out var movement))
            {
                if (visited.Contains(tilePos))
                    continue;

                visited.Add(tilePos);
                positions.Add(tilePos);

                var nextTile = tilePos + new Vector2I(1, 0);
                if (nextTile.X < MapSize.X && !visited.Contains(nextTile))
                    addTileIfCanMoveTo(nextTile, movement);

                nextTile = tilePos + new Vector2I(-1, 0);
                if (nextTile.X >= 0 && !visited.Contains(nextTile))
                    addTileIfCanMoveTo(nextTile, movement);

                nextTile = tilePos + new Vector2I(0, 1);
                if (nextTile.Y < MapSize.Y && !visited.Contains(nextTile))
                    addTileIfCanMoveTo(nextTile, movement);

                nextTile = tilePos + new Vector2I(0, -1);
                if (nextTile.Y >= 0 && !visited.Contains(nextTile))
                    addTileIfCanMoveTo(nextTile, movement);
            }
        }

        private int movementForWeather(MovementType moveType, WeatherType noAffect, WeatherType additionalEffect, TerrainType type, int cost)
        {
            if (CurrentWeather.Value == WeatherType.Clear || CurrentWeather.Value == noAffect)
                return cost;

            if (CurrentWeather.Value == WeatherType.Rain && additionalEffect != WeatherType.Rain)
            {
                if ((moveType & (MovementType.Tread | MovementType.Tire)) == 0)
                    return cost;

                return (type & (TerrainType.Plain | TerrainType.Forest)) != 0 ? cost + 1 : cost;
            }

            switch (moveType)
            {
                default:
                    return cost;

                case MovementType.Air:
                    return cost * 2;

                case MovementType.LightInf:
                    return (type & (TerrainType.Plain | TerrainType.Forest | TerrainType.Mountain)) != 0 ? cost * 2 : cost;

                case MovementType.HeavyInf:
                    return type == TerrainType.Mountain ? cost * 2 : cost;

                case MovementType.Lander:
                case MovementType.Sea:
                    return (type & (TerrainType.Sea | TerrainType.Building)) != 0 ? cost * 2 : cost;

                case MovementType.Tire:
                case MovementType.Tread:
                    return (type & (TerrainType.Plain | TerrainType.Forest)) != 0 ? cost + 1 : cost;
            }
        }

        private long? getPlayerIDFromCountryID(int countryID) => ReplayController.Players.FirstOrDefault(x => x.Value.OriginalCountryID == countryID).Value?.ID;

        public UnitData GetUnitDataForUnitName(string unitName) => UnitStorage.GetUnitByCode(unitName);

        public virtual TileCursor CreateTileCursor() => new TileCursor() { Alpha = 0f };
    }
}
