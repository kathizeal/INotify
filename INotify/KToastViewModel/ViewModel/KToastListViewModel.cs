using INotify.KToastView.Model;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Extension;
using WinCommon.Util;
using Windows.ApplicationModel;
using Windows.UI.Core;

namespace INotify.KToastViewModel.ViewModelContract
{
    public class KToastListViewModel : KToastListVMBase
    {
        public KToastListViewModel()
        {
        }

        #region Public Methods

        public override void LoadControl()
        {
            PoputeAllNotifications();
        }

        public override void UpdateViewType(ViewType viewType)
        {
            CurrentViewType = viewType;
            switch (viewType)
            {
                case ViewType.All:
                    PoputeAllNotifications();
                    break;
                case ViewType.Space:
                    PopulateSpaceCVS();
                    break;
                case ViewType.Package:
                    PopulatePackageCVS();
                    break;
            }
        }

        public void PoputeAllNotifications()
        {
            KToastNotifications.Clear();
            if (View is null)
            {
                return;
            }
            GetAllNotifications();
        }

        public void PopulateSpaceCVS()
        {
            if (View is null)
            {
                return;
            }
            GetAllSpace();
        }

        public void PopulatePackageCVS()
        {
            if (View is null)
            {
                return;
            }
            GetAllPackages();
        }

        [Obsolete]
        public override void UpdateKToastNotifications(ObservableCollection<KToastNotification> kToastNotifications)
        {
        }

        public void GetAllNotifications()
        {
            var getKToastsRequest = new GetKToastsRequest(NotificatioRequestType.All, CurrentViewType, default, INotifyConstant.CurrentUser);
            var getKToasts = new GetKToasts(getKToastsRequest, new GetAllKToastsNotificationPresenterCallback(this));
            getKToasts.Execute();
        }

        public override void GetKToastNotificationByPackageId(string packageId)
        {
            var getKToastsRequest = new GetKToastsRequest(NotificatioRequestType.Individual, CurrentViewType, packageId, INotifyConstant.CurrentUser);
            var getKToasts = new GetKToasts(getKToastsRequest, new GetAllKToastsNotificationPresenterCallback(this));
            getKToasts.Execute();
        }

        public override void UpdateKToastNotification(KToastVObj ToastData)
        {
            var updateKToastRequest = new UpdateKToastRequest(ToastData, INotifyConstant.CurrentUser);
            var updateKToast = new UpdateKToast(updateKToastRequest, new UpdateKToastsNotificationPresenterCallback(this));
            updateKToast.Execute();
        }

        public override async Task PopulateKToastNotifications(ObservableCollection<KToastBObj> kToastDataNotifications)
        {
            KToastNotifications.Clear();
            foreach (var kToastData in kToastDataNotifications)
            {
                var data = await AddKToastNotification(kToastData);
                KToastUtil.InsertNotificationByCreatedTime(KToastNotifications, data);
            }
        }

        public override async Task PopulateKToastNotificationsByPackageId(string packageId, ObservableCollection<KToastBObj> kToastDataNotifications)
        {
            KToastNotifications.Clear();
            await PopulateKToastNotifications(kToastDataNotifications);
        }

        public override async Task<KToastVObj> AddKToastNotification(KToastBObj toastData)
        {
            var kToastViewData = new KToastVObj(toastData.NotificationData, toastData.ToastPackageProfile);
            if (IconCache.ContainsKey(kToastViewData.ToastPackageProfile.PackageId))
            {
                kToastViewData.AppIcon = IconCache[kToastViewData.ToastPackageProfile.PackageId];
            }
            else
            {
                var (icon, success) = await kToastViewData.SetAppIcon();
                if (success)
                {
                    IconCache[kToastViewData.ToastPackageProfile.PackageId] = icon;
                }
            }
            return kToastViewData;
        }

        public override void GetAllSpace()
        {
            var getAllSpaceRequest = new GetAllSpaceRequest(INotifyConstant.CurrentUser);
            var getAllSpace = new GetAllSpace(getAllSpaceRequest, new GetAllSpacePresenterCallback(this));
            getAllSpace.Execute();
        }

        public override void GetAllPackages()
        {
            var getAllPackageRequest = new GetAllKPackageProfilesRequest(INotifyConstant.CurrentUser);
            var getAllPackage = new GetAllKPackageProfiles(getAllPackageRequest, new GetAllKPackageProfilesPresenterCallback(this));
            getAllPackage.Execute();
        }

        public override void GetPackagesBySpaceById(string spaceId)
        {
            var getPackageBySpaceRequest = new GetPackageBySpaceRequest(spaceId, INotifyConstant.CurrentUser);
            var getPackageBySpace = new GetPackageBySpace(getPackageBySpaceRequest, new GetPackageBySpacePresenterCallback(this));
            getPackageBySpace.Execute();
        }

        public override void AddPackageToSpace(KPackageProfile package, string spaceId)
        {
            var addPackageToSpaceRequest = new AddPackageToSpaceRequest(package.PackageId, spaceId, INotifyConstant.CurrentUser);
            var addPackageToSpace = new AddPackageToSpace(addPackageToSpaceRequest, new AddPackageToSpacePresenterCallback(this));
            addPackageToSpace.Execute();
        }

