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
        private DateTimeOffset _createdTime;
        private string _notificationTitle;
        private string _notificationMessage;


        [PrimaryKey]
        public string NotificationId { get; set; }

        public string PackageFamilyName { get; set; }

        public DateTimeOffset CreatedTime 
        {
            get => _createdTime;
            set => SetIfDifferent(ref _createdTime, value);
        }
        public string NotificationTitle
        {
            get => _notificationTitle;
            set => SetIfDifferent(ref _notificationTitle, value);
        }

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetIfDifferent(ref _notificationMessage, value);
        }


        [Ignore]
        public string DisplayTime=> CreatedTime.LocalDateTime.ToString(DateTimeFormatConstant.Format_hh_mm_ss_tt_d_Slash_M);

        public KToastNotification() { }

        public KToastNotification DeepClone()
        {
            return new KToastNotification
            {
                NotificationId = this.NotificationId,
                PackageFamilyName = this.PackageFamilyName,
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
                PackageFamilyName = newData.PackageFamilyName;
                CreatedTime = newData.CreatedTime;
                NotificationTitle = newData.NotificationTitle;
                NotificationMessage = newData.NotificationMessage;
            }

          
        }
    }
}
