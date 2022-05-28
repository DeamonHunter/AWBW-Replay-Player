using System;
using System.Reflection;
using System.Threading.Tasks;
using AWBWApp.Game.API;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.Input;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Interrupts;
using AWBWApp.Game.UI.Notifications;
using AWBWApp.Resources;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;

namespace AWBWApp.Game
{
    public class AWBWAppGameBase : osu.Framework.Game
    {
        // Anything in this class is shared between the test browser and the game implementation.
        // It allows for caching global dependencies that should be accessible to tests, or changing
        // the screen scaling for all components including the test browser and framework overlays.

        protected override Container<Drawable> Content { get; }

        protected AWBWConfigManager LocalConfig { get; set; }

        protected Storage HostStorage { get; set; }

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

        private InterruptDialogueOverlay interruptOverlay;
        private AWBWSessionHandler sessionHandler;

        private GlobalActionContainer globalBindings;

        protected AWBWAppGameBase()
        {
            // Ensure game and tests scale with window size and screen DPI.
            base.Content.Add(Content = new DrawSizePreservingFillContainer
            {
                // You may want to change TargetDrawSize to your "default" resolution, which will decide how things scale and position when using absolute coordinates.
                TargetDrawSize = new Vector2(1366, 768)
            });

            Name = @"AWBW Replay Player";
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new DllResourceStore(typeof(AWBWAppResources).Assembly));

            dependencies.CacheAs(this);
            dependencies.Cache(HostStorage);
            dependencies.Cache(LocalConfig);

            unfilteredTextures = new NearestNeighbourTextureStore(Textures);
            dependencies.Cache(unfilteredTextures);

            fileStorage = new ResourceStore<byte[]>(Resources);
            fileStorage.AddExtension(".json");
            fileStorage.AddExtension(".zip");
            dependencies.Cache(fileStorage);

            replayStorage = new ReplayManager(HostStorage);
            dependencies.Cache(replayStorage);

            mapStorage = new MapFileStorage(HostStorage);
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

            LoadComponentAsync(interruptOverlay = new InterruptDialogueOverlay(), Add);
            dependencies.Cache(interruptOverlay);

            sessionHandler = new AWBWSessionHandler();
            dependencies.Cache(sessionHandler);

            base.Content.Add(globalBindings = new GlobalActionContainer(this, HostStorage));
            dependencies.Cache(globalBindings);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            replayStorage.PostLoad();
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            HostStorage ??= host.Storage;

            LocalConfig ??= new AWBWConfigManager(HostStorage);
        }

        public virtual Version AssemblyVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

        public bool IsDeployedBuild => AssemblyVersion.Major > 0 || AssemblyVersion.Minor > 0;

        public virtual string Version
        {
            get
            {
                if (!IsDeployedBuild)
                    return @"local " + (DebugUtils.IsDebugBuild ? @"debug" : @"release");

                var version = AssemblyVersion;
                return $@"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        protected override void Update()
        {
            base.Update();

            mapStorage.CheckForMapsToDownload();
        }

        public virtual async Task ImportFiles(ProgressNotification updateNotification, params string[] paths)
        {
            if (paths.Length == 0)
                return;

            //Todo: Are we going to have any other extensions?

            if (interruptOverlay?.CurrentInterrupt is GetNewReplayInterrupt || interruptOverlay?.CurrentInterrupt is LoginInterrupt)
                Schedule(interruptOverlay.PopAll);

            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];

                try
                {
                    var data = await replayStorage.ParseAndStoreReplay(path);
                    var hasMap = mapStorage.HasMap(data.ReplayInfo.MapId);
                    await mapStorage.GetOrAwaitDownloadMap(data.ReplayInfo.MapId);
                    if (!hasMap)
                        replayStorage.ReplayChanged?.Invoke(data.ReplayInfo);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to parse replay: " + e.Message);
                }

                if (updateNotification != null)
                    updateNotification.Progress = (float)i / paths.Length;

                await Task.Delay(150);
            }

            if (updateNotification != null)
                updateNotification.State = ProgressNotificationState.Completed;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            LocalConfig?.Dispose();
            globalBindings?.Dispose();
        }

        public void GracefullyExit()
        {
            if (!OnExiting())
                Exit();
            else
                Scheduler.AddDelayed(GracefullyExit, 2000);
        }

        protected override UserInputManager CreateUserInputManager() => new AWBWAppUserInputManager();

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
    }
}
