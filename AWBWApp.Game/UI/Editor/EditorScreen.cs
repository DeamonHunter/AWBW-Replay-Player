using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace AWBWApp.Game.UI.Editor
{
    public class EditorScreen : EscapeableScreen
    {
        [Cached(type: typeof(IBindable<MapSkin>))]
        private Bindable<MapSkin> MapSkin = new Bindable<MapSkin>(AWBWApp.Game.MapSkin.AW2);

        private readonly CameraControllerWithGrid cameraControllerWithGrid;
        private readonly DetailedInformationPopup infoPopup;
        private readonly EditorGameMap map;

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
                        MaxScale = 8,
                        MapSpace = mapPadding,
                        MovementRegion = safeMovement,
                        RelativeSizeAxes = Axes.Both,
                        Child = map = new EditorGameMap(),
                    },
                    infoPopup = new DetailedInformationPopup(),
                }
            });

            map.SetInfoPopup(infoPopup);
        }
    }
}
