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
        public  string NotificationId { get ; set;}
        public  string PackageId { get ; set;}
        public  DateTimeOffset CreatedTime { get ; set;}
        public  string NotificationTitle { get ; set;}
        public  string NotificationMessage { get ; set;}
        public KToastNotification() { }
    }
}
