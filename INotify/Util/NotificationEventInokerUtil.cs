using INotify.KToastView.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.Util
{
    public static class NotificationEventInokerUtil
    {
        public static event Action<NotificationReceivedEventArgs> NotificationReceived;

        public static void NotifyNotificationListened(NotificationReceivedEventArgs eventArgs)
        {
            NotificationReceived?.Invoke(eventArgs);
        }

    }

    public class NotificationReceivedEventArgs : EventArgs
    {
        public KToastVObj Notification { get; }

        public NotificationReceivedEventArgs(KToastVObj notification)
        {
            Notification = notification;
        }
    }
}
