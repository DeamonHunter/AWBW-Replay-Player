using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Tests.Visual;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;

namespace AWBWApp.Game.Tests
{
    public class TestSceneGameMap : AWBWAppTestScene
    {
        [Cached]
        private TerrainTileStorage terrainTileStorage = new TerrainTileStorage();
        [Cached]
        private BuildingStorage buildingStorage = new BuildingStorage();
        [Cached]
        private UnitStorage unitStorage = new UnitStorage();

        private ReplayController replayController;

        public TestSceneGameMap()
        {
            Add(replayController = new ReplayController()
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

            replayController.LoadInitialGameState(393637);
        }
    }
}
