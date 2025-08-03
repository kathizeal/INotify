using AppList;
using INotify.KToastView.Model;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinCommon.Util;
using WinLogger;
using WinLogger.Contract;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class ToastViewModelBase : ObservableObject,ICleanup
    {
        private bool IsInstalledAppFetched = false;

        public ObservableCollection<KPackageProfileVObj> PackageProfiles = new();

        #region Properties

        protected ObservableCollection<InstalledAppInfo> InstalledApps = new();

        public Dictionary<string, BitmapImage> IconCache = new Dictionary<string, BitmapImage>();

        public readonly ILogger Logger = LogManager.GetLogger();
        public CancellationTokenSource cts { get; private set; }

        public DispatcherQueue DispatcherQueue { get; private set; }

        private string _OwnerZuid;
        public string OwnerZuid
        {
            get { return _OwnerZuid; }
            set { _OwnerZuid = value; OnPropertyChanged(); }
        }

        protected readonly InstalledAppsService AppService;

        #endregion Properties

        #region Constructor

        public ToastViewModelBase()
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();
            AppService = new InstalledAppsService();
            ResetCTS();
        }

        #endregion Constructor

        public void ResetCTS()
        {
            if (cts != null)
            {
                cts.Cancel();
            }

            cts = new CancellationTokenSource();
        }

        #region Virtual Methods

        public virtual void Dispose()
        {
        }

        protected virtual void RegisterNotification()
        {
        }
        protected virtual void UnRegisterNotification()
        {
        }

        protected virtual void ClearData()
        {

        }

        public async void GetInstalledApps()
        {
            InstalledApps = await AppService.GetAllInstalledAppsAsync();
            IsInstalledAppFetched = true;
            ConvertInstalledAppsToPackageProfiles();


        }

        private void ConvertInstalledAppsToPackageProfiles()
        {
            PackageProfiles.Clear();
            foreach (var app in InstalledApps)
            {
                KPackageProfileVObj package = new KPackageProfileVObj();
                package.PopulateInstalledAppInfo(app, priority: INotifyLibrary.Util.Enums.Priority.None, 0);
                PackageProfiles.Add(package);
            }
        }

        #endregion Virtual Methods
    }
}
