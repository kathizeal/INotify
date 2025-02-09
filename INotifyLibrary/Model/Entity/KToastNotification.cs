using INotifyLibrary.Model.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;

namespace INotifyLibrary.Model.Entity
{
    public class KToastNotification : ObservableObject, IKToastNotification
    {
        public required string NotificationId { get ; set;}
        public required string PackageId { get ; set;}
        public required DateTimeOffset CreatedTime { get ; set;}
        public required string NotificationTitle { get ; set;}
        public required string NotificationMessage { get ; set;}
        public KToastNotification() { }
    }
}
