using osuTK.Graphics;

namespace AWBWApp.Game.UI.Notifications
{
    public class SimpleErrorNotification : SimpleNotification
    {
        public SimpleErrorNotification()
            : base(true)
        {
            Light.Colour = new Color4(200, 20, 20, 255);

            Activated = () => false;
        }
    }
}
