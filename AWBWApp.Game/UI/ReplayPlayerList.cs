using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.Game.COs;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.Helpers;
using AWBWApp.Game.UI.Replay;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Lists;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI
{
    public class ReplayPlayerList : Container
    {
        private FillFlowContainer fillContainer;

        private SortedList<DrawableReplayPlayer> drawablePlayers = new SortedList<DrawableReplayPlayer>();

        public ReplayPlayerList()
        {
            Masking = true;
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Colour = Color4.Black.Opacity(0.1f),
                Radius = 5
            };

            InternalChildren = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.3f
                },
                new BasicScrollContainer()
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopRight,
                    ScrollbarOverlapsContent = false,
                    Children = new Drawable[]
                    {
                        fillContainer = new FillFlowContainer()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            LayoutDuration = 450,
                            LayoutEasing = Easing.OutQuint
                        }
                    }
                }
            };
        }

        public void UpdateList(Dictionary<long, PlayerInfo> players)
        {
            Schedule(() =>
            {
                fillContainer.Clear();
                drawablePlayers.Clear();

                if (players.Count <= 0)
                    return;

                foreach (var player in players)
                {
                    var drawable = new DrawableReplayPlayer(player.Value);
                    drawablePlayers.Add(drawable);
                    fillContainer.Add(drawable);
                }

                SortList(drawablePlayers[0].PlayerID, 0);
                fillContainer.FinishTransforms(true);
            });
        }

        //Todo: Fix scheduling here.
        public void SortList(long playerID, int turnNumber) => Schedule(() => sortList(playerID, turnNumber));

        private void sortList(long playerID, int turnNumber)
        {
            if (drawablePlayers.Count <= 0)
                return;

            var list = new List<DrawableReplayPlayer>();
            var topPlayer = drawablePlayers.Find(x => x.PlayerID == playerID);

            //As this is only run once per turn. Use linQ to help keep this concise but readible.

            //First add all alive drawable players and list in order of round order
            list.AddRange(drawablePlayers.Where(x => !x.EliminatedOn.HasValue || x.EliminatedOn.Value > turnNumber).OrderBy(x => x.RoundOrder < topPlayer.RoundOrder ? 1 : 0));

            //Then add all eliminated players in order of when they were eliminated
            list.AddRange(drawablePlayers.Where(x => x.EliminatedOn.HasValue && x.EliminatedOn.Value <= turnNumber).OrderBy(x => -x.EliminatedOn));

            for (int i = 0; i < list.Count; i++)
            {
                var player = list[i];
                if (i == 0)
                    player.ResizeTo(new Vector2(1, player.Height), 200, Easing.In);
                else
                    player.ResizeTo(new Vector2(0.9f, player.Height), 200, Easing.In);

                fillContainer.SetLayoutPosition(player, i);
            }
        }

        public class DrawableReplayPlayer : Container, IComparable<DrawableReplayPlayer>
        {
            public long PlayerID;
            public int RoundOrder;
            public int? EliminatedOn;

            public string Team;
            private Sprite teamSprite;

            private Sprite unitValueCoin;
            private TableContainer tableContainer;

            private COData co;
            private Sprite coSprite;
            private PowerProgress coProgress;

            private COData tagCO;
            private Sprite tagCOSprite;
            private PowerProgress tagProgress;

            private Container normalPowerBackground;
            private Sprite normalPower;
            private Container superPowerBackground;
            private Sprite superPower;

            [Resolved]
            private NearestNeighbourTextureStore textureStore { get; set; }

            public DrawableReplayPlayer(PlayerInfo info)
            {
                PlayerID = info.ID;
                RoundOrder = info.RoundOrder;
                EliminatedOn = info.EliminatedOn;
                Team = info.Team;

                RelativeSizeAxes = Axes.X;
                Size = new Vector2(1, 80);
                Margin = new MarginPadding { Bottom = 2 };
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;

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
                    coProgress = new PowerProgress(info.ActiveCO.Value.PowerRequiredForNormal, info.ActiveCO.Value.PowerRequiredForSuper)
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
                info.ActiveCO.BindValueChanged(x => onCOChange(x, false), true);
                info.TagCO.BindValueChanged(x => onCOChange(x, true), true);
                info.ActivePower.BindValueChanged(x => onPowerActivationChange(x.NewValue), true);
            }

            private Drawable createNameAndTeamContainer(PlayerInfo info)
            {
                var container = new Container()
                {
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 20),

                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4Extensions.FromHex(info.Country.Value.Colours["playerList"]).Darken(0.1f), //Todo: Fix config values
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
                ReplayPlayerInfo unitValue;

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
                            coSprite = new Sprite
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
                                    new ReplayPlayerInfo("UI/Coin", info.Funds),
                                    new ReplayPlayerInfo("UI/BuildingsCaptured", info.PropertyValue)
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
                                    new ReplayPlayerInfo($"{info.Country.Value.Path}/Infantry-0", info.UnitCount),
                                    unitValue = new ReplayPlayerInfo($"{info.Country.Value.Path}/Infantry-0", info.UnitValue)
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
                        tagCOSprite = new Sprite
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
            private void load()
            {
                if (Team != null)
                    teamSprite.Texture = textureStore.Get($"UI/Team-{Team}");

                unitValueCoin.Texture = textureStore.Get("UI/Coin");

                normalPower.Texture = textureStore.Get("UI/NormalPower");
                normalPower.Size = normalPower.Texture.Size;

                superPower.Texture = textureStore.Get("UI/SuperPower");
                superPower.Size = superPower.Texture.Size;
            }

            public int CompareTo(DrawableReplayPlayer other)
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
                    }
                    else
                    {
                        coSprite.Texture = textureStore.Get($"CO/{coUpdated.NewValue.CO.Name}-Small");
                        co = coUpdated.NewValue.CO;
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

                    coSprite.ResizeTo(new Vector2(36), 400, Easing.Out);
                    tagCOSprite.ResizeTo(new Vector2(24), 400, Easing.Out);
                    coProgress.MoveTo(new Vector2(0, 62), 400, Easing.Out);
                    tagProgress.MoveTo(new Vector2(0, 76), 400, Easing.Out);
                    var content = tableContainer.Content;
                    var newContent = new[,] { { coSprite, tagCOSprite, content[0, 2], content[0, 3] } };
                    tableContainer.Content = newContent;
                }

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

            private class PowerProgress : Container, IHasCurrentValue<int>
            {
                private readonly BindableWithCurrent<int> current = new BindableWithCurrent<int>();

                public Bindable<int> Current
                {
                    get => current.Current;
                    set => current.Current = value;
                }

                private int displayedPower;

                public int DisplayedValue
                {
                    get => displayedPower;
                    set
                    {
                        if (displayedPower == value)
                            return;
                        displayedPower = value;
                        UpdatePower();
                    }
                }

                private List<PowerSegment> segments = new List<PowerSegment>();

                public int PowerRequiredForSuper => segments.Count * ProgressPerBar;
                public int PowerRequiredForNormal => smallBars * ProgressPerBar;

                public int ProgressPerBar = 90000;

                private readonly int smallBars;

                public PowerProgress(int? requiredNormal, int? requiredSuper)
                {
                    if (!requiredNormal.HasValue && !requiredSuper.HasValue)
                        throw new Exception("RequiredPowers cannot both be null.");

                    //AWBW actually makes both powers the same.
                    if (requiredNormal == requiredSuper)
                        requiredNormal = null;

                    smallBars = requiredNormal.HasValue ? requiredNormal.Value / ProgressPerBar : 0;
                    var largeBars = requiredSuper.HasValue ? (requiredSuper.Value / ProgressPerBar) - smallBars : 0;
                    var barWidth = 1f / (smallBars + largeBars);

                    for (int i = 0; i < smallBars; i++)
                    {
                        var child = new PowerSegment(false)
                        {
                            RelativePositionAxes = Axes.X,
                            Position = new Vector2(barWidth * segments.Count, 0),
                            Width = barWidth
                        };

                        Add(child);
                        segments.Add(child);
                    }

                    for (int i = 0; i < largeBars; i++)
                    {
                        var child = new PowerSegment(true)
                        {
                            RelativePositionAxes = Axes.X,
                            Position = new Vector2(barWidth * segments.Count, 0),
                            Width = barWidth
                        };

                        Add(child);
                        segments.Add(child);
                    }

                    Current.BindValueChanged(val => TransformPower(DisplayedValue, val.NewValue));
                }

                public void UpdatePower()
                {
                    var power = displayedPower;

                    var hasPower = power >= ProgressPerBar * smallBars;
                    var hasSuperPower = power >= ProgressPerBar * segments.Count;

                    foreach (var segment in segments)
                    {
                        var countForSegment = Math.Max(0, Math.Min(ProgressPerBar, power));

                        power -= countForSegment;
                        segment.SegmentProgress = (float)countForSegment / ProgressPerBar;

                        segment.Pulsating = segment.Super ? hasSuperPower : hasPower;
                    }
                }

                public void TransformPower(int currentValue, int newValue)
                {
                    this.TransformTo(nameof(DisplayedValue), newValue, 400, Easing.OutCubic);
                }

                private class PowerSegment : Container
                {
                    private float progress;

                    public float SegmentProgress
                    {
                        get => progress;
                        set
                        {
                            progress = value;
                            UpdateDisplay();
                        }
                    }

                    private bool pulsating;

                    public bool Pulsating
                    {
                        get => pulsating;
                        set
                        {
                            pulsating = value;
                            UpdatePulsating();
                        }
                    }

                    public bool Super { get; private set; }

                    private readonly Box fill;
                    private readonly Color4 notFilledColor = Color4Extensions.FromHex("059113");
                    private readonly Color4 filledColor = Color4Extensions.FromHex("0eaf1e").Lighten(0.25f);
                    private readonly Color4 filledPulsateColor = Color4Extensions.FromHex("17d129").LightenAndFade(0.6f);

                    public PowerSegment(bool super)
                    {
                        RelativeSizeAxes = Axes.Both;

                        Anchor = Anchor.CentreLeft;
                        Origin = Anchor.CentreLeft;

                        Super = super;

                        Height = super ? 1 : 0.7f;

                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black.Opacity(0.4f)
                            },
                            fill = new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = notFilledColor
                            },
                            new Container()
                            {
                                Masking = true,
                                RelativeSizeAxes = Axes.Both,
                                BorderColour = new Color4(200, 200, 200, 255),
                                BorderThickness = 3,
                                Child = new Box()
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0.1f,
                                    AlwaysPresent = true
                                }
                            },
                        };
                    }

                    protected override void LoadComplete()
                    {
                        base.LoadComplete();
                        UpdateDisplay();
                    }

                    protected void UpdateDisplay()
                    {
                        fill.Width = progress;

                        if (progress < 1)
                            pulsating = false;

                        FinishTransforms();
                        fill.FadeColour(progress >= 1 ? filledColor : notFilledColor, 200, Easing.OutQuint);
                    }

                    protected void UpdatePulsating()
                    {
                        if (!pulsating)
                        {
                            UpdateDisplay();
                            return;
                        }

                        fill.FadeColour(filledColor, 200, Easing.OutQuint);
                        fill.Delay(200).Loop(600, p => p.FadeColour(filledPulsateColor, 400, Easing.In).Then().FadeColour(filledColor, 600, Easing.In));
                    }
                }
            }

            private class ReplayPlayerInfo : Container
            {
                private Sprite infoIcon;
                private string infoIconTexture;
                //private ShrinkingSpriteText text;
                private RollingCounter<int> counter;

                public ReplayPlayerInfo(string icon, Bindable<int> count)
                {
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
                                    new ShrinkingCompositeDrawable(counter = new RollingCounter<int>()
                                    {
                                        Colour = Color4.Black,
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Padding = new MarginPadding { Left = 1, Right = 3 }
                                    })
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        ShrinkAxes = Axes.X
                                    }
                                }
                            }
                        }
                    };

                    counter.Current.BindTo(count);
                }

                [BackgroundDependencyLoader]
                private void Load(TextureStore storage)
                {
                    infoIcon.Texture = storage.Get(infoIconTexture);
                }
            }
        }
    }
}