        public override async Task PopulateSpaces(ObservableCollection<KSpace> kSpaceDataNotifications)
        {
            foreach (var kSpaceData in kSpaceDataNotifications)
            {
                await AddKToastSpace(kSpaceData);
            }
        }

        public override void PopulatePackageBySpaceId(string spaceId, ObservableCollection<KPackageProfile> packageProfiles)
        {
            PopulatePackages(packageProfiles);
        }

        public override void PopulatePackages(ObservableCollection<KPackageProfile> packageProfiles)
        {
            KPackageProfilesList.Clear();
            foreach (var packageProfile in packageProfiles)
            {
                var packageProfileVObj = new KPackageProfileVObj();
                packageProfileVObj.Update(packageProfile);
                packageProfileVObj.PopulateAppIconAsync();
                KPackageProfilesList.Add(packageProfileVObj);
            }
            PopulateAllInOnePackageSpace();
        }

        private void PopulateAllInOnePackageSpace()
        {
            var package = KPackageProfilesList.FirstOrDefault(p => p.PackageId == IKPackageProfileConstant.DefaultAllInPackageId);
            if (package is null)
            {
                var packageProfileVObj = new KPackageProfileVObj();
                packageProfileVObj.Update(INotifyUtil.CreatePackageProfileForAllNotification());
                packageProfileVObj.PopulateAppIconAsync();
                KPackageProfilesList.Add(packageProfileVObj);
            }
        }

        public override void PopulateAddPackageToSpace(KPackageProfile packageProfile, string spaceId)
        {
        }

        #endregion

        #region Private Methods

        private async Task AddKToastSpace(KSpace spaceData)
        {
        }

        #endregion

        #region PresenterCallBack

        public class UpdateKToastsNotificationPresenterCallback : IUpdateKToastPresenterCallback
        {
            private KToastListViewModel _presenter { get; set; }

            public UpdateKToastsNotificationPresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnIgnored(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnProgress(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnSuccess(ZResponse<UpdateKToastResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    _ = _presenter.AddKToastNotification(response.Data.KToastData);
                });
            }
        }

        public class GetAllKToastsNotificationPresenterCallback : IGetKToastPresenterCalback
        {
            private KToastListViewModel _presenter { get; set; }

            public GetAllKToastsNotificationPresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<GetKToastsResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<GetKToastsResponse> response)
            {
            }

            public void OnIgnored(ZResponse<GetKToastsResponse> response)
            {
            }

            public void OnProgress(ZResponse<GetKToastsResponse> response)
            {
            }

            public async void OnSuccess(ZResponse<GetKToastsResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    if (response.Data.ViewType is ViewType.Package && response.Data.PackageId is not IKPackageProfileConstant.DefaultAllInPackageId)
                    {
                        _ = _presenter.PopulateKToastNotificationsByPackageId(response.Data.PackageId, response.Data.ToastDataNotifications);
                    }
                    else
                    {
                        _ = _presenter.PopulateKToastNotifications(response.Data.ToastDataNotifications);
                    }
                });
            }
        }

        public class GetAllSpacePresenterCallback : IGetAllSpacePresenterCallback
        {
            private KToastListViewModel _presenter { get; set; }

            public GetAllSpacePresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<GetAllSpaceResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<GetAllSpaceResponse> response)
            {
            }

            public void OnIgnored(ZResponse<GetAllSpaceResponse> response)
            {
            }

            public void OnProgress(ZResponse<GetAllSpaceResponse> response)
            {
            }

            public async void OnSuccess(ZResponse<GetAllSpaceResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    _ = _presenter.PopulateSpaces(response.Data.Spaces);
                });
            }
        }

        public class GetPackageBySpacePresenterCallback : IGetPackageBySpacePresenterCallback
        {
            private KToastListViewModel _presenter { get; set; }

            public GetPackageBySpacePresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<GetPackageBySpaceResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<GetPackageBySpaceResponse> response)
            {
            }

            public void OnIgnored(ZResponse<GetPackageBySpaceResponse> response)
            {
            }

            public void OnProgress(ZResponse<GetPackageBySpaceResponse> response)
            {
            }

            public async void OnSuccess(ZResponse<GetPackageBySpaceResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    _presenter.PopulatePackageBySpaceId(response.Data.SpaceId, response.Data.Packages);
                });
            }
        }

        public class AddPackageToSpacePresenterCallback : IAddPackageToSpacePresenterCallback
        {
            private KToastListViewModel _presenter { get; set; }

            public AddPackageToSpacePresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<AddPackageToSpaceResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<AddPackageToSpaceResponse> response)
            {
            }

            public void OnIgnored(ZResponse<AddPackageToSpaceResponse> response)
            {
            }

            public void OnProgress(ZResponse<AddPackageToSpaceResponse> response)
            {
            }

            public async void OnSuccess(ZResponse<AddPackageToSpaceResponse> response)
            {
            }
        }

        public class GetAllKPackageProfilesPresenterCallback : IGetAllKPackageProfilesPresenterCallback
        {
            private KToastListViewModel _presenter;

            public GetAllKPackageProfilesPresenterCallback(KToastListViewModel presenter)
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
                    _presenter.PopulatePackages(response.Data.KPackageProfiles);
                });
            }
        }

        #endregion
    }
}
