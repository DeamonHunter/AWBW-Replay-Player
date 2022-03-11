using System;
using System.Threading;
using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.IO;
using AWBWApp.Game.UI.Components;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class ReplaySelectScreen : EscapeableScreen
    {
        protected ReplayCarousel Carousel { get; private set; }
        protected ReplayInfoWedge ReplayInfo { get; private set; }

        private Container carouselContainer;

        private ReplayController replayController;

        [Resolved]
        private ReplayManager replayManager { get; set; }

        [Resolved]
        private MapFileStorage mapStorage { get; set; }

        private Box background;
        private MovingGrid grid;
        private Container noReplaysContainer;

        private static Color4 backgroundColor = new Color4(232, 209, 153, 255);

        [BackgroundDependencyLoader]
        private void load()
        {
            LoadComponentAsync(Carousel = new ReplayCarousel()
            {
                AllowSelection = false,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Both,
                SelectionChanged = updateSelected,
                ReplaysChanged = carouselReplaysLoaded,
            }, c => carouselContainer.Child = c);

            AddRangeInternal(new Drawable[]
            {
                background = new Box
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
                new ResetScrollContainer(() => Carousel.ScrollToSelected())
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 250
                },
                noReplaysContainer = new Container()
                {
                    Size = new Vector2(500, 300),
                    Masking = true,
                    CornerRadius = 15,
                    Alpha = 0,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box()
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black.Opacity(0.5f)
                        },
                        createMissingReplaysContainer()
                    }
                },
                carouselContainer = new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Size = new Vector2(0.7f, 1),
                    Child = new LoadingSpinner(true) { State = { Value = Visibility.Visible } }
                },
                ReplayInfo = new ReplayInfoWedge()
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.3f, 1)
                },
            });
        }

        private TextFlowContainer createMissingReplaysContainer()
        {
            var textFlow = new TextFlowContainer()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                TextAnchor = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(0, 5)
            };

            textFlow.AddText("No Replays have been added.\n\n\n", text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 36));
            textFlow.AddText("You can add more replays by doing the following:\n\n", text => text.Font = new FontUsage("Roboto", weight: "Bold", size: 24));

            textFlow.AddText("Select \"Import a Replay\" and follow the prompts.", text => text.Font = new FontUsage("Roboto", size: 24));
            textFlow.AddText("\nOR\n");
            textFlow.AddText("Dragging a replay on top of this player.", text => text.Font = new FontUsage("Roboto", size: 24));

            return textFlow;
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(this);
            return dependencies;
        }

        public void SetGridOffset(Vector2 offset) => grid.GridOffset = offset;

        public override void OnEntering(IScreen last)
        {
            base.OnEntering(last);

            background.FadeColour(backgroundColor).FadeColour(backgroundColor.Darken(0.2f), 500, Easing.In);
        }

        public void SelectReplay(ReplayInfo replayInfo)
        {
            Carousel.Select(replayInfo);
        }

        public void FinaliseSelection(ReplayInfo replayInfo = null)
        {
            if (!Carousel.ReplaysLoaded)
                return;

            if (Carousel.SelectedReplayData == null) return;

            if (OnStart())
                Carousel.AllowSelection = false;
        }

        private CancellationTokenSource tokenSource;
        private CancellationToken cancellationToken;

        protected virtual bool OnStart()
        {
            if (replayController != null)
                return false;

            tokenSource = new CancellationTokenSource();
            cancellationToken = tokenSource.Token;
            LoadComponentAsync(replayController = new ReplayController(), _ =>
            {
                this.Push(replayController);

                Task.Run(async () =>
                {
                    var data = await replayManager.GetReplayData(Carousel.SelectedReplayData);
                    var terrainFile = await mapStorage.GetOrDownloadMap(data.ReplayInfo.MapId);
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    replayController.LoadReplay(data, terrainFile);
                }, cancellationToken);
            }, cancellationToken);

            return true;
        }

        public override void OnResuming(IScreen last)
        {
            base.OnResuming(last);

            if (replayController == null || !replayController.IsLoaded)
                tokenSource.Cancel();
            replayController = null;
            Carousel.AllowSelection = true;
        }

        private void updateSelected(ReplayInfo updatedReplay)
        {
            ReplayInfo.Replay = updatedReplay;

            if (updatedReplay == null)
                Schedule(() => noReplaysContainer.FadeIn(400, Easing.In));
            else
                Schedule(() => noReplaysContainer.FadeOut(400, Easing.In));
        }

        private void carouselReplaysLoaded()
        {
            Carousel.AllowSelection = true;

            if (Carousel.SelectedReplayData != null)
            {
                noReplaysContainer.FadeOut(400, Easing.Out);
                return;
            }

            noReplaysContainer.FadeIn(400, Easing.In);
        }

        private class ResetScrollContainer : Container
        {
            private readonly Action onHoverAction;

            public ResetScrollContainer(Action onHoverAction)
            {
                this.onHoverAction = onHoverAction;
            }

            protected override bool OnHover(HoverEvent e)
            {
                onHoverAction?.Invoke();
                return base.OnHover(e);
            }
        }
    }
}
