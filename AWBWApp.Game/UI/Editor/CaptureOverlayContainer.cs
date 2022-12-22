using System;
using AWBWApp.Game.Editor;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace AWBWApp.Game.UI.Editor
{
    public partial class CaptureOverlayContainer : Container
    {
        [Resolved]
        private Bindable<bool> showCaptureOverlay { get; set; }
        [Resolved]
        private BuildingStorage buildingStorage { get; set; }
        [Resolved]
        private UnitStorage unitStorage { get; set; }

        private Container lineContainer;
        private EditorGameMap map;

        public CaptureOverlayContainer(EditorGameMap gameMap)
        {
            map = gameMap;
            RelativeSizeAxes = Axes.Both;

            Masking = true;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChild = lineContainer = new Container()
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(20, 2),
                    Position = new Vector2(0, DrawableTile.BASE_SIZE.Y), // (0,0) sits 1 tile above the map
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Children = new Drawable[] {}
                };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            showCaptureOverlay.BindValueChanged(_ => calcAndShowCaptures(), true);

            calcAndShowCaptures();
        }

        public void calcAndShowCaptures()
        {
            if (!showCaptureOverlay.Value)
            {
                lineContainer.Hide();
                return;
            }

            lineContainer.Clear();

            var capPhase = CaptureCalcHelper.CalculateCapPhase(buildingStorage, unitStorage, map);
            foreach (var prop in capPhase.contestedProps)
            {
                var coord = new Vector2((prop.X + 0.5f) * DrawableTile.BASE_SIZE.X, (prop.Y + 0.5f) * DrawableTile.BASE_SIZE.Y);
                lineContainer.Add(new SpriteIcon()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Position = coord,
                            Size = new Vector2(6, 4),
                            Icon = FontAwesome.Solid.Dice,
                            Colour = new Colour4(200, 50, 50, 255)
                        });
            }
            foreach (var factory in capPhase.capChains.Keys)
            {
                var chainList = capPhase.capChains[factory];
                var arrowAlpha = 1.0;
                foreach (var chain in chainList)
                {
                    var coordStart = chain[0].coord;
                    var arrowStart = new Vector2((coordStart.X + 0.5f) * DrawableTile.BASE_SIZE.X, (coordStart.Y + 0.5f) * DrawableTile.BASE_SIZE.Y);
                    for (int i = 1; i < chain.Count; ++i) // Skip the first node since it's the factory
                    {
                        var coordEnd = chain[i].coord;
                        var arrowEnd = new Vector2((coordEnd.X + 0.5f) * DrawableTile.BASE_SIZE.X, (coordEnd.Y + 0.5f) * DrawableTile.BASE_SIZE.Y);
                        var arrowDiff = new Vector2(coordEnd.X - coordStart.X, coordEnd.Y - coordStart.Y);
                        // I am not what you'd call a trig wizard
                        var arrowAngle = Math.Atan2(arrowDiff.X, -arrowDiff.Y) * 180 / Math.PI;
                        lineContainer.Add(new Box()
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.BottomCentre,
                                    Position = arrowStart,
                                    Rotation = (float)arrowAngle,
                                    Size = new Vector2(4, arrowDiff.Length * DrawableTile.BASE_SIZE.X),
                                    Colour = new Colour4(0.2f, 0.2f, 1f, (float)arrowAlpha)
                                });
                        coordStart = coordEnd;
                        arrowStart = arrowEnd;
                    }
                    arrowAlpha = Math.Max(0.2, arrowAlpha*0.75);
                }
            }
            lineContainer.Show();
        }
    }
}
