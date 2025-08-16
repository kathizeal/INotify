using AppList;
using INotify.KToastView.Model;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;
using WinLogger.Contract;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class ToastViewModelBase : ObservableObject,ICleanup
    {
        private bool IsInstalledAppFetched = false;

        public readonly ObservableCollection<KPackageProfileVObj> PackageProfiles = new();

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


        protected virtual void ClearData()
        {
            PackageProfiles.Clear();
        }

        public async void GetInstalledApps()
        {
            InstalledApps = await AppService.GetAllInstalledAppsAsync();
            IsInstalledAppFetched = true;
            ConvertInstalledAppsToPackageProfiles();


        }

        public void ConvertInstalledAppsToPackageProfiles()
        {
            var hashSet = PackageProfiles.Select(p => p.PackageFamilyName).ToHashSet();
            foreach (var app in InstalledApps)
            {

                if (!hashSet.Contains(app.PackageFamilyName))
                {
                    KPackageProfileVObj package = new KPackageProfileVObj();
                    package.PopulateInstalledAppInfo(app, 0);
                    PackageProfiles.Add(package);
                }
                hashSet.Add(app.PackageFamilyName);
              
            }
        }

        public void GetAppPackageProfile()
        {
            var getAllPackageRequest = new GetAllKPackageProfilesRequest(INotifyConstant.CurrentUser);
            var getAllPackage = new GetAllKPackageProfiles(getAllPackageRequest, new GetAllKPackageProfilesPresenterCallback(this));
            getAllPackage.Execute();
        }

        public void SyncAppPackageProfileWithInstalled(ObservableCollection<KPackageProfile> packageProfiles)
        {
            var hashSet = PackageProfiles.Select(p => p.PackageFamilyName).ToHashSet();
            if (CommonUtil.IsNonEmpty(packageProfiles))
            {
                foreach (var package in packageProfiles)
                {
                    if (!hashSet.Contains(package.PackageFamilyName))
                    {
                        KPackageProfileVObj VObjpackage = new KPackageProfileVObj();
                        VObjpackage.PopulateInstalledAppInfo(package, 0);
                        PackageProfiles.Add(VObjpackage);
                    }
                    hashSet.Add(package.PackageFamilyName);
                }
            }
        }


        #endregion Virtual Methods

        public class GetAllKPackageProfilesPresenterCallback : IGetAllKPackageProfilesPresenterCallback
        {
            private ToastViewModelBase _presenter;

            public GetAllKPackageProfilesPresenterCallback(ToastViewModelBase presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<GetAllKPackageProfilesResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<GetAllKPackageProfilesResponse> response)
            {
            }

            public void OnIgnored(ZResponse<GetAllKPackageProfilesResponse> response)
            {
            }

            public void OnProgress(ZResponse<GetAllKPackageProfilesResponse> response)
            {
            }

            public async void OnSuccess(ZResponse<GetAllKPackageProfilesResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    _presenter.SyncAppPackageProfileWithInstalled(response.Data.KPackageProfiles);
                });
            }
        }

    }
}
