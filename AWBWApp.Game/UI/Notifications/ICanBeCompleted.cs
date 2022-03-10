using System;

namespace AWBWApp.Game.UI.Notifications
{
    public interface ICanBeCompleted
    {
        Action<Notification> SendCompleteNotification { get; set; }
    }
}
