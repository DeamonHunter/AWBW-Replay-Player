using AWBWApp.Game.Editor.Overlays;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor.Overlays
{
    public partial class DrawableCaptureCalcEditorOverlay : Container
    {
        [Resolved]
        private Bindable<bool> showCaptureOverlay { get; set; }

        private readonly Container lineContainer;
        private readonly EditorGameMap map;
        private CaptureCalcEditorOverlay overlay;

        public DrawableCaptureCalcEditorOverlay(EditorGameMap gameMap)
        {
            map = gameMap;

            RelativeSizeAxes = Axes.Both;

            Masking = true;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChild = lineContainer = new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(0, DrawableTile.BASE_SIZE.Y), // (0,0) sits 1 tile above the map
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Children = new Drawable[] { }
            };
        }

        [BackgroundDependencyLoader]
        private void load(UnitStorage unitStorage)
        {
            overlay = new CaptureCalcEditorOverlay(map, unitStorage);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            showCaptureOverlay.BindValueChanged(_ => CalcAndShowCaptures(), true);

            CalcAndShowCaptures();
        }

        public void CalcAndShowCaptures()
        {
            if (!showCaptureOverlay.Value || overlay == null)
            {
                lineContainer.Hide();
                return;
            }

            lineContainer.Clear();

            var capPhase = overlay.CalculateCapPhase();

            foreach (var prop in capPhase.ContestedProps)
            {
                lineContainer.Add(new SpriteIcon()
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Position = getTileCenter(prop),
                    Size = new Vector2(10, 10),
                    Icon = FontAwesome.Solid.Dice,
                    Colour = new Colour4(200, 50, 50, 255)
                });
            }

            foreach (var factory in capPhase.CapChains.Keys)
            {
                var chainList = capPhase.CapChains[factory];

                foreach (var chain in chainList)
                {
                    var path = new Path();
                    path.PathRadius = 1.25f;
                    foreach (var stop in chain)
                        path.AddVertex(getTileCenter(stop));
                    path.Colour = new Color4(0.2f, 0.2f, 1f, 1f);
                    lineContainer.Add(path);
                }
            }
            lineContainer.Show();
        }

        private Vector2 getTileCenter(CaptureCalcEditorOverlay.CapStop stop) => getTileCenter(stop.Coord);

        private Vector2 getTileCenter(Vector2I mapPosition)
        {
            //Todo: This should probably be in a more central spot
            return new Vector2((mapPosition.X + 0.5f) * DrawableTile.BASE_SIZE.X, (mapPosition.Y + 0.5f) * DrawableTile.BASE_SIZE.Y);
        }
    }
}
