using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public class SimpleErrorNotification : SimpleNotification
    {
        public bool ShowClickMessage = true;

        [Resolved(CanBeNull = true)]
        private Clipboard clipboard { get; set; }

        private Exception error;

        public SimpleErrorNotification()
            : base(true)
        {
            Light.Colour = new Color4(200, 20, 20, 255);

            Activated = copyErrorToClipboard;
        }

        public SimpleErrorNotification(string message, Exception error)
            : base(true)
        {
            this.error = error;

            Light.Colour = new Color4(200, 20, 20, 255);

            Activated = copyErrorToClipboard;

            //Truncate the message if its too long.
            Text = !string.IsNullOrEmpty(message) ? (message.Length > 256 ? message[..256] : message) : "";
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (clipboard != null && ShowClickMessage)
                Text += "\n\nPlease click this to copy this message, and give that text to the devs.";
        }

        private bool copyErrorToClipboard()
        {
            if (error != null)
                clipboard?.SetText(error.ToString());

            this.FlashColour(new Color4(200, 200, 200, 255), 250);

            return false;
        }
    }
}
