using System;
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
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class ReplaySelectScreen : EscapeableScreen
    {
        protected ReplayCarousel Carousel { get; private set; }

        private Container carouselContainer;

        private ReplayController replayController;

        [Resolved]
        private ReplayManager replayManager { get; set; }

        [Resolved]
        private MapFileStorage mapStorage { get; set; }

        private Box background { get; set; }
        private MovingGrid grid { get; set; }

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
                carouselContainer = new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Size = new Vector2(0.7f, 1),
                    Child = new LoadingSpinner(true) { State = { Value = Visibility.Visible } }
                },
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(200, 200, 200, 150),
                    Size = new Vector2(0.3f, 1)
                },
            });
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

        protected virtual bool OnStart()
        {
            if (replayController != null)
                return false;

            this.Push(replayController = new ReplayController());
            Task.Run(async () =>
            {
                var data = await replayManager.GetReplayData(Carousel.SelectedReplayData);
                var terrainFile = await mapStorage.GetOrDownloadMap(data.ReplayInfo.MapId);
                replayController.LoadReplay(data, terrainFile);
            });

            return true;
        }

        private void updateSelected(ReplayInfo updatedReplay)
        {
        }

        private void carouselReplaysLoaded()
        {
            Carousel.AllowSelection = true;

            if (Carousel.SelectedReplayData != null)
                return;
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
