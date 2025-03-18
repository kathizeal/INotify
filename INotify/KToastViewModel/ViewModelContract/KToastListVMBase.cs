using INotify.KToastView.Model;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class KToastListVMBase : KToastViewModelBase
    {

        public ViewType CurrentViewType { get; set; }


        public Dictionary<string, BitmapImage> IconCache = new Dictionary<string, BitmapImage>();

        private ObservableCollection<KToastVObj> _kToastNotifications;
        public ObservableCollection<KToastVObj> KToastNotifications
        {
            get
            {
                if (_kToastNotifications == null)
                {
                    _kToastNotifications = new ObservableCollection<KToastVObj>();
                }
                return _kToastNotifications;
            }
            set
            {
                _kToastNotifications = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<KNotificationByPackageCVS> _KToastNotificationPackageCVS;
        public ObservableCollection<KNotificationByPackageCVS> KToastNotificationPackageCVS
        {
            get
            {
                if (_KToastNotificationPackageCVS == null)
                {
                    _KToastNotificationPackageCVS = new ObservableCollection<KNotificationByPackageCVS>();
                }
                return _KToastNotificationPackageCVS;
            }
            set
            {
                _KToastNotificationPackageCVS = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<KNotificationBySpaceCVS> _KToastNotificationSpaceCVS;
        public ObservableCollection<KNotificationBySpaceCVS> KToastNotificationSpaceCVS
        {
            get
            {
                if (_KToastNotificationSpaceCVS == null)
                {
                    _KToastNotificationSpaceCVS = new ObservableCollection<KNotificationBySpaceCVS>();
                }
                return _KToastNotificationSpaceCVS;
            }
            set
            {
                _KToastNotificationSpaceCVS = value;
                OnPropertyChanged();
            }
        }

        #region Methods
        public abstract void UpdateViewType(ViewType viewType);

        public abstract void LoadControl();
        public abstract void UpdateKToastNotifications(ObservableCollection<KToastNotification> kToastNotifications);
        public abstract void UpdateKToastNotification(KToastVObj toastData);
        public abstract Task PopulateKToastNotifications(ObservableCollection<KToastBObj> kToastDataNotifications);
        public abstract Task PopulateKToastNotificationsByPackageId(string packageId, ObservableCollection<KToastBObj> kToastDataNotifications);
        public abstract Task<KToastVObj> AddKToastNotification(KToastBObj toastData);
        public abstract void GetAllSpace();
        public abstract Task PopulateSpaces(ObservableCollection<KSpace> kSpaceDataNotifications);
        public abstract void GetPackagesBySpaceById(string spaceId);
        public abstract void PopulatePackageBySpaceId(string spaceId, ObservableCollection<KPackageProfile> packageProfiles);
        public abstract void AddPackageToSpace(KPackageProfile package, string spaceId);
        public abstract void PopulateAddPackageToSpace(KPackageProfile PackageProfile, string spaceId);

        public abstract void GetKToastNotificationByPackageId(string packageId);
        #endregion


        public KToastListVMBase()
        {
        }
    }
}
