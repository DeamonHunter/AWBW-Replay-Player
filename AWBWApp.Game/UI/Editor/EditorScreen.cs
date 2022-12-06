using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Editor;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorScreen : EscapeableScreen
    {
        [Cached(type: typeof(IBindable<MapSkin>))]
        private Bindable<MapSkin> MapSkin = new Bindable<MapSkin>(AWBWApp.Game.MapSkin.AW2);

        [Resolved]
        private InterruptDialogueOverlay interruptOverlay { get; set; }

        [Cached]
        private Bindable<SymmetryMode> symmetryMode = new Bindable<SymmetryMode>();

        [Cached]
        private Bindable<SymmetryDirection> symmetryDirection = new Bindable<SymmetryDirection>();

        [Cached]
        private Bindable<TerrainTile> selectedTile = new Bindable<TerrainTile>();

        private readonly CameraControllerWithGrid cameraControllerWithGrid;
        private readonly DetailedInformationPopup infoPopup;
        private readonly EditorGameMap map;
        private readonly EditorMenu menu;

        public EditorScreen()
        {
            var mapPadding = new MarginPadding
            {
                Top = DrawableTile.HALF_BASE_SIZE.Y,
                Bottom = 8 + DrawableTile.HALF_BASE_SIZE.Y,
                Left = DrawableTile.HALF_BASE_SIZE.X,
                Right = 201 + DrawableTile.HALF_BASE_SIZE.X
            };

            var safeMovement = new MarginPadding
            {
                Top = mapPadding.Top + DrawableTile.BASE_SIZE.Y * 4,
                Bottom = mapPadding.Bottom + DrawableTile.BASE_SIZE.Y * 4,
                Left = mapPadding.Left + DrawableTile.BASE_SIZE.X * 4,
                Right = mapPadding.Right + DrawableTile.BASE_SIZE.X * 4,
            };

            AddInternal(new AWBWNonRelativeContextMenuContainer()
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    cameraControllerWithGrid = new CameraControllerWithGrid()
                    {
                        AllowLeftMouseToDrag = false,
                        MaxScale = 8,
                        MapSpace = mapPadding,
                        MovementRegion = safeMovement,
                        RelativeSizeAxes = Axes.Both,
                        Child = map = new EditorGameMap(),
                    },
                    menu = new EditorMenu(),
                    infoPopup = new DetailedInformationPopup(),
                }
            });

            map.SetInfoPopup(infoPopup);
            map.OnLoadComplete += _ => cameraControllerWithGrid.FitMapToSpace();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var taskCompletion = new TaskCompletionSource<ReplayMap>();
            interruptOverlay.Push(new DownloadOrCreateMapInterrupt(taskCompletion));
            Task.Run(async () =>
            {
                ReplayMap info;

                try
                {
                    info = await taskCompletion.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                Schedule(() =>
                {
                    map.SetMap(info);
                    ScheduleAfterChildren(() => cameraControllerWithGrid.FitMapToSpace());
                });
            });
            /*
            ScheduleAfterChildren(() =>
            {
                var emptyMap = new ReplayMap()
                {
                    TerrainName = "Editor Test",
                    Size = new Vector2I(16, 16),
                    Ids = new short[16 * 16]
                };
                Array.Fill(emptyMap.Ids, (short)1);
                map.SetMap(emptyMap);
                ScheduleAfterChildren(() => cameraControllerWithGrid.FitMapToSpace());
            });
            */
        }
    }
}
