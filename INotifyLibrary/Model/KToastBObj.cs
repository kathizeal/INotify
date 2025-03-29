using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;

namespace INotifyLibrary.Model
{
    public class KToastBObj : ObservableObject
    {
        private KToastNotification _NotificationData;

        public KToastNotification NotificationData
        {
            get { return _NotificationData; }
            private set { SetIfDifferent(ref _NotificationData, value); }
        }

        private KPackageProfile _toastPackageProfile;
        public KPackageProfile ToastPackageProfile
        {
            get { return _toastPackageProfile; }
            private set { SetIfDifferent(ref _toastPackageProfile, value); }
        }

        public KToastBObj(KToastNotification notificationData, KPackageProfile packageProfile)
        {
            NotificationData = notificationData;
            ToastPackageProfile = packageProfile;
        }
        public KToastBObj DeepClone()
        {
            return new KToastBObj(
                NotificationData.DeepClone(),
                ToastPackageProfile.DeepClone()
            );
        }

        public KToastBObj()
        {
            
        }

        public void Update(KToastBObj newData)
        {
            if (newData == null) { return; }

            NotificationData.Update(newData.NotificationData);
            ToastPackageProfile.Update(newData.ToastPackageProfile);
        }
    }
}
