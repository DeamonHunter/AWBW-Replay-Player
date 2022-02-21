using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI;
using AWBWApp.Resources;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.IO.Stores;
using osuTK;

namespace AWBWApp.Game
{
    public class AWBWAppGameBase : osu.Framework.Game
    {
        // Anything in this class is shared between the test browser and the game implementation.
        // It allows for caching global dependencies that should be accessible to tests, or changing
        // the screen scaling for all components including the test browser and framework overlays.

        protected override Container<Drawable> Content { get; }

        private NearestNeighbourTextureStore unfilteredTextures;
        private DependencyContainer dependencies;
        private ResourceStore<byte[]> fileStorage;
        private ReplayManager replayStorage;
        private MapFileStorage mapStorage;

        private TerrainTileStorage terrainTileStorage;
        private BuildingStorage buildingStorage;
        private UnitStorage unitStorage;
        private COStorage coStorage;
        private CountryStorage countryStorage;

        protected AWBWAppGameBase()
        {
            // Ensure game and tests scale with window size and screen DPI.
            base.Content.Add(Content = new DrawSizePreservingFillContainer
            {
                // You may want to change TargetDrawSize to your "default" resolution, which will decide how things scale and position when using absolute coordinates.
                TargetDrawSize = new Vector2(1366, 768)
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new DllResourceStore(typeof(AWBWAppResources).Assembly));

            unfilteredTextures = new NearestNeighbourTextureStore(Textures);
            dependencies.Cache(unfilteredTextures);

            fileStorage = new ResourceStore<byte[]>(Resources);
            fileStorage.AddExtension(".json");
            dependencies.Cache(fileStorage);

            replayStorage = new ReplayManager();
            dependencies.Cache(replayStorage);

            mapStorage = new MapFileStorage();
            dependencies.Cache(mapStorage);

            var tilesJson = fileStorage.GetStream("Json/Tiles");
            terrainTileStorage = new TerrainTileStorage();
            terrainTileStorage.LoadStream(tilesJson);
            dependencies.Cache(terrainTileStorage);

            var buildingsJson = fileStorage.GetStream("Json/Buildings");
            buildingStorage = new BuildingStorage();
            buildingStorage.LoadStream(buildingsJson);
            dependencies.Cache(buildingStorage);

            var unitsJson = fileStorage.GetStream("Json/Units");
            unitStorage = new UnitStorage();
            unitStorage.LoadStream(unitsJson);
            dependencies.Cache(unitStorage);

            var cosJson = fileStorage.GetStream("Json/COs");
            coStorage = new COStorage();
            coStorage.LoadStream(cosJson);
            dependencies.Cache(coStorage);

            var countriesJson = fileStorage.GetStream("Json/Countries");
            countryStorage = new CountryStorage();
            countryStorage.LoadStream(countriesJson);
            dependencies.Cache(countryStorage);
        }

        protected override UserInputManager CreateUserInputManager() => new AWBWAppUserInputManager();

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
    }
}
