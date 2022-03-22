using System;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public class SimpleErrorNotification : SimpleNotification
    {
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
            if (clipboard != null)
                Text += "\n\nPlease click this to copy the error and give that to the devs.";
        }

        private bool copyErrorToClipboard()
        {
            if (error != null)
                clipboard?.SetText(error.ToString());

            return false;
        }
    }
}
