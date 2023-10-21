﻿using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class BasicTileContainer : VisibilityContainer
    {
        public Action<TerrainTile, BuildingTile> SelectTileAction;
        public Action<int, TerrainTile, BuildingTile> SetHotbarAction;

        private FillFlowContainer<EditorSpriteButton> tileButtons;

        public BasicTileContainer()
        {
            AutoSizeAxes = Axes.Y;
            Width = 190;
            Masking = true;
            CornerRadius = 6;

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(25, 25, 25, 180)
                },
                tileButtons = new FillFlowContainer<EditorSpriteButton>()
                {
                    Direction = FillDirection.Full,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(BuildingStorage buildingStorage, TerrainTileStorage tileStorage)
        {
            var tileIDs = new List<int>()
            {
                1, 2, 3, 28, 33, 29, 30, 31, 32,
                4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
                15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27,
                101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
                113, 114, 115, 116, 111, 112, 195
            };

            foreach (var tileID in tileIDs)
            {
                if (buildingStorage.TryGetBuildingByAWBWId(tileID, out var building))
                {
                    tileButtons.Add(new EditorSpriteButton()
                    {
                        Building = building,
                        Action = (x, y) => SelectTileAction?.Invoke(x, y),
                        Scale = new Vector2(0.75f)
                    });
                }
                else
                {
                    tileButtons.Add(new EditorSpriteButton()
                    {
                        Tile = tileStorage.GetTileByAWBWId(tileID),
                        Action = (x, y) => SelectTileAction?.Invoke(x, y),
                        Scale = new Vector2(0.75f)
                    });
                }
            }
        }

        protected override void PopIn()
        {
            this.ScaleTo(new Vector2(0, 0.8f)).ScaleTo(1, 150, Easing.OutQuint)
                .FadeOut().FadeIn(150, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.ScaleTo(new Vector2(0, 0.8f), 150, Easing.OutQuint)
                .FadeOut(150, Easing.OutQuint);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            base.OnMouseDown(e);
            return true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key < Key.Number0 || e.Key > Key.Number9)
                return base.OnKeyDown(e);

            foreach (var button in tileButtons)
            {
                if (!button.IsHovered)
                    continue;

                var slotNumber = (e.Key - Key.Number0) - 1;
                if (slotNumber < 0)
                    slotNumber += 10;

                SetHotbarAction?.Invoke(slotNumber, button.Tile, button.Building);
                SelectTileAction?.Invoke(button.Tile, button.Building);
                return true;
            }

            return false;
        }
    }
}
