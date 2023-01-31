using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Editor.Components
{
    public partial class OverlayButtonContainer : VisibilityContainer
    {
        private FillFlowContainer<EditorSpriteButton> overlayButtons;
        private Dictionary<SelectedOverlay, EditorSpriteButton> overlayToButton = new Dictionary<SelectedOverlay, EditorSpriteButton>();

        [Resolved]
        private Bindable<SelectedOverlay> selectedOverlay { get; set; }

        public OverlayButtonContainer()
        {
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 6;
            Width = 60;

            Children = new Drawable[]
            {
                new Box()
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = new Color4(25, 25, 25, 180)
                },
                new FillFlowContainer()
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Margin = new MarginPadding { Vertical = 5 },
                    Children = new Drawable[]
                    {
                        overlayButtons = new FillFlowContainer<EditorSpriteButton>()
                        {
                            Direction = FillDirection.Full,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            addOverlayButton(SelectedOverlay.Capture, "Units/OrangeStar/Infantry-0", "Capture Overlay");
        }

        private void addOverlayButton(SelectedOverlay overlay, string texture, string tooltip)
        {
            var button = new EditorSpriteButton()
            {
                TexturePath = texture,
                TooltipText = tooltip,
                Action = (_, _) => selectedOverlay.Value = selectedOverlay.Value == overlay ? SelectedOverlay.None : overlay
            };

            overlayToButton.Add(overlay, button);
            overlayButtons.Add(button);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            selectedOverlay.BindValueChanged(x => onNewOverlaySelected(x.NewValue));
        }

        private void onNewOverlaySelected(SelectedOverlay overlay)
        {
            foreach (var buttonPair in overlayToButton)
                buttonPair.Value.SetSelected(buttonPair.Key == overlay);
        }

        protected override void PopIn()
        {
            this.ScaleTo(new Vector2(0, 0.8f)).ScaleTo(1, 150, Easing.OutQuint)
                .FadeOut().FadeIn(150, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.ScaleTo(new Vector2(0, 0.8f), 150, Easing.OutQuint)
                .FadeOut(150, Easing.OutQuint);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            base.OnMouseDown(e);
            return true;
        }
    }
}
