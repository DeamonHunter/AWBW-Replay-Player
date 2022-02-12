using System;
using System.Threading.Tasks;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.Game.Logic;
using AWBWApp.Game.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Screens;

namespace AWBWApp.Game.UI.Select
{
    public class ReplaySelectScreen : Screen
    {
        protected ReplayCarousel Carousel { get; private set; }

        private Container carouselContainer;

        private ReplayController replayController;

        [Resolved]
        private ReplayManager replayManager { get; set; }

        [Resolved]
        private MapFileStorage mapStorage { get; set; }

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
                new ResetScrollContainer(() => Carousel.ScrollToSelected())
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 250
                },
                carouselContainer = new Container()
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new LoadingSpinner(true) { State = { Value = Visibility.Visible } }
                }
            });
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(this);
            return dependencies;
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
                var terrainFile = mapStorage.Get(data.ReplayInfo.MapId);
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
