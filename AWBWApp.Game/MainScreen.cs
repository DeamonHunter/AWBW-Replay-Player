using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.UI;
using AWBWApp.Game.UI.Components;
using AWBWApp.Game.UI.Interrupts;
using AWBWApp.Game.UI.Replay;
using AWBWApp.Game.UI.Select;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game
{
    public partial class MainScreen : EscapeableScreen
    {
        private Screen replayScreen;

        private AWBWLogo logo;

        private FillFlowContainer buttonsContainer;
        private HintBox hintBox;

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private InterruptDialogueOverlay interruptOverlay { get; set; }

        private MovingGrid grid;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = new Color4(232, 209, 153, 255),
                    RelativeSizeAxes = Axes.Both,
                },
                grid = new MovingGrid()
                {
                    GridColor = new Color4(100, 100, 100, 255),
                    RelativeSizeAxes = Axes.Both,
                    Spacing = new Vector2(30),
                    Velocity = new Vector2(11, 9)
                },
                logo = new AWBWLogo()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Position = new Vector2(0, -50)
                },
                buttonsContainer = new FillFlowContainer()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopCentre,
                    Position = new Vector2(0, 50),
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Y,
                    Spacing = new Vector2(0, 10),
                    Width = 400,
                    Children = new Drawable[]
                    {
                        new MainMenuButton(false)
                        {
                            Text = "Select A Replay",
                            Action = GoToReplaySelect
                        },
                        new MainMenuButton(false)
                        {
                            Text = "Import A Replay",
                            Action = openGetNewReplayInterrupt
                        }
                    }
                },
                hintBox = new HintBox()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopCentre,
                    Position = new Vector2(0, 220),
                    Width = 400,
                }
            };

            if (host.CanExit)
            {
                buttonsContainer.Add(new MainMenuButton(true)
                {
                    Text = "Exit",
                    Action = () => this.Exit()
                });
            }

            preLoadReplaySelect();
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (buttonsContainer.IsPresent)
                return base.OnClick(e);

            logo.MoveTo(new Vector2(0, -150), 250, Easing.OutQuint);

            buttonsContainer.FadeIn();
            hintBox.FadeIn();

            foreach (var child in buttonsContainer.Children)
                child.ScaleTo(new Vector2(0, 0.75f)).ScaleTo(1, 300, Easing.OutQuint);

            hintBox.ScaleTo(new Vector2(0, 0.75f)).ScaleTo(1, 300, Easing.OutQuint);

            return true;
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            buttonsContainer.FadeOut();
            hintBox.FadeOut();
            this.FadeIn();
            logo.Animate();
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            preLoadReplaySelect();
        }

        private void preLoadReplaySelect()
        {
            if (replayScreen == null)
                LoadComponentAsync(replayScreen = new ReplaySelectScreen());
        }

        private Screen consumeReplaySelect(ReplayInfo info = null)
        {
            var rs = (ReplaySelectScreen)replayScreen;
            replayScreen = null;

            rs.SetGridOffset(grid.GridOffset);

            if (info != null)
                rs.SelectReplay(info);

            return rs;
        }

        private void openGetNewReplayInterrupt()
        {
            if (interruptOverlay.CurrentInterrupt != null)
                return;

            var taskCompletion = new TaskCompletionSource<ReplayInfo>();

            interruptOverlay.Push(new GetNewReplayInterrupt(taskCompletion));
            Task.Run(async () =>
            {
                ReplayInfo info;

                try
                {
                    info = await taskCompletion.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                ScheduleAfterChildren(() => this.Push(consumeReplaySelect(info)));
            });
        }

        public void GoToReplaySelect() => ScheduleAfterChildren(() => this.Push(consumeReplaySelect()));

        private partial class MainMenuButton : BasicButton
        {
            public MainMenuButton(bool exit)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                RelativeSizeAxes = Axes.X;
                Size = new Vector2(1, 40);

                BackgroundColour = exit ? new Color4(140, 42, 42, 255) : new Color4(42, 91, 139, 255);
                HoverColour = BackgroundColour.Lighten(0.1f);

                SpriteText.Colour = Color4.White;
                Text = "Select A Replay";

                Masking = true;
                CornerRadius = 5;
                BorderColour = Color4.Black;
                BorderThickness = 3;
            }
        }

        private partial class AWBWLogo : Container
        {
            private Wrench wrench;
            private FillFlowContainer<Container> logoText;
            private Container subtitle;

            public AWBWLogo()
            {
                AutoSizeAxes = Axes.Both;
                Children = new Drawable[]
                {
                    wrench = new Wrench(new Color4(53, 86, 218, 255), new Color4(26, 29, 203, 255), new Color4(77, 114, 221, 255))
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    },
                    logoText = new FillFlowContainer<Container>()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Shear = new Vector2(0.2f, 0f)
                    },
                    subtitle = new Container()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        BorderThickness = 5,
                        CornerRadius = 10,
                        BorderColour = new Color4(26, 35, 44, 255),
                        Size = new Vector2(350, 55),
                        Position = new Vector2(0, 130),
                        Children = new Drawable[]
                        {
                            new Box()
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = new Color4(42, 91, 139, 255)
                            },
                            new SpriteText()
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Colour = Color4.White,
                                Shadow = true,
                                Font = FontUsage.Default.With(size: 48),
                                Text = "Replay Viewer"
                            }
                        }
                    }
                };

                foreach (var character in "AWBW")
                {
                    logoText.Add(new Container()
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(67, 170),
                        Child = new TextureSpriteText("UI/Power")
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = character.ToString(),
                            Font = FontUsage.Default.With(size: 2.25f),
                        }
                    });
                }
            }

            public void Animate()
            {
                using (BeginDelayedSequence(0))
                {
                    wrench.ScaleTo(0).ScaleTo(new Vector2(1, 1), 500, Easing.Out);
                    wrench.RotateTo(540, 500, Easing.OutCubic);
                    wrench.MoveTo(new Vector2(600, -600)).MoveTo(new Vector2(0), 400, Easing.OutCirc);
                    subtitle.ScaleTo(new Vector2(0.8f, 0));

                    foreach (var child in logoText.Children)
                        child.Child.FadeTo(0);

                    using (BeginDelayedSequence(600))
                    {
                        logoText.FadeIn(250);

                        var index = 0;

                        foreach (var child in logoText.Children)
                        {
                            child.Child.Delay(index * 150).FadeTo(1, 250);
                            child.Child.MoveToX(250).Delay(index * 150).MoveToX(0, 400, Easing.OutBounce);
                            index++;
                        }

                        using (BeginDelayedSequence(550))
                        {
                            subtitle.ScaleTo(1, 250, Easing.OutSine);
                        }
                    }
                }
            }

            private partial class Wrench : Container
            {
                public Wrench(ColourInfo baseColor, ColourInfo outLineColour, ColourInfo highlightColour)
                {
                    AutoSizeAxes = Axes.Both;
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            Colour = outLineColour,
                            Size = new Vector2(290, 110),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre
                        },
                        new SemiCirclePiece(false)
                        {
                            CircleColour = outLineColour,
                            Size = new Vector2(160, 180),
                            Position = new Vector2(-140, -20)
                        },
                        new SemiCirclePiece(false)
                        {
                            CircleColour = baseColor,
                            Size = new Vector2(140),
                            Position = new Vector2(-140, -30)
                        },
                        new SemiCirclePiece(true)
                        {
                            CircleColour = outLineColour,
                            Size = new Vector2(160, 180),
                            Position = new Vector2(-140, 20)
                        },
                        new SemiCirclePiece(true)
                        {
                            CircleColour = baseColor,
                            Size = new Vector2(140),
                            Position = new Vector2(-140, 30)
                        },
                        new SemiCirclePiece(false)
                        {
                            CircleColour = outLineColour,
                            Size = new Vector2(160, 180),
                            Position = new Vector2(140, -20)
                        },
                        new SemiCirclePiece(false)
                        {
                            CircleColour = baseColor,
                            Size = new Vector2(140),
                            Position = new Vector2(140, -30)
                        },
                        new SemiCirclePiece(true)
                        {
                            CircleColour = outLineColour,
                            Size = new Vector2(160, 180),
                            Position = new Vector2(140, 20)
                        },
                        new SemiCirclePiece(true)
                        {
                            CircleColour = baseColor,
                            Size = new Vector2(140),
                            Position = new Vector2(140, 30)
                        },
                        new Box()
                        {
                            Colour = new Color4(53, 86, 218, 255),
                            Size = new Vector2(270, 90),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre
                        },
                        new SemiCirclePiece(false)
                        {
                            CircleColour = highlightColour,
                            Size = new Vector2(100),
                            Position = new Vector2(-140, -40)
                        },
                        new SemiCirclePiece(true)
                        {
                            CircleColour = highlightColour,
                            Size = new Vector2(100),
                            Position = new Vector2(-140, 40)
                        },
                        new SemiCirclePiece(false)
                        {
                            CircleColour = highlightColour,
                            Size = new Vector2(100),
                            Position = new Vector2(140, -40)
                        },
                        new SemiCirclePiece(true)
                        {
                            CircleColour = highlightColour,
                            Size = new Vector2(100),
                            Position = new Vector2(140, 40)
                        },
                    };
                }
            }

            private partial class SemiCirclePiece : Container
            {
                public ColourInfo CircleColour
                {
                    get => Child.Colour;
                    set => Child.Colour = value;
                }

                public SemiCirclePiece(bool flipped)
                {
                    Masking = true;
                    Anchor = Anchor.Centre;
                    Origin = flipped ? Anchor.TopCentre : Anchor.BottomCentre;

                    Child = new Circle()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = flipped ? Anchor.TopCentre : Anchor.BottomCentre,
                        Origin = Anchor.Centre
                    };
                }
            }
        }
    }
}
