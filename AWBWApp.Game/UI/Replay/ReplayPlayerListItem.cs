using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Country;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Game.Units;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Components.Menu;
using AWBWApp.Game.UI.Components.Tooltip;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Replay
{
    public class ReplayPlayerListItem : Container, IComparable<ReplayPlayerListItem>, IHasContextMenu
    {
        public long PlayerID;
        public int RoundOrder;
        public int? EliminatedOn;

        public string Team;
        private Sprite teamSprite;

        private Sprite unitValueCoin;
        private TableContainer tableContainer;

        private COData co;
        private SpriteWithTooltip coSprite;
        private PowerProgress coProgress;

        private COData tagCO;
        private SpriteWithTooltip tagCOSprite;
        private PowerProgress tagProgress;

        private Container normalPowerBackground;
        private Sprite normalPower;
        private Container superPowerBackground;
        private Sprite superPower;

        private ReplayPlayerInfo playerMoney;
        private ReplayPlayerInfo playerIncome;
        private ReplayPlayerInfo unitCount;
        private ReplayPlayerInfo unitValue;

        private Box nameBox;

        [Resolved]
        private NearestNeighbourTextureStore textureStore { get; set; }

        [Resolved]
        private CountryStorage countryStorage { get; set; }

        private Bindable<FaceDirection> faceDirection;
        private Bindable<object> countryBindable;
        private Action<long> openPlayerStats;
        private Func<long, List<DrawableUnit>> getUnits;

        private bool usePercentagePowers;
        private float unitPriceMultiplier;
        private ReplayPlayerList playerList;
        private List<DrawableUnit> tooltipContent;

        public ReplayPlayerListItem(ReplayPlayerList playerList, PlayerInfo info, Action<long> openPlayerStats, bool usePercentagePowers, Func<long, List<DrawableUnit>> getUnits)
        {
            PlayerID = info.ID;
            RoundOrder = info.RoundOrder;
            EliminatedOn = info.EliminatedOn;
            Team = info.Team;
            unitPriceMultiplier = info.ActiveCO.Value.CO.DayToDayPower.UnitPriceMultiplier;

            this.openPlayerStats = openPlayerStats;
            this.usePercentagePowers = usePercentagePowers;
            this.playerList = playerList;
            this.getUnits = getUnits;

            faceDirection = info.UnitFaceDirection.GetBoundCopy();

            countryBindable = new Bindable<object>();
            info.Country.BindValueChanged(x => countryBindable.Value = x.NewValue.Code, true);
            countryBindable.BindValueChanged(x =>
            {
                info.Country.Value = countryStorage.GetCountryByCode((string)x.NewValue);

                nameBox!.Colour = Color4Extensions.FromHex(info.Country.Value.Colours["playerList"]).Darken(0.1f);
                unitCount!.UpdateIcon($"{info.Country.Value.UnitPath}/Infantry-0");
                unitValue!.UpdateIcon($"{info.Country.Value.UnitPath}/Infantry-0");
            });

            RelativeSizeAxes = Axes.X;
            Size = new Vector2(1, 80);
            Margin = new MarginPadding { Bottom = 2 };
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;

            int? requiredNormalPower;
            int? requiredSuperPower;

            if (usePercentagePowers)
            {
                requiredNormalPower = info.ActiveCO.Value.CO.NormalPower?.PowerStars * 90000;
                requiredSuperPower = info.ActiveCO.Value.CO.SuperPower?.PowerStars * 90000;
            }
            else
            {
                requiredNormalPower = info.ActiveCO.Value.PowerRequiredForNormal;
                requiredSuperPower = info.ActiveCO.Value.PowerRequiredForSuper;
            }

            InternalChildren = new Drawable[]
            {
                new Container()
                {
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 60),
                    Masking = true,
                    EdgeEffect = new EdgeEffectParameters()
                    {
                        Colour = Color4.Black.Opacity(0.8f),
                        Type = EdgeEffectType.Shadow,
                        Radius = 4
                    },
                    Children = new Drawable[]
                    {
                        createNameAndTeamContainer(info),
                        new Container()
                        {
                            RelativeSizeAxes = Axes.X,
                            Size = new Vector2(1, 40),
                            Position = new Vector2(0, 20),
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Box()
                                {
                                    RelativeSizeAxes = Axes.Both,
                                },
                                createInfoContainer(info),
                            }
                        },
                    }
                },
                coProgress = new PowerProgress(requiredNormalPower, requiredSuperPower)
                {
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 15f),
                    Position = new Vector2(0, 62)
                },
                normalPowerBackground = new Container()
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Position = new Vector2(0, 62),
                    Masking = true,
                    CornerRadius = 3,
                    Width = 0.6f,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4Extensions.FromHex("932213").Opacity(0.5f)
                        },
                        normalPower = new Sprite()
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                    }
                },
                superPowerBackground = new Container()
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Position = new Vector2(0, 62),
                    Masking = true,
                    CornerRadius = 3,
                    Width = 0.6f,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4Extensions.FromHex("191393").Opacity(0.5f)
                        },
                        superPower = new Sprite()
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                    }
                }
            };

            if (info.TagCO.Value.CO != null)
                adjustContentForTagCO(info);

            info.Eliminated.BindValueChanged(onEliminationChange, true);
            info.ActivePower.BindValueChanged(x => onPowerActivationChange(x.NewValue), true);

            info.ActiveCO.BindValueChanged(x => onCOChange(x, false), true);
            info.TagCO.BindValueChanged(x => onCOChange(x, true), true);

            if (usePercentagePowers)
                info.PowerPercentage.BindValueChanged(onPowerPercentageChange, true);
        }

        private Drawable createNameAndTeamContainer(PlayerInfo info)
        {
            var container = new Container()
            {
                RelativeSizeAxes = Axes.X,
                Size = new Vector2(1, 20),

                Children = new Drawable[]
                {
                    nameBox = new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4Extensions.FromHex(info.Country.Value.Colours["playerList"]).Darken(0.1f)
                    }
                }
            };

            var text = new SpriteText()
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Right = 5 },
                Text = info.Username,
                Truncate = true
            };

            if (Team == null)
            {
                container.Add(text);
                return container;
            }

            container.Add(new TableContainer
            {
                RelativeSizeAxes = Axes.Both,
                ShowHeaders = false,
                Columns = new[]
                {
                    new TableColumn(dimension: new Dimension(mode: GridSizeMode.Distributed)),
                    new TableColumn(dimension: new Dimension(mode: GridSizeMode.Absolute, size: 45), anchor: Anchor.CentreRight)
                },
                Content = new Drawable[1, 2]
                {
                    {
                        text,
                        teamSprite = new Sprite
                        {
                            Size = new Vector2(43, 19),
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight
                        }
                    }
                }
            });

            return container;
        }

        private Drawable createInfoContainer(PlayerInfo info)
        {
            tableContainer = new TableContainer()
            {
                RelativeSizeAxes = Axes.Both,
                ShowHeaders = false,
                Columns = new TableColumn[]
                {
                    new TableColumn(dimension: new Dimension(GridSizeMode.AutoSize)),
                    new TableColumn(dimension: new Dimension(GridSizeMode.AutoSize), anchor: Anchor.BottomCentre),
                    new TableColumn(dimension: new Dimension(GridSizeMode.Distributed)),
                    new TableColumn(dimension: new Dimension(GridSizeMode.Distributed))
                },
                Content = new Drawable[,]
                {
                    {
                        coSprite = new SpriteWithTooltip
                        {
                            FillMode = FillMode.Fit,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(36),
                            Position = new Vector2(2, 2)
                        },
                        null,
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                playerMoney = new ReplayPlayerInfo("UI/Coin", info.Funds, true),
                                playerIncome = new ReplayPlayerInfo("UI/BuildingsCaptured", info.PropertyValue, true)
                                {
                                    Position = new Vector2(0, 19)
                                }
                            }
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                unitCount = new ReplayPlayerInfoWithTooltip($"{info.Country.Value.UnitPath}/Infantry-0", info.UnitCount, () => getUnits(PlayerID), false, 1, true),
                                unitValue = new ReplayPlayerInfoWithTooltip($"{info.Country.Value.UnitPath}/Infantry-0", info.UnitValue, () => getUnits(PlayerID), true, unitPriceMultiplier, true)
                                {
                                    Position = new Vector2(0, 19),
                                }
                            }
                        },
                    },
                },
            };

            unitValue.Add(unitValueCoin = new Sprite
            {
                Position = new Vector2(10, 8),
                Size = new Vector2(10, 10)
            });

            return tableContainer;
        }

        private void adjustContentForTagCO(PlayerInfo info)
        {
            var content = tableContainer.Content;
            var newContent = new[,]
            {
                {
                    content[0, 0],
                    tagCOSprite = new SpriteWithTooltip
                    {
                        FillMode = FillMode.Fit,
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Size = new Vector2(24),
                        Position = new Vector2(2, -2)
                    },
                    content[0, 2],
                    content[0, 3],
                }
            };

            tableContainer.Content = newContent;

            coProgress.Size = new Vector2(1, 14f);
            Height += 10f;

            Add(tagProgress = new PowerProgress(info.TagCO.Value.PowerRequiredForNormal, info.TagCO.Value.PowerRequiredForSuper)
            {
                RelativeSizeAxes = Axes.X,
                Size = new Vector2(1, 14),
                Position = new Vector2(0, 76)
            });
        }

        [BackgroundDependencyLoader]
        private void load(AWBWConfigManager config)
        {
            if (Team != null)
                teamSprite.Texture = textureStore.Get($"UI/Team-{Team}");

            unitValueCoin.Texture = textureStore.Get("UI/Coin");

            normalPower.Texture = textureStore.Get("UI/NormalPower");
            normalPower.Size = normalPower.Texture.Size;

            superPower.Texture = textureStore.Get("UI/SuperPower");
            superPower.Size = superPower.Texture.Size;
        }

        public void SetShowHiddenInformation(bool show)
        {
            playerMoney.SetShowHiddenInformation(show);
            playerIncome.SetShowHiddenInformation(show);
            unitCount.SetShowHiddenInformation(show);
            unitValue.SetShowHiddenInformation(show);
        }

        public int CompareTo(ReplayPlayerListItem other)
        {
            return RoundOrder.CompareTo(other.RoundOrder);
        }

        private void onEliminationChange(ValueChangedEvent<bool> eliminated)
        {
            if (eliminated.NewValue)
                this.FadeTo(0.6f, 100, Easing.In);
            else
                this.FadeTo(1f, 100, Easing.In);
        }

        private void onPowerActivationChange(ActiveCOPower power)
        {
            coProgress.FadeTo(power == ActiveCOPower.None ? 1 : 0, 400);
            tagProgress?.FadeTo(power == ActiveCOPower.None ? 1 : 0, 400);

            if (power == ActiveCOPower.Super)
            {
                superPowerBackground.FadeTo(1, 400, Easing.OutQuint);
                superPower.Loop(2400, p => p.ScaleTo(new Vector2(1.5f, 1), 1000, Easing.InOutSine).Then(200).ScaleTo(1, 1000, Easing.InOutSine));

                normalPowerBackground.FadeTo(0, 400, Easing.OutQuint);
                normalPower.ScaleTo(1);
            }
            else if (power == ActiveCOPower.Normal)
            {
                normalPowerBackground.FadeTo(1, 400, Easing.OutQuint);
                normalPower.Loop(2400, p => p.ScaleTo(new Vector2(1.5f, 1), 1000, Easing.InOutSine).Then(200).ScaleTo(1, 1000, Easing.InOutSine));

                superPowerBackground.FadeTo(0, 400, Easing.OutQuint);
                superPower.ScaleTo(1);
            }
            else
            {
                superPowerBackground.FadeTo(0, 400, Easing.OutQuint);
                superPower.ScaleTo(1);

                normalPowerBackground.FadeTo(0, 400, Easing.OutQuint);
                normalPower.ScaleTo(1);
            }
        }

        private void onCOChange(ValueChangedEvent<COInfo> coUpdated, bool wasTagCO)
        {
            if (LoadState != LoadState.Loaded)
            {
                Schedule(() => onCOChange(coUpdated, wasTagCO));
                return;
            }

            if (wasTagCO && coUpdated.NewValue.CO == null)
                return;

            if ((wasTagCO ? tagCO : co) == null)
            {
                if (wasTagCO)
                {
                    tagCOSprite.Texture = textureStore.Get($"CO/{coUpdated.NewValue.CO.Name}-Small");
                    tagCO = coUpdated.NewValue.CO;
                    tagCOSprite.Tooltip = tagCO.Tooltip;
                }
                else
                {
                    coSprite.Texture = textureStore.Get($"CO/{coUpdated.NewValue.CO.Name}-Small");
                    co = coUpdated.NewValue.CO;
                    coSprite.Tooltip = co.Tooltip;
                }
            }

            //See if we need to swap active and tag co's
            if ((wasTagCO ? tagCO : co) != coUpdated.NewValue.CO)
            {
                var other = wasTagCO ? co : tagCO;
                if (other != coUpdated.NewValue.CO)
                    throw new Exception("Player managed to change CO's during a match without tagging?");

                (co, tagCO) = (tagCO, co);
                (coSprite, tagCOSprite) = (tagCOSprite, coSprite);
                (coProgress, tagProgress) = (tagProgress, coProgress);

                coSprite.Tooltip = co.Tooltip;
                tagCOSprite.Tooltip = tagCO.Tooltip;

                coSprite.ResizeTo(new Vector2(36), 400, Easing.Out);
                tagCOSprite.ResizeTo(new Vector2(24), 400, Easing.Out);
                coProgress.MoveTo(new Vector2(0, 62), 400, Easing.Out);
                tagProgress.MoveTo(new Vector2(0, 76), 400, Easing.Out);
                var content = tableContainer.Content;
                var newContent = new[,] { { coSprite, tagCOSprite, content[0, 2], content[0, 3] } };
                tableContainer.Content = newContent;
            }

            if (usePercentagePowers)
                return;

            var progressBar = wasTagCO ? tagProgress : coProgress;

            var requiredNormalPower = coUpdated.NewValue.PowerRequiredForNormal ?? 0;
            var requiredSuperPower = coUpdated.NewValue.PowerRequiredForSuper ?? requiredNormalPower;

            //For Von Bolt who has no normal power. AWBW sets Normal Power = Super Power, but then never updates it.
            if (progressBar.PowerRequiredForNormal == 0)
                requiredNormalPower = 0;

            var normalPowerChanged = requiredNormalPower != progressBar.PowerRequiredForNormal;
            var superPowerChanged = requiredSuperPower != progressBar.PowerRequiredForSuper;

            if (normalPowerChanged || superPowerChanged)
            {
                if (!((normalPowerChanged || progressBar.PowerRequiredForNormal == 0) && superPowerChanged))
                    throw new Exception("Only one of the two required powers changed?");

                var superSegments = progressBar.PowerRequiredForSuper / progressBar.ProgressPerBar;
                progressBar.ProgressPerBar = requiredSuperPower / superSegments;

                if (progressBar.PowerRequiredForSuper != requiredSuperPower || progressBar.PowerRequiredForNormal != requiredNormalPower)
                    throw new Exception("Failed to update powers correctly");
            }

            progressBar.Current.Value = coUpdated.NewValue.Power ?? 0;
        }

        private void onPowerPercentageChange(ValueChangedEvent<double> powerPercentage)
        {
            coProgress.Current.Value = (int)Math.Round(powerPercentage.NewValue * coProgress.PowerRequiredForSuper);
        }

        public MenuItem[] ContextMenuItems => createContextMenuItems();

        private MenuItem[] createContextMenuItems()
        {
            var countryMenuItem = new MenuItem("Country")
            {
                Items = countryStorage.GetAllCountryIDs().Select(x =>
                {
                    var country = countryStorage.GetCountryByAWBWID(x);
                    return new StatefulMenuItem(country.Name, countryBindable, country.Code);
                }).ToArray()
            };

            var items = new List<MenuItem>()
            {
                new MenuItem("Open Stats", () => openPlayerStats?.Invoke(PlayerID)),
                new EnumMenuItem<FaceDirection>("Unit Face Direction", faceDirection),
                countryMenuItem
            };

            if (playerList != null)
                items.AddRange(playerList.ContextMenuItems);

            return items.ToArray();
        }

        private class ReplayPlayerInfo : Container
        {
            private Sprite infoIcon;
            private string infoIconTexture;
            //private ShrinkingSpriteText text;
            private RollingCounter<int> counterText;

            private ShrinkingCompositeDrawable informationDrawable;
            private SpriteText hiddenInformationText;

            private bool hideInFog;

            [Resolved]
            private NearestNeighbourTextureStore textureStore { get; set; }

            public ReplayPlayerInfo(string icon, Bindable<int> count, bool hideInFog)
            {
                this.hideInFog = hideInFog;

                infoIconTexture = icon;
                RelativeSizeAxes = Axes.X;
                Height = 18;
                CornerRadius = 3;
                Masking = true;
                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(236, 236, 236, 255)
                    },
                    new TableContainer()
                    {
                        RelativeSizeAxes = Axes.Both,
                        ShowHeaders = false,
                        Columns = new[]
                        {
                            new TableColumn(dimension: new Dimension(mode: GridSizeMode.AutoSize)),
                            new TableColumn(anchor: Anchor.CentreRight)
                        },
                        Content = new Drawable[,]
                        {
                            {
                                infoIcon = new Sprite()
                                {
                                    Size = new Vector2(18),
                                    Position = new Vector2(1),
                                    FillMode = FillMode.Fit
                                },
                                new Container()
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        informationDrawable = new ShrinkingCompositeDrawable(counterText = new RollingCounter<int>()
                                        {
                                            Colour = Color4.Black,
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            Padding = new MarginPadding { Left = 1, Right = 3 }
                                        })
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            ShrinkAxes = Axes.X
                                        },
                                        hiddenInformationText = new SpriteText()
                                        {
                                            Colour = Color4.Black,
                                            Anchor = Anchor.CentreRight,
                                            Origin = Anchor.CentreRight,
                                            Padding = new MarginPadding { Left = 1, Right = 3 },
                                            Text = "???"
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                counterText.Current.BindTo(count);
            }

            public void SetShowHiddenInformation(bool visible)
            {
                if (visible || !hideInFog)
                {
                    informationDrawable.Show();
                    hiddenInformationText.Hide();
                }
                else
                {
                    informationDrawable.Hide();
                    hiddenInformationText.Show();
                }
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                UpdateIcon(infoIconTexture);
            }

            public void UpdateIcon(string path)
            {
                infoIcon.Texture = textureStore.Get(path);
            }
        }

        private class ReplayPlayerInfoWithTooltip : ReplayPlayerInfo, IHasCustomTooltip<UnitMouseoverTooltip.UnitMouseOverInfo>
        {
            private Func<List<DrawableUnit>> getUnits;
            private bool value;
            private float unitPriceMultiplier;

            public ReplayPlayerInfoWithTooltip(string icon, Bindable<int> count, Func<List<DrawableUnit>> getUnits, bool value, float unitPriceMultiplier, bool hideInFog)
                : base(icon, count, hideInFog)
            {
                this.value = value;
                this.unitPriceMultiplier = unitPriceMultiplier;

                this.getUnits = getUnits;
            }

            public ITooltip<UnitMouseoverTooltip.UnitMouseOverInfo> GetCustomTooltip() => new UnitMouseoverTooltip();

            public UnitMouseoverTooltip.UnitMouseOverInfo TooltipContent =>
                new UnitMouseoverTooltip.UnitMouseOverInfo
                {
                    Units = getUnits(),
                    ShowValue = value,
                    ValueMultiplier = unitPriceMultiplier
                };
        }
    }
}
