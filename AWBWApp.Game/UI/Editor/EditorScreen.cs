using System;
using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Editor;
using AWBWApp.Game.Editor.History;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.Input;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Editor.Components;
using AWBWApp.Game.UI.Replay;
using AWBWApp.Game.UI.Toolbar;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osuTK;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorScreen : EscapeableScreen, IKeyBindingHandler<AWBWGlobalAction>
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

        [Cached]
        private Bindable<BuildingTile> selectedBuilding = new Bindable<BuildingTile>();

        [Cached]
        private HistoryManager historyManager = new HistoryManager();

        [Resolved]
        private MainControlMenuBar menuBar { get; set; }

        private readonly CameraControllerWithGrid cameraControllerWithGrid;
        private readonly DetailedInformationPopup infoPopup;
        private readonly EditorGameMap map;
        private readonly EditorMenu menu;
        private readonly FillFlowContainer messageContainer;

        private string lastSaveLocation;

        private const int max_message_count = 5;

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
                    messageContainer = new FillFlowContainer()
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Position = new Vector2(0, -75),
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        LayoutEasing = Easing.OutCubic,
                        LayoutDuration = 100
                    }
                }
            });

            map.SetInfoPopup(infoPopup);
            map.OnLoadComplete += _ => cameraControllerWithGrid.FitMapToSpace();
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            menuBar.SetShowEditorMenu(true);
            menuBar.OnSaveEditorTriggered += () => saveMap(lastSaveLocation);
            menuBar.OnSaveAsEditorTriggered += () => saveMap(null);
            menuBar.OnUploadEditorTriggered += uploadMap;
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            menuBar.SetShowEditorMenu(false);
            return base.OnExiting(e);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            //loadDefaultMap();
            //return;

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
        }

        private void loadDefaultMap()
        {
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
        }

        private void saveMap(string saveLocation)
        {
            if (saveLocation.IsNullOrEmpty())
            {
                if (interruptOverlay.CurrentInterrupt != null)
                    return;

                interruptOverlay.Push(new FileSaveInterrupt(lastSaveLocation, saveMap));
                return;
            }

            var serializedMap = JsonConvert.SerializeObject(map.GenerateMap());
            SafeWriteHelper.WriteTextToFile(saveLocation, serializedMap);
            lastSaveLocation = saveLocation;
        }

        private void uploadMap()
        {
            if (interruptOverlay.CurrentInterrupt != null)
                return;

            interruptOverlay.Push(new UploadMapInterrupt(map.GenerateMap()));
        }

        public void ShowMessage(LocalisableString messageToShow)
        {
            if (messageContainer.Children.Count >= max_message_count)
                messageContainer.Remove(messageContainer.Children[0], true);

            messageContainer.Add(new TemporaryPopupMessage(messageToShow)
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre
            });
        }

        public bool OnPressed(KeyBindingPressEvent<AWBWGlobalAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case AWBWGlobalAction.Undo:
                    historyManager.Undo(this, map);
                    return true;

                case AWBWGlobalAction.Redo:
                    historyManager.Redo(this, map);
                    return true;

                case AWBWGlobalAction.Save:
                    saveMap(lastSaveLocation);
                    return true;

                case AWBWGlobalAction.SaveAs:
                    saveMap(null);
                    return true;

                case AWBWGlobalAction.ChangeSymmetry:
                    symmetryMode.Value = (symmetryMode.Value + 1) > SymmetryMode.Rotated ? 0 : (symmetryMode.Value + 1);
                    return true;

                case AWBWGlobalAction.PickTile:
                {
                    if (map.TryGetTileAtMousePosition(e.ScreenSpaceMousePosition, out var tile, out var building))
                    {
                        if (building != null)
                        {
                            selectedBuilding.Value = building;
                            selectedTile.Value = null;
                        }
                        else
                        {
                            selectedBuilding.Value = null;
                            selectedTile.Value = tile;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<AWBWGlobalAction> e)
        {
        }
    }
}
