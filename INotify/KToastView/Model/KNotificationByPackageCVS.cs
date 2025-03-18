using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using WinCommon.Util;

namespace INotify.KToastView.Model
{
    public class KNotificationByPackageCVS : ObservableObject
    {

        private ObservableCollection<KToastVObj> _notificationList;

        public ObservableCollection<KToastVObj> kToastNotifications
        {
            get { return _notificationList; }
            set { SetProperty(ref _notificationList, value); }
        }

        private KPackageProfileVObj _Profile;

        public KPackageProfileVObj Profile
        {
            get { return _Profile; }
            set => SetIfDifferent(ref _Profile, value);
        }

        public ViewType ViewType { get; set; }
        public string HeaderId { get; set; }


        private string _DisplayName;

        public string DisplayName
        {
            get { return _DisplayName; }
            set => SetIfDifferent(ref _DisplayName, value);        }

    }
}
