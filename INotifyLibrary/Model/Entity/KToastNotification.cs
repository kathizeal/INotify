using INotifyLibrary.Model.Contract;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;

namespace INotifyLibrary.Model.Entity
{
    public class KToastNotification : ObservableObject, IKToastNotification
    {
        private string _notificationId;
        private string _packageId;
        private DateTimeOffset _createdTime;
        private string _notificationTitle;
        private string _notificationMessage;


        [PrimaryKey]
        public string NotificationId
        {
            get => _notificationId;
            set => SetProperty(ref _notificationId, value);
        }

        public string PackageId
        {
            get => _packageId;
            set => SetProperty(ref _packageId, value);
        }

        public DateTimeOffset CreatedTime
        {
            get => _createdTime;
            set => SetProperty(ref _createdTime, value);
        }

        public string NotificationTitle
        {
            get => _notificationTitle;
            set => SetProperty(ref _notificationTitle, value);
        }

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetProperty(ref _notificationMessage, value);
        }


        private string _DisplayTime;

        [Ignore]
        public string DisplayTime
        {
            get => CreatedTime.LocalDateTime.ToString("hh.mm.ss tt d/M"); 
            set => SetProperty(ref _DisplayTime, value);
        }

        public KToastNotification() { }

        public KToastNotification DeepClone()
        {
            return new KToastNotification
            {
                NotificationId = this.NotificationId,
                PackageId = this.PackageId,
                CreatedTime = this.CreatedTime,
                NotificationTitle = this.NotificationTitle,
                NotificationMessage = this.NotificationMessage
            };
        }

        public void Update(KToastNotification newData)
        {
            if (newData != null)
            {
                NotificationId = newData.NotificationId;
                PackageId = newData.PackageId;
                CreatedTime = newData.CreatedTime;
                NotificationTitle = newData.NotificationTitle;
                NotificationMessage = newData.NotificationMessage;
            }

          
        }
    }
}
