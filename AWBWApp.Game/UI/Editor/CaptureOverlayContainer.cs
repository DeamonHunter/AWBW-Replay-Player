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

        private IconUsage[] dice = {
            FontAwesome.Solid.DiceOne,
            FontAwesome.Solid.DiceTwo,
            FontAwesome.Solid.DiceThree,
            FontAwesome.Solid.DiceFour,
            FontAwesome.Solid.DiceFive,
            FontAwesome.Solid.DiceSix,
            FontAwesome.Solid.Dice,
            };

        public CaptureOverlayContainer(EditorGameMap gameMap)
        {
            map = gameMap;
            RelativeSizeAxes = Axes.Both;

            Masking = true;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                lineContainer = new Container()
                {
                    RelativeSizeAxes = Axes.Y,
                    Size = new Vector2(20, 2),
                    Position = new Vector2(0, DrawableTile.BASE_SIZE.Y), // (0,0) sits 1 tile above the map
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.Centre,
                    Children = new Drawable[] {}
                },
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

            // var adjustedCenter = new Vector2((symmetryCenter.X + 1) / 2.0f, (symmetryCenter.Y + 1) / 2.0f) * DrawableTile.BASE_SIZE;
            // adjustedCenter.Y += DrawableTile.BASE_SIZE.Y;
            // lineContainer.Position = adjustedCenter;
            // arrowA.Rotation = (arrowA.Rotation + 90) % 180;
            var capPhase = CaptureCalcHelper.CalculateCapPhase(buildingStorage, unitStorage, map);
            foreach (var factory in capPhase.capChains.Keys)
            {
                var chainList = capPhase.capChains[factory];
                int dieIconIndex = 0;
                foreach (var chain in chainList)
                {
                    for (int i = 1; i < chain.Count; ++i) // Skip the first node since it's the factory
                    {
                        var node = chain[i];
                        var coord = new Vector2((node.coord.X + 0.5f) * DrawableTile.BASE_SIZE.X, (node.coord.Y + 0.5f) * DrawableTile.BASE_SIZE.Y);
                        lineContainer.Add(new SpriteIcon()
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Position = coord,
                                    Size = new Vector2(6, 4),
                                    Icon = dice[dieIconIndex],
                                    Colour = new Colour4(20, 50, 50, 255)
                                });
                    }
                    dieIconIndex = Math.Max(dice.Length, 1 + dieIconIndex);
                }
            }
            lineContainer.Show();
        }
    }
}
