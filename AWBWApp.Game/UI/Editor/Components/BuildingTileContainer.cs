using System;
using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Tile;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class BuildingTileContainer : VisibilityContainer
    {
        public Action<TerrainTile, BuildingTile> SelectTileAction;
        public Action<int, TerrainTile, BuildingTile> SetHotbarAction;

        [Resolved]
        private BuildingStorage buildingStorage { get; set; }

        [Resolved]
        private Bindable<BuildingTile> selectedBuilding { get; set; }

        private FillFlowContainer baseContainer;
        private FillFlowContainer<EditorSpriteButton> neutralBuildingButtons;
        private FillFlowContainer<EditorSpriteButton> countryBuildingButtons;

        public BuildingTileContainer()
        {
            AutoSizeAxes = Axes.Y;
            Width = 200;
            Masking = true;
            CornerRadius = 6;

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(25, 25, 25, 180)
                },
                baseContainer = new FillFlowContainer()
                {
                    Padding = new MarginPadding(5),
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
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
        private void load()
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

        public void AddDropDowns(EditorDetachedDropdown<CountryData> selectedCountry, EditorDetachedDropdown<CountryData> symmetryCountry)
        {
            baseContainer.Add(new SpriteText()
            {
                Margin = new MarginPadding { Left = 5 },
                Text = "Placed"
            });
            var selectedCountryHeader = (EditorDetachedDropdown<CountryData>.EditorDropdownHeader)selectedCountry.GetDetachedHeader();
            selectedCountryHeader.Anchor = Anchor.TopCentre;
            selectedCountryHeader.Origin = Anchor.TopCentre;
            selectedCountryHeader.MinimumWidth = 180;
            baseContainer.Add(selectedCountryHeader);

            baseContainer.Add(new SpriteText()
            {
                Margin = new MarginPadding { Left = 5 },
                Text = "Symmetry"
            });
            var symmetryCountryHeader = (EditorDetachedDropdown<CountryData>.EditorDropdownHeader)symmetryCountry.GetDetachedHeader();
            symmetryCountryHeader.Anchor = Anchor.TopCentre;
            symmetryCountryHeader.Origin = Anchor.TopCentre;
            symmetryCountryHeader.MinimumWidth = 180;
            baseContainer.Add(symmetryCountryHeader);

            selectedCountry.Current.BindValueChanged(x => selectedCountryChanged(x.NewValue), true);
        }

        private void selectedCountryChanged(CountryData newCountry)
        {
            if (newCountry == null)
                return;

            foreach (var button in countryBuildingButtons)
            {
                var building = button.Building;
                if (building.CountryID == newCountry.AWBWID)
                    continue;

                button.Building = buildingStorage.GetBuildingByTypeAndCountry(building.BuildingType, newCountry.AWBWID);
            }

            if (selectedBuilding?.Value != null && selectedBuilding.Value.CountryID > 0)
                SelectTileAction(null, buildingStorage.GetBuildingByTypeAndCountry(selectedBuilding.Value.BuildingType, newCountry.AWBWID));
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

        private class DropDownLabel : SpriteText
        {
        }
    }
}
