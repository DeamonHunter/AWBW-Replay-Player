using System.Collections.Generic;
using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Country;
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

namespace AWBWApp.Game.UI.Editor
{
    public partial class EditorSidebar : Container
    {
        [Resolved]
        private Bindable<TerrainTile> selectedTile { get; set; }

        [Resolved]
        private Bindable<BuildingTile> selectedBuilding { get; set; }

        [Resolved]
        private Bindable<(CountryData, CountryData)> selectedCountries { get; set; }

        [Resolved]
        private EditorHotbar hotbar { get; set; }

        private FillFlowContainer<EditorSpriteButton> selectionBar;

        private BasicTileContainer basicTileContainer;
        private EditorSpriteButton basicTileButton;

        private BuildingTileContainer buildingTileContainer;
        private EditorSpriteButton buildingTileButton;

        private OverlayButtonContainer overlayButtonsContainer;
        private EditorSpriteButton overlayButtonsButton;

        private EditorDetachedDropdown<CountryData> selectedCountry;
        private EditorDetachedDropdown<CountryData> symmetryCountry;

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
                },
                overlayButtonsContainer = new OverlayButtonContainer()
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Position = new Vector2(60, 0)
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(BuildingStorage buildingStorage, TerrainTileStorage tileStorage, CountryStorage countryStorage)
        {
            selectionBar.Children = new EditorSpriteButton[]
            {
                basicTileButton = new EditorSpriteButton()
                {
                    Tile = tileStorage.GetTileByAWBWId(1),
                    TooltipText = "Basic Tiles",
                    Action = (_, _) => showContainer(basicTileContainer.State.Value == Visibility.Visible ? -1 : 0)
                },
                buildingTileButton = new EditorSpriteButton()
                {
                    Building = buildingStorage.GetBuildingByAWBWId(38),
                    TooltipText = "Buildings",
                    Action = (_, _) => showContainer(buildingTileContainer.State.Value == Visibility.Visible ? -1 : 1)
                },
                overlayButtonsButton = new EditorSpriteButton()
                {
                    TexturePath = "Effects/Target.png",
                    TooltipText = "Overlays",
                    Action = (_, _) => showContainer(overlayButtonsContainer.State.Value == Visibility.Visible ? -1 : 2)
                },
            };

            var countries = new List<CountryData>();

            foreach (var countryID in countryStorage.GetAllCountryIDs())
                countries.Add(countryStorage.GetCountryByAWBWID(countryID));

            Add(selectedCountry = new EditorDetachedDropdown<CountryData>()
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Position = new Vector2(250, 0),
                OffsetHeight = -40,
                Items = countries,
                Width = 150
            });

            Add(symmetryCountry = new EditorDetachedDropdown<CountryData>()
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Position = new Vector2(250, 0),
                OffsetHeight = 25,
                Items = countries,
                Width = 150
            });

            selectedCountries.BindValueChanged(x =>
            {
                selectedCountry.Current.Value = x.NewValue.Item1;
                symmetryCountry.Current.Value = x.NewValue.Item2;
            });
            selectedCountry.Current.BindValueChanged(x => selectedCountries.Value = (x.NewValue, selectedCountries.Value.Item2));
            symmetryCountry.Current.BindValueChanged(x => selectedCountries.Value = (selectedCountries.Value.Item1, x.NewValue));

            buildingTileContainer.AddDropDowns(selectedCountry, symmetryCountry);
        }

        private void showContainer(int containerID)
        {
            basicTileContainer.State.Value = containerID == 0 ? Visibility.Visible : Visibility.Hidden;
            basicTileButton.SetSelected(basicTileContainer.State.Value == Visibility.Visible);

            buildingTileContainer.State.Value = containerID == 1 ? Visibility.Visible : Visibility.Hidden;
            buildingTileButton.SetSelected(buildingTileContainer.State.Value == Visibility.Visible);

            overlayButtonsContainer.State.Value = containerID == 2 ? Visibility.Visible : Visibility.Hidden;
            overlayButtonsButton.SetSelected(overlayButtonsContainer.State.Value == Visibility.Visible);
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
    }
}
