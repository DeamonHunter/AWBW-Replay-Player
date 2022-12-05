using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public partial class SimpleNotification : Notification
    {
        public override LocalisableString Text
        {
            get => text;
            set
            {
                text = value;
                Schedule(() => TextDrawable.Text = text);
            }
        }

        public override bool Read
        {
            get => base.Read;
            set
            {
                if (base.Read == value) return;

                base.Read = value;
                Light.FadeTo(value ? 0.5f : 1, 300, Easing.OutQuint);
            }
        }

        private LocalisableString text;

        public override bool IsImportant => isImportant;
        private bool isImportant;

        protected readonly TextFlowContainer TextDrawable;

        public SimpleNotification(bool important)
        {
            isImportant = important;
            Content.Add(TextDrawable = new TextFlowContainer(text => text.Font.With(size: 14))
            {
                Colour = new Color4(64, 64, 64, 255),
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Text = text
            });

            Light.Colour = new Color4(20, 200, 20, 255);
        }
    }
}
