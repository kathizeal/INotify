using INotify.KToastView.Model;
using INotify.Util;
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
using WinLogger;
using Windows.ApplicationModel;
using Windows.UI.Core;

namespace INotify.KToastViewModel.ViewModelContract
{
    public class KToastListViewModel : KToastListVMBase
    {
        public KToastListViewModel()
        {
            // Load available apps when ViewModel is created
            LoadAvailableApps();
        }

        #region Public Methods

        public override void LoadInitialNotifications()
        {
            try
            {
                if (IsLoading) return;

                IsLoading = true;
                ResetPagination();
                KToastNotifications.Clear();

                var (skip, take) = GetPaginationParams();
                var getKToastsRequest = new GetKToastsRequest(
                    NotificatioRequestType.All, 
                    ViewType.All, 
                    default, 
                    INotifyConstant.CurrentUser, 
                    skip, 
                    take,
                    SearchKeyword,
                    SelectedAppFilter,
                    SelectedDate,
                    FromDate,
                    ToDate);

                var getKToasts = new GetKToasts(getKToastsRequest, new GetKToastsNotificationPresenterCallback(this, false));
                getKToasts.Execute();

                Logger.Info(LogManager.GetCallerInfo(), "Loading initial notifications with filters");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Logger.Error(LogManager.GetCallerInfo(), $"Error loading initial notifications: {ex.Message}");
            }
        }

        public override void LoadMoreNotifications()
        {
            try
            {
                if (!HasMoreData || IsLoadingMore || IsLoading) return;

                IsLoadingMore = true;
                CurrentPage++;

                var (skip, take) = GetPaginationParams();
                var getKToastsRequest = new GetKToastsRequest(
                    NotificatioRequestType.All, 
                    ViewType.All, 
                    default, 
                    INotifyConstant.CurrentUser, 
                    skip, 
                    take,
                    SearchKeyword,
                    SelectedAppFilter,
                    SelectedDate,
                    FromDate,
                    ToDate);

                var getKToasts = new GetKToasts(getKToastsRequest, new GetKToastsNotificationPresenterCallback(this, true));
                getKToasts.Execute();

                Logger.Info(LogManager.GetCallerInfo(), $"Loading more notifications - Page {CurrentPage} with filters");
            }
            catch (Exception ex)
            {
                IsLoadingMore = false;
                Logger.Error(LogManager.GetCallerInfo(), $"Error loading more notifications: {ex.Message}");
            }
        }

