using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.IO.Stores;
using osu.Framework.Screens;

namespace AWBWApp.Game.Tests.Visual.Logic
{
    public abstract class BaseGameMapTestScene : AWBWAppTestScene
    {
        [Cached]
        private TerrainTileStorage terrainTileStorage = new TerrainTileStorage();
        [Cached]
        private BuildingStorage buildingStorage = new BuildingStorage();
        [Cached]
        private UnitStorage unitStorage = new UnitStorage();
        [Cached]
        private COStorage coStorage = new COStorage();
        [Cached]
        private CountryStorage countryStorage = new CountryStorage();

        protected ReplayController ReplayController;

        protected ScreenStack ScreenStack;

        public BaseGameMapTestScene()
        {
            Add(ScreenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            });

            ScreenStack.Push(ReplayController = new ReplayController()
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        [BackgroundDependencyLoader]
        private void load(ResourceStore<byte[]> storage)
        {
            var tilesJson = storage.GetStream("Json/Tiles");
            terrainTileStorage.LoadStream(tilesJson);
            var buildingsJson = storage.GetStream("Json/Buildings");
            buildingStorage.LoadStream(buildingsJson);
            var unitsJson = storage.GetStream("Json/Units");
            unitStorage.LoadStream(unitsJson);
            var cosJson = storage.GetStream("Json/COs");
            coStorage.LoadStream(cosJson);
            var countriesJson = storage.GetStream("Json/Countries");
            countryStorage.LoadStream(countriesJson);
        }

        protected TerrainTileStorage GetTileStorage()
        {
            return terrainTileStorage;
        }

        protected BuildingStorage GetBuildingStorage()
        {
            return buildingStorage;
        }

        protected UnitStorage GetUnitStorage()
        {
            return unitStorage;
        }

        protected COStorage GetCOStorage()
        {
            return coStorage;
        }

        protected CountryStorage GetCountryStorage()
        {
            return countryStorage;
        }

        public ReplayMap CreateBasicMap(int x, int y)
        {
            var map = new ReplayMap
            {
                Size = new Vector2I(x, y),
                Ids = new short[x * y]
            };

            for (int i = 0; i < map.Ids.Length; i++)
                map.Ids[i] = 1;
            return map;
        }

        public ReplayData CreateEmptyReplay()
        {
            var data = new ReplayData();

            data.ReplayInfo.Players = new Dictionary<int, ReplayUser> { { 0, new ReplayUser { CountryId = 1, ID = 0, UserId = 0 } } };
            data.TurnData = new List<TurnData>
            {
                new TurnData
                {
                    ActivePlayerID = 0,
                    Players = new Dictionary<int, AWBWReplayPlayerTurn> { { 0, new AWBWReplayPlayerTurn { ActiveCOID = 1, RequiredPowerForNormal = 90000, RequiredPowerForSuper = 180000 } } },
                    ReplayUnit = new Dictionary<long, ReplayUnit>(),
                    Buildings = new Dictionary<Vector2I, ReplayBuilding>(),
                    StartWeather = new ReplayWeather { Type = Weather.Clear }
                }
            };

            return data;
        }
    }
}
