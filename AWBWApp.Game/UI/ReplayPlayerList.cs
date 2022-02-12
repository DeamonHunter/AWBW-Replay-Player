using System;
using System.Collections.Generic;
using System.Linq;
using AWBWApp.Game.API.Replay;
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

        public void UpdateList(Dictionary<int, PlayerInfo> players)
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

        public void SortList(long playerID, int turnNumber)
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

            private string coName;
            private Sprite coSprite;
            private PowerProgress coProgress;

            private string tagCOName;
            private Sprite tagCOSprite;
            private PowerProgress tagProgress;

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

                Masking = true;
                EdgeEffect = new EdgeEffectParameters()
                {
                    Colour = Color4.Black,
                    Type = EdgeEffectType.Shadow,
                    Radius = 2
                };

                InternalChildren = new Drawable[]
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
                    coProgress = new PowerProgress(info.ActiveCO.Value.PowerRequiredForNormal, info.ActiveCO.Value.PowerRequiredForSuper)
                    {
                        RelativeSizeAxes = Axes.X,
                        Size = new Vector2(1, 19f),
                        Position = new Vector2(0, 61)
                    }
                };

                if (info.TagCO.Value.Name != null)
                    adjustContentForTagCO(info);

                info.Eliminated.BindValueChanged(OnEliminationChange, true);
                info.ActiveCO.BindValueChanged(x => OnCOChange(x, false), true);
                info.TagCO.BindValueChanged(x => OnCOChange(x, true), true);
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
                            Colour = AWBWReplayPlayer.CountryColour(info.CountryID.Value).Darken(0.1f),
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
                                    new ReplayPlayerInfo($"Team/{info.CountryPath.Value}/Infantry-0", info.UnitCount),
                                    unitValue = new ReplayPlayerInfo($"Team/{info.CountryPath.Value}/Infantry-0", info.UnitValue)
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
            }

            private void OnEliminationChange(ValueChangedEvent<bool> eliminated)
            {
                if (eliminated.NewValue)
                    this.FadeTo(0.6f, 100, Easing.In);
                else
                    this.FadeTo(1f, 100, Easing.In);
            }

            public int CompareTo(DrawableReplayPlayer other)
            {
                return RoundOrder.CompareTo(other.RoundOrder);
            }

            private void OnCOChange(ValueChangedEvent<COInfo> coUpdated, bool wasTagCO)
            {
                if (LoadState != LoadState.Loaded)
                {
                    Schedule(() => OnCOChange(coUpdated, wasTagCO));
                    return;
                }

                if (wasTagCO && !coUpdated.NewValue.ID.HasValue)
                    return;

                if ((wasTagCO ? tagCOName : coName) == null)
                {
                    if (wasTagCO)
                    {
                        tagCOSprite.Texture = textureStore.Get($"CO/{coUpdated.NewValue.Name}-Small");
                        tagCOName = coUpdated.NewValue.Name;
                    }
                    else
                    {
                        coSprite.Texture = textureStore.Get($"CO/{coUpdated.NewValue.Name}-Small");
                        coName = coUpdated.NewValue.Name;
                    }
                }

                //See if we need to swap active and tag co's
                if ((wasTagCO ? tagCOName : coName) != coUpdated.NewValue.Name)
                {
                    var other = wasTagCO ? coName : tagCOName;
                    if (other != coUpdated.NewValue.Name)
                        throw new Exception("Player managed to change CO's during a match without tagging?");

                    (coName, tagCOName) = (tagCOName, coName);
                    (coSprite, tagCOSprite) = (tagCOSprite, coSprite);
                    (coProgress, tagProgress) = (tagProgress, coProgress);

                    coSprite.ResizeTo(new Vector2(36), 400, Easing.Out);
                    tagCOSprite.ResizeTo(new Vector2(24), 400, Easing.Out);
                    coProgress.MoveTo(new Vector2(0, 61), 400, Easing.Out);
                    tagProgress.MoveTo(new Vector2(0, 76), 400, Easing.Out);
                    var content = tableContainer.Content;
                    var newContent = new[,] { { coSprite, tagCOSprite, content[0, 2], content[0, 3] } };
                    tableContainer.Content = newContent;
                }

                var progressBar = wasTagCO ? tagProgress : coProgress;

                var requiredNormalPower = coUpdated.NewValue.PowerRequiredForNormal ?? 0;
                var requiredSuperPower = coUpdated.NewValue.PowerRequiredForSuper ?? requiredNormalPower;

                var normalPowerChanged = requiredNormalPower != progressBar.PowerRequiredForNormal;
                var superPowerChanged = requiredSuperPower != progressBar.PowerRequiredForSuper;

                if (normalPowerChanged || superPowerChanged)
                {
                    if (!(normalPowerChanged && superPowerChanged))
                        throw new Exception("Only one of the two required powers changed?");

                    var superSegments = progressBar.PowerRequiredForSuper / progressBar.ProgressPerBar;
                    progressBar.ProgressPerBar = requiredSuperPower / superSegments;

                    if (progressBar.PowerRequiredForSuper != requiredSuperPower || progressBar.PowerRequiredForNormal != requiredNormalPower)
                        throw new Exception("Failed to update powers correctly");
                }

                progressBar.UpdatePower(coUpdated.NewValue.Power ?? 0);
            }

            private class PowerProgress : Container
            {
                private List<PowerSegment> segments = new List<PowerSegment>();

                public int PowerRequiredForSuper => segments.Count * progressPerBar;
                public int PowerRequiredForNormal => smallBars * progressPerBar;

                public int ProgressPerBar
                {
                    get => progressPerBar;
                    set
                    {
                        progressPerBar = value;
                        UpdateProgressPerBar();
                    }
                }

                private int progressPerBar = 90000;

                private readonly int smallBars;

                public PowerProgress(int? requiredNormal, int? requiredSuper)
                {
                    if (!requiredNormal.HasValue && !requiredSuper.HasValue)
                        throw new Exception("RequiredPowers cannot both be null.");

                    smallBars = requiredNormal.HasValue ? requiredNormal.Value / progressPerBar : 0;
                    var largeBars = requiredSuper.HasValue ? (requiredSuper.Value / progressPerBar) - smallBars : 0;
                    var barWidth = 1f / (smallBars + largeBars);

                    for (int i = 0; i < smallBars; i++)
                    {
                        var child = new PowerSegment(false)
                        {
                            Max = progressPerBar,
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
                            Max = progressPerBar,
                            RelativePositionAxes = Axes.X,
                            Position = new Vector2(barWidth * segments.Count, 0),
                            Width = barWidth
                        };

                        Add(child);
                        segments.Add(child);
                    }
                }

                protected void UpdateProgressPerBar()
                {
                    foreach (var segment in segments)
                        segment.Max = progressPerBar;
                }

                public void UpdatePower(int newPower)
                {
                    foreach (var segment in segments)
                    {
                        var countForSegment = Math.Max(0, Math.Min(progressPerBar, newPower));

                        newPower -= countForSegment;
                        segment.Current.Value = countForSegment;
                    }
                }

                private class PowerSegment : Container, IHasCurrentValue<int>
                {
                    public Bindable<int> Current
                    {
                        get => current.Current;
                        set => current.Current = value;
                    }

                    public int DisplayedValue
                    {
                        get => displayedValue;
                        set
                        {
                            if (displayedValue == value)
                                return;
                            displayedValue = value;
                            UpdateDisplay();
                        }
                    }

                    private int displayedValue;

                    public int Max
                    {
                        get => max;
                        set
                        {
                            max = value;
                            UpdateDisplay();
                        }
                    }

                    private int max;

                    private readonly BindableWithCurrent<int> current = new BindableWithCurrent<int>();

                    private readonly Box background;
                    private readonly Box fill;

                    public PowerSegment(bool super)
                    {
                        RelativeSizeAxes = Axes.Both;

                        Anchor = Anchor.CentreLeft;
                        Origin = Anchor.CentreLeft;

                        Height = super ? 1 : 0.5f;

                        Children = new Drawable[]
                        {
                            fill = new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Green
                            },
                            new Container()
                            {
                                Masking = true,
                                RelativeSizeAxes = Axes.Both,
                                BorderColour = Color4.White,
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

                        Current.BindValueChanged(val => TransformBar(DisplayedValue, val.NewValue));
                    }

                    protected void UpdateDisplay()
                    {
                        fill.Width = (float)DisplayedValue / Max;
                    }

                    protected void TransformBar(int currentValue, int newValue)
                    {
                        this.TransformTo(nameof(DisplayedValue), newValue, 400, Easing.OutQuint);
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
