using System;
using System.Collections.Generic;
using AWBWApp.Game.API.Replay;
using AWBWApp.Game.UI.Interrupts;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Select
{
    public class DrawableCarouselReplay : DrawableCarouselItem, IHasContextMenu
    {
        public const float CAROUSEL_BEATMAP_SPACING = 5;

        public const float CAROUSEL_SELECTED_SCALE = 1.05f;

        public const float SELECTEDHEIGHT = (height * CAROUSEL_SELECTED_SCALE + CAROUSEL_BEATMAP_SPACING);

        private const float height = MAX_HEIGHT * 0.9f;

        private ReplayInfo replayInfo;
        private string mapName;

        private Action<ReplayInfo> startRequest;
        private Action<ReplayInfo> showFolderRequest;

        private Sprite background;

        [Resolved(CanBeNull = true)]
        private InterruptDialogueOverlay interruptOverlay { get; set; }

        public DrawableCarouselReplay() { }

        public DrawableCarouselReplay(CarouselReplay panel)
        {
            replayInfo = panel.ReplayInfo;
            mapName = panel.MapName;
            Item = panel;
        }

        [BackgroundDependencyLoader(true)]
        private void load(ReplaySelectScreen replaySelect)
        {
            Panel.Height = height;

            if (replaySelect != null)
            {
                startRequest = replaySelect.FinaliseSelection;
                showFolderRequest = replaySelect.ShowReplayInFolderRequest;
            }

            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;

            MovementContainer.Anchor = Anchor.TopCentre;
            MovementContainer.Origin = Anchor.TopCentre;

            Panel.Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                }
            };
        }

        public MenuItem[] ContextMenuItems
        {
            get
            {
                var items = new List<MenuItem>
                {
                    new MenuItem("Start", () =>
                    {
                        Item.State.Value = CarouselItemState.Selected;
                        startRequest?.Invoke(replayInfo);
                    }),
                    new MenuItem("Show In Folder", () => showFolderRequest?.Invoke(replayInfo))
                };

                if (interruptOverlay != null)
                {
                    var usernameItems = new List<MenuItem>();
                    foreach (var player in replayInfo.Players)
                        usernameItems.Add(new MenuItem($"{player.Value.GetUIFriendlyUsername()} (ID: {player.Value.UserId})", () => interruptOverlay.Push(new EditUsernameInterrupt(replayInfo, player.Value.UserId))));

                    items.AddRange(new MenuItem[]
                    {
                        new MenuItem("Edit Game Name", () => interruptOverlay.Push(new EditGameNameInterrupt(replayInfo))),
                        new MenuItem("Edit Username") { Items = usernameItems },
                        new MenuItem("Delete...", () => interruptOverlay.Push(new DeleteReplayInterrupt(replayInfo, mapName)))
                    });
                }

                return items.ToArray();
            }
        }

        protected override void Selected()
        {
            base.Selected();
            MovementContainer.ScaleTo(1.05f, 500, Easing.OutExpo);
            background.Colour = ColourInfo.GradientVertical(new Color4(20, 43, 51, 255), new Color4(40, 86, 102, 255));
        }

        protected override void Deselected()
        {
            base.Deselected();
            MovementContainer.ScaleTo(1f, 500, Easing.OutExpo);
            background.Colour = new Color4(20, 43, 51, 255);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Item.State.Value == CarouselItemState.Selected)
                startRequest?.Invoke(replayInfo);

            return base.OnClick(e);
        }

        protected override void UpdateItem()
        {
            base.UpdateItem();

            if (Item == null)
                return;

            replayInfo = ((CarouselReplay)Item).ReplayInfo;
            mapName = ((CarouselReplay)Item).MapName;

            DelayedLoadWrapper background;
            DelayedLoadWrapper mainFlow;

            Panel.Children = new Drawable[]
            {
                background = new DelayedLoadWrapper(() => new ReplayCarouselPanelBackground(), 100)
                {
                    RelativeSizeAxes = Axes.Both
                },
                mainFlow = new DelayedLoadWrapper(() => new ReplayCarouselPanelContent(((CarouselReplay)Item)), 100)
                {
                    RelativeSizeAxes = Axes.Both
                }
            };

            background.DelayedLoadComplete += fadeContentIn;
            mainFlow.DelayedLoadComplete += fadeContentIn;
        }

        private void fadeContentIn(Drawable d) => d.FadeInFromZero(250, Easing.OutQuint);
    }
}