        public override void RefreshNotifications()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Refreshing notifications");
                LoadInitialNotifications();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error refreshing notifications: {ex.Message}");
            }
        }

        public override void ApplyFilters()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), $"Applying filters - Keyword: '{SearchKeyword}', App: '{SelectedAppFilter}', Date: {SelectedDate}, Range: {FromDate} to {ToDate}");
                LoadInitialNotifications();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error applying filters: {ex.Message}");
            }
        }

        public override void ClearFilters()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Clearing all filters");
                ClearFilterValues();
                LoadInitialNotifications();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error clearing filters: {ex.Message}");
            }
        }

        public override void LoadAvailableApps()
        {
            try
            {
                var getAllPackageRequest = new GetAllKPackageProfilesRequest(INotifyConstant.CurrentUser);
                var getAllPackage = new GetAllKPackageProfiles(getAllPackageRequest, new GetAllAppsPresenterCallback(this));
                getAllPackage.Execute();

                Logger.Info(LogManager.GetCallerInfo(), "Loading available apps for filtering");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error loading available apps: {ex.Message}");
            }
        }

        public override void AddNotification(KToastVObj notification)
        {
            try
            {
                if (notification != null)
                {
                    // Insert at the beginning since notifications are in chronological order (latest first)
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        KToastNotifications.Insert(0, notification);
                        UpdateTotalCountText();
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error adding notification: {ex.Message}");
            }
        }

        public override void OnScrollToBottom()
        {
            try
            {
                if (HasMoreData && !IsLoadingMore && !IsLoading)
                {
                    LoadMoreNotifications();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error handling scroll to bottom: {ex.Message}");
            }
        }

        private async Task<KToastVObj> CreateKToastVObj(KToastBObj toastData)
        {
            try
            {
                var kToastViewData = new KToastVObj(toastData.NotificationData, toastData.ToastPackageProfile);
                
                if (IconCache.ContainsKey(kToastViewData.ToastPackageProfile.PackageFamilyName))
                {
                    kToastViewData.AppIcon = IconCache[kToastViewData.ToastPackageProfile.PackageFamilyName];
                }
                else
                {
                    var (icon, success) = await kToastViewData.SetAppIcon();
                    if (success)
                    {
                        IconCache[kToastViewData.ToastPackageProfile.PackageFamilyName] = icon;
                    }
                }
                return kToastViewData;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error creating KToastVObj: {ex.Message}");
                return new KToastVObj(toastData.NotificationData, toastData.ToastPackageProfile);
            }
        }

        #endregion

        #region PresenterCallbacks

        public class GetKToastsNotificationPresenterCallback : IGetKToastPresenterCalback
        {
            private KToastListViewModel _presenter { get; set; }
            private bool _isLoadingMore { get; set; }

            public GetKToastsNotificationPresenterCallback(KToastListViewModel presenter, bool isLoadingMore = false)
            {
                _presenter = presenter;
                _isLoadingMore = isLoadingMore;
            }

            public void OnCanceled(ZResponse<GetKToastsResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    if (_isLoadingMore)
                    {
                        _presenter.IsLoadingMore = false;
                        _presenter.CurrentPage--; // Rollback page increment
                    }
                    else
                    {
                        _presenter.IsLoading = false;
                    }
                });
            }

            public void OnError(ZError error)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    if (_isLoadingMore)
                    {
                        _presenter.IsLoadingMore = false;
                        _presenter.CurrentPage--; // Rollback page increment
                    }
                    else
                    {
                        _presenter.IsLoading = false;
                    }
                    _presenter.Logger.Error(LogManager.GetCallerInfo(), $"Error loading notifications: {error?.ErrorObject?.ToString() ?? "Unknown error"}");
                });
            }

            public void OnFailed(ZResponse<GetKToastsResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    if (_isLoadingMore)
                    {
                        _presenter.IsLoadingMore = false;
                        _presenter.CurrentPage--; // Rollback page increment
                    }
                    else
                    {
                        _presenter.IsLoading = false;
                    }
                });
            }

            public void OnIgnored(ZResponse<GetKToastsResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    if (_isLoadingMore)
                    {
                        _presenter.IsLoadingMore = false;
                        _presenter.CurrentPage--; // Rollback page increment
                    }
                    else
                    {
                        _presenter.IsLoading = false;
                    }
                });
            }

            public void OnProgress(ZResponse<GetKToastsResponse> response)
            {
                // Handle progress if needed
            }

            public async void OnSuccess(ZResponse<GetKToastsResponse> response)
            {
                try
                {
                    _presenter.DispatcherQueue.TryEnqueue(async () =>
                    {
                        var data = response.Data;
                        
                        // Update pagination state
                        _presenter.HasMoreData = data.HasMoreData;

                        // Process notifications
                        foreach (var toastData in data.ToastDataNotifications)
                        {
                            var kToastVObj = await _presenter.CreateKToastVObj(toastData);
                            _presenter.KToastNotifications.Add(kToastVObj);
                        }

                        // Update loading states
                        if (_isLoadingMore)
                        {
                            _presenter.IsLoadingMore = false;
                        }
                        else
                        {
                            _presenter.IsLoading = false;
                        }

                        _presenter.UpdateTotalCountText();

                        _presenter.Logger.Info(LogManager.GetCallerInfo(), 
                            $"Loaded {data.ToastDataNotifications.Count} notifications. Total: {_presenter.TotalCount}, HasMore: {_presenter.HasMoreData}");
                    });
                }
                catch (Exception ex)
                {
                    _presenter.Logger.Error(LogManager.GetCallerInfo(), $"Error processing successful response: {ex.Message}");
                }
            }
        }

        public class GetAllAppsPresenterCallback : IGetAllKPackageProfilesPresenterCallback
        {
            private KToastListViewModel _presenter;

            public GetAllAppsPresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<GetAllKPackageProfilesResponse> response)
            {
            }

            public void OnError(ZError error)
            {
                _presenter.Logger.Error(LogManager.GetCallerInfo(), $"Error loading available apps: {error?.ErrorObject?.ToString() ?? "Unknown error"}");
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

            public void OnSuccess(ZResponse<GetAllKPackageProfilesResponse> response)
            {
                _presenter.DispatcherQueue.TryEnqueue(() =>
                {
                    _presenter.AvailableApps.Clear();
                    
                    // Add "All Apps" option at the top
                    _presenter.AvailableApps.Add(new KPackageProfile 
                    { 
                        PackageFamilyName = "", 
                        AppDisplayName = "All Apps" 
                    });

                    // Add all packages
                    foreach (var package in response.Data.KPackageProfiles)
                    {
                        _presenter.AvailableApps.Add(package);
                    }

                    _presenter.Logger.Info(LogManager.GetCallerInfo(), 
                        $"Loaded {response.Data.KPackageProfiles.Count} available apps for filtering");
                });
            }
        }

        #endregion

        #region Obsolete Methods - Kept for Compatibility
        [Obsolete("This method is obsolete. Use LoadInitialNotifications instead.")]
        public void LoadControl()
        {
            LoadInitialNotifications();
        }

        [Obsolete("This method is obsolete. Not supported in notification-only view.")]
        public void UpdateViewType(ViewType viewType)
        {
            // Not applicable for notification-only view
        }

        [Obsolete("This method is obsolete. Not supported in notification-only view.")]
        public void UpdateKToastNotifications(ObservableCollection<KToastNotification> kToastNotifications)
        {
            // Not applicable for notification-only view
        }

        [Obsolete("This method is obsolete. Not supported in notification-only view.")]
        public void UpdateKToastNotification(KToastVObj ToastData)
        {
            AddNotification(ToastData);
        }
        #endregion
    }
}
