using AWBWApp.Game.Game.Building;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Tile;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class DetailedInformationPopup : Container
    {
        private TerrainPopup terrainPopup;
        private BuildingPopup buildingPopup;
        private UnitPopup unitPopup;

        private Bindable<bool> playerListLeftSide;

        public DetailedInformationPopup()
        {
            AutoSizeAxes = Axes.X;
            CornerRadius = 8;
            Height = 130;
            AutoSizeDuration = 250;
            AutoSizeEasing = Easing.OutQuint;
            Masking = true;

            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;
            Position = new Vector2(10, -10);

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(25, 25, 25, 180),
                },
                new FillFlowContainer()
                {
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(2, 0),
                    Children = new Drawable[]
                    {
                        terrainPopup = new TerrainPopup
                        {
                            Alpha = 0
                        },
                        buildingPopup = new BuildingPopup
                        {
                            Alpha = 0
                        },
                        unitPopup = new UnitPopup
                        {
                            Alpha = 0
                        },
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager configManager)
        {
            playerListLeftSide = configManager.GetBindable<bool>(AWBWSetting.PlayerListLeftSide);
            playerListLeftSide.BindValueChanged(x =>
            {
                Anchor = x.NewValue ? Anchor.BottomRight : Anchor.BottomLeft;
                Origin = x.NewValue ? Anchor.BottomRight : Anchor.BottomLeft;
                Position = x.NewValue ? new Vector2(-10, -10) : new Vector2(10, -10);
            }, true);
        }

        public void ShowDetails(DrawableTile tile, DrawableBuilding building, DrawableUnit unit)
        {
            if (building != null)
            {
                buildingPopup.BindTo(building);
                terrainPopup.Reset();
            }
            else if (tile != null)
            {
                terrainPopup.BindTo(tile);
                buildingPopup.Reset();
            }
            else
            {
                terrainPopup.Reset();
                buildingPopup.Reset();
            }

            if (unit != null)
                unitPopup.BindTo(unit);
            else
                unitPopup.Reset();
        }

        private class TerrainPopup : Container
        {
            private Sprite terrainSprite;
            private StatContainer terrainStarCounter;

            private DrawableTile boundToTile;

            [Resolved]
            private NearestNeighbourTextureStore textureStore { get; set; }

            [Resolved]
            private IBindable<MapSkin> currentSkin { get; set; }

            public TerrainPopup()
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Y;
                Width = 50;
                Masking = true;
                CornerRadius = 8;
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(20, 20, 20, 150)
                    },
                    new FillFlowContainer()
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Top = 10, Bottom = 5 },
                        Children = new Drawable[]
                        {
                            new Container()
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Child = terrainSprite = new Sprite()
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Size = new Vector2(32)
                                },
                            },
                            terrainStarCounter = new StatContainer("UI/TerrainStar"),
                        }
                    }
                };
            }

            public void BindTo(DrawableTile tile)
            {
                if (tile == boundToTile)
                    return;

                boundToTile = tile;

                terrainSprite.Texture = textureStore.Get($"Map/{currentSkin.Value}/{tile.TerrainTile.Textures[WeatherType.Clear]}");
                terrainSprite.Size = terrainSprite.Texture.Size * 2;
                Show();
                terrainStarCounter.SetTo(tile.TerrainTile.BaseDefence);
            }

            public void Reset()
            {
                Hide();
                terrainStarCounter.SetTo(0);
                boundToTile = null;
            }
        }

        private class BuildingPopup : Container
        {
            private Container spriteContainer;
            private StatContainer terrainStarCounter;
            private StatContainer buildingHPCounter;

            private DrawableBuilding boundToBuilding;

            [Resolved]
            private BuildingStorage buildingStorage { get; set; }

            [Resolved]
            private NearestNeighbourTextureStore textureStore { get; set; }

            [Resolved]
            private IBindable<MapSkin> currentSkin { get; set; }

            public BuildingPopup()
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Y;
                Width = 50;
                Masking = true;
                CornerRadius = 8;
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(20, 20, 20, 150)
                    },
                    new FillFlowContainer()
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Top = 10, Bottom = 5 },
                        Children = new Drawable[]
                        {
                            spriteContainer = new Container()
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both
                            },
                            terrainStarCounter = new StatContainer("UI/TerrainStar"),
                            buildingHPCounter = new StatContainer("UI/BuildingsCaptured")
                        }
                    }
                };
            }

            public void BindTo(DrawableBuilding building)
            {
                if (building == boundToBuilding)
                    return;

                boundToBuilding = building;

                var drawnBuiilding = building.BuildingTile;

                if (drawnBuiilding.CountryID != -1)
                {
                    var country = building.GetCurrentCountry();
                    if (country != null)
                        drawnBuiilding = buildingStorage.GetBuildingByTypeAndCountry(drawnBuiilding.BuildingType, country.AWBWID);
                }

                var unitSprite = new TextureAnimation()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                };
                spriteContainer.Child = unitSprite;

                var firstTexture = textureStore.Get($"Map/{currentSkin.Value}/{drawnBuiilding.Textures[WeatherType.Clear]}-0");
                unitSprite.Size = firstTexture.Size * 2;

                if (drawnBuiilding.Frames != null)
                {
                    unitSprite.AddFrame(firstTexture, drawnBuiilding.Frames[0]);
                    for (int i = 1; i < drawnBuiilding.Frames.Length; i++)
                        unitSprite.AddFrame(textureStore.Get($"Map/{currentSkin.Value}/{drawnBuiilding.Textures[WeatherType.Clear]}-{i}"), drawnBuiilding.Frames[i]);
                }
                else
                    unitSprite.AddFrame(firstTexture);

                Show();
                terrainStarCounter.SetTo(building.BuildingTile.BaseDefence);
                buildingHPCounter.BindTo(building.CaptureHealth);
            }

            public void Reset()
            {
                Hide();
                terrainStarCounter.SetTo(0);
                buildingHPCounter.SetTo(0);
                boundToBuilding = null;
            }
        }

        private class UnitPopup : Container
        {
            private Container spriteContainer;
            private StatContainer unitHPCounter;
            private StatContainer unitAmmoCounter;
            private StatContainer unitFuelCounter;

            private DrawableUnit boundToUnit;

            [Resolved]
            private NearestNeighbourTextureStore textureStore { get; set; }

            public UnitPopup()
            {
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;
                AutoSizeAxes = Axes.Y;
                Width = 50;
                Masking = true;
                CornerRadius = 8;
                Children = new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(20, 20, 20, 150)
                    },
                    new FillFlowContainer()
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Top = 10, Bottom = 5 },
                        Children = new Drawable[]
                        {
                            spriteContainer = new Container()
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both
                            },
                            unitHPCounter = new StatContainer("UI/HP"),
                            unitAmmoCounter = new StatContainer("UI/Ammo"),
                            unitFuelCounter = new StatContainer("UI/Fuel"),
                        }
                    }
                };
            }

            public void BindTo(DrawableUnit unit)
            {
                if (unit == boundToUnit)
                    return;

                boundToUnit = unit;

                var unitSprite = new TextureAnimation()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                };
                spriteContainer.Child = unitSprite;

                textureStore.LoadIntoAnimation($"{unit.Country.UnitPath}/{unit.UnitData.IdleAnimation.Texture}", unitSprite, unit.UnitData.IdleAnimation.Frames, unit.UnitData.IdleAnimation.FrameOffset);
                unitSprite.Size *= 2;

                Show();
                unitHPCounter.BindTo(unit.HealthPoints);
                unitAmmoCounter.BindTo(unit.Ammo);
                unitFuelCounter.BindTo(unit.Fuel);
            }

            public void Reset()
            {
                Hide();
                unitHPCounter.SetTo(0);
                unitAmmoCounter.SetTo(0);
                unitFuelCounter.SetTo(0);
                boundToUnit = null;
            }
        }

        private class StatContainer : Container
        {
            private Sprite sprite;
            private string texturePath;

            private RollingCounter<int> rollingCounter;

            private IBindable<int> bindableReference;

            public StatContainer(string texturePath)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                this.texturePath = texturePath;

                Children = new Drawable[]
                {
                    new Container()
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(22),
                        Position = new Vector2(4, 0),
                        Child = sprite = new Sprite()
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        }
                    },
                    rollingCounter = new RollingCounter<int>()
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Position = new Vector2(-4, 0),
                        Font = new FontUsage("Roboto", weight: "Bold")
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(NearestNeighbourTextureStore textureStore)
            {
                sprite.Texture = textureStore.Get(texturePath);
                sprite.Size = sprite.Texture.Size * 2;
            }

            public void BindTo(Bindable<int> stat)
            {
                bindableReference = stat.GetBoundCopy();
                bindableReference.BindValueChanged(x => rollingCounter.Current.Value = x.NewValue, true);
            }

            public void SetTo(int stat)
            {
                bindableReference = null;
                rollingCounter.Current.Value = stat;
            }
        }
    }
}
