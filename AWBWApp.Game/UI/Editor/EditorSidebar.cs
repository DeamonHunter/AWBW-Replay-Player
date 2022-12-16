using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.UI.Editor.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorSidebar : Container
    {
        private FillFlowContainer<EditorSpriteButton> selectionBar;
        private BasicTileContainer basicTileContainer;
        private BuildingTileContainer buildingTileContainer;

        [Resolved]
        private Bindable<TerrainTile> selectedTile { get; set; }

        [Resolved]
        private Bindable<BuildingTile> selectedBuilding { get; set; }

        [Resolved]
        private EditorHotbar hotbar { get; set; }

        public EditorSidebar()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                new MouseDownBlockingContainer()
                {
                    Masking = true,
                    CornerRadius = 6,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(25, 25, 25, 180)
                        },
                        selectionBar = new FillFlowContainer<EditorSpriteButton>()
                        {
                            Margin = new MarginPadding { Vertical = 5 },
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding { Horizontal = 4 }
                        },
                    }
                },
                basicTileContainer = new BasicTileContainer()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(60, 0),
                    SelectTileAction = selectTile,
                    SetHotbarAction = (x, y, z) => hotbar?.SetHotbarSlot(x, y, z)
                },
                buildingTileContainer = new BuildingTileContainer()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(60, 0),
                    SelectTileAction = selectTile,
                    SetHotbarAction = (x, y, z) => hotbar?.SetHotbarSlot(x, y, z)
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(BuildingStorage buildingStorage, TerrainTileStorage tileStorage)
        {
            selectionBar.Children = new EditorSpriteButton[]
            {
                new EditorSpriteButton()
                {
                    Tile = tileStorage.GetTileByAWBWId(1),
                    Action = (_, _) => showContainer(basicTileContainer.State.Value == Visibility.Visible ? -1 : 0)
                },
                new EditorSpriteButton()
                {
                    Building = buildingStorage.GetBuildingByAWBWId(38),
                    Action = (_, _) => showContainer(buildingTileContainer.State.Value == Visibility.Visible ? -1 : 1)
                }
            };
        }

        private void showContainer(int containerID)
        {
            basicTileContainer.State.Value = containerID == 0 ? Visibility.Visible : Visibility.Hidden;
            buildingTileContainer.State.Value = containerID == 1 ? Visibility.Visible : Visibility.Hidden;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            //selectedTile.BindValueChanged(_ => onSelectedTileChanged(), true);
        }

        private void selectTile(TerrainTile tile, BuildingTile building)
        {
            selectedTile.Value = tile;
            selectedBuilding.Value = building;
        }

        private partial class MouseDownBlockingContainer : Container
        {
            protected override bool OnMouseDown(MouseDownEvent e)
            {
                base.OnMouseDown(e);
                return true;
            }
        }

        private partial class BasicTileContainer : VisibilityContainer
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
                    113, 114, 115, 116, 111, 112,
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

        private partial class BuildingTileContainer : VisibilityContainer
        {
            public Action<TerrainTile, BuildingTile> SelectTileAction;
            public Action<int, TerrainTile, BuildingTile> SetHotbarAction;

            private FillFlowContainer<EditorSpriteButton> neutralBuildingButtons;
            private FillFlowContainer<EditorSpriteButton> countryBuildingButtons;

            public BuildingTileContainer()
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
                    new FillFlowContainer()
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            neutralBuildingButtons = new FillFlowContainer<EditorSpriteButton>()
                            {
                                Direction = FillDirection.Full,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y
                            },
                            countryBuildingButtons = new FillFlowContainer<EditorSpriteButton>()
                            {
                                Direction = FillDirection.Full,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y
                            }
                        }
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(BuildingStorage buildingStorage)
            {
                var neutralIds = new List<int>()
                {
                    34, 35, 36, 37, 133, 145,
                };

                foreach (var tileID in neutralIds)
                {
                    neutralBuildingButtons.Add(new EditorSpriteButton()
                    {
                        Building = buildingStorage.GetBuildingByAWBWId(tileID),
                        Action = (x, y) => SelectTileAction?.Invoke(x, y),
                        Scale = new Vector2(0.75f)
                    });
                }

                var orangeStarIds = new List<int>()
                {
                    42, 38, 39, 40, 41, 134, 146
                };

                foreach (var tileID in orangeStarIds)
                {
                    countryBuildingButtons.Add(new EditorSpriteButton()
                    {
                        Building = buildingStorage.GetBuildingByAWBWId(tileID),
                        Action = (x, y) => SelectTileAction?.Invoke(x, y),
                        Scale = new Vector2(0.75f)
                    });
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

                foreach (var button in neutralBuildingButtons)
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

                foreach (var button in countryBuildingButtons)
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
}
