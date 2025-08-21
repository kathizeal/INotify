using AppList;
using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
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
using WinUI3Component.ViewContract;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class ToastViewModelBase : ObservableObject,ICleanup
    {
        public IView View;
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
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Starting to load installed apps with icons...");
                
                // Load apps with icons on background thread
                InstalledApps = await AppService.GetAllInstalledAppsAsync();
                IsInstalledAppFetched = true;
                
                Logger.Info(LogManager.GetCallerInfo(), $"Successfully loaded {InstalledApps.Count} apps with icons");
                
                // Convert on UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    ConvertInstalledAppsToPackageProfiles();
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error loading installed apps: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in GetInstalledApps: {ex.Message}");
            }
        }

        public void ConvertInstalledAppsToPackageProfiles()
        {
            try
            {
                var hashSet = PackageProfiles.Select(p => p.PackageFamilyName).ToHashSet();
                int appsWithIcons = 0;
                int totalApps = 0;
                
                foreach (var app in InstalledApps)
                {
                    totalApps++;
                    if (!hashSet.Contains(app.PackageFamilyName))
                    {
                        KPackageProfileVObj package = new KPackageProfileVObj();
                        package.PopulateInstalledAppInfo(app, 0);
                        
                        // Debug icon loading
                        if (app.Icon != null)
                        {
                            appsWithIcons++;
                            System.Diagnostics.Debug.WriteLine($"App {app.DisplayName} has icon loaded");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"App {app.DisplayName} has NO icon");
                        }
                        
                        PackageProfiles.Add(package);
                    }
                    hashSet.Add(app.PackageFamilyName);
                }
                
                // Sort the collection by AppDisplayName
                var sortedItems = PackageProfiles.OrderBy(p => p.AppDisplayName).ToList();
                PackageProfiles.Clear();
                foreach (var item in sortedItems)
                {
                    PackageProfiles.Add(item);
                }

                Logger.Info(LogManager.GetCallerInfo(), $"Converted {totalApps} apps to package profiles. {appsWithIcons} apps have icons.");
                System.Diagnostics.Debug.WriteLine($"Icon stats: {appsWithIcons}/{totalApps} apps have icons loaded");

                if(View is IAllPackageView allPackageView)
                {
                    allPackageView.Package1Fetched();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error converting apps to package profiles: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error in ConvertInstalledAppsToPackageProfiles: {ex.Message}");
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
                
                // Sort the collection by AppDisplayName
                var sortedItems = PackageProfiles.OrderBy(p => p.AppDisplayName).ToList();
                PackageProfiles.Clear();
                foreach (var item in sortedItems)
                {
                    PackageProfiles.Add(item);
                }
            }
            if (View is IAllPackageView allPackageView)
            {
                allPackageView.Package2Fetched();
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
