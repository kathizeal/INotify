using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;

namespace INotify.KToastViewModel.ViewModel
{
    public class NotificationListVM : NotificationListVMBase
    {
        public override void LoadNotifications()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectionTypeId))
                {
                    Logger.Info(LogManager.GetCallerInfo(), "SelectionTypeId is null or empty, skipping load");
                    return;
                }

                IsLoading = true;

                var request = new GetNotificationsByConditionRequest(
                    CurrentTargetType,
                    SelectionTypeId,
                    IsPackageView,
                    INotifyConstant.CurrentUser);

                var presenterCallback = new GetNotificationsByConditionPresenterCallback(this);
                var useCase = new GetNotificationsByCondition(request, presenterCallback);
                useCase.Execute();

                Logger.Info(LogManager.GetCallerInfo(), 
                    $"Loading {(IsPackageView ? "packages" : "notifications")} for {CurrentTargetType}: {SelectionTypeId}");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error loading notifications: {ex.Message}");
            }
        }

        public override void ToggleView()
        {
            IsPackageView = !IsPackageView;
            OnPropertyChanged(nameof(ToggleButtonText));
            OnPropertyChanged(nameof(ViewDisplayText));
            OnPropertyChanged(nameof(NavigationPackages));
        }

        public override void RefreshView()
        {
            LoadNotifications();
        }

        public override void TogglePackageGroup(KPackageNotificationGroup group)
        {
            try
            {
                group?.ToggleExpanded();
                Logger.Info(LogManager.GetCallerInfo(), 
                    $"Toggled package group '{group?.DisplayName}' to {(group?.IsExpanded == true ? "expanded" : "collapsed")}");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error toggling package group: {ex.Message}");
            }
        }

        public override void NavigateToPackage(string packageFamilyName)
        {
            try
            {
                // This method will be called from the UI to trigger navigation
                // The actual scrolling logic will be handled in the code-behind
                Logger.Info(LogManager.GetCallerInfo(), 
                    $"Navigation requested to package: {packageFamilyName}");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error navigating to package: {ex.Message}");
            }
        }

        public override void ClearPackageNotifications(KPackageNotificationGroup group)
        {
            try
            {
                if (group == null || string.IsNullOrEmpty(group.PackageFamilyName))
                {
                    Logger.Error(LogManager.GetCallerInfo(), "Invalid package group provided for clearing notifications");
                    return;
                }

                Logger.Info(LogManager.GetCallerInfo(), 
                    $"Clear notifications requested for package: {group.DisplayName} ({group.PackageFamilyName})");

                var request = new ClearPackageNotificationsRequest(
                    group.PackageFamilyName,
                    INotifyConstant.CurrentUser);

                var presenterCallback = new ClearPackageNotificationsPresenterCallback(this, group);
                var useCase = new ClearPackageNotifications(request, presenterCallback);
                useCase.Execute();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error clearing package notifications: {ex.Message}");
            }
        }

        private void CreateGroupedPackageNotifications(GetNotificationsByConditionResponse data)
        {
            try
            {
                GroupedPackageNotifications.Clear();

                // Get all notifications grouped by package
                var notificationsByPackage = data.Notifications.GroupBy(n => n.ToastPackageProfile.PackageFamilyName);

                foreach (var packageGroup in notificationsByPackage)
                {
                    var packageProfile = packageGroup.First().ToastPackageProfile;
                    var group = new KPackageNotificationGroup(packageProfile.AppDisplayName, packageProfile.PackageFamilyName, packageProfile.LogoFilePath);
                    
                    // Add notifications to the group (sorted by most recent first)
                    var sortedNotifications = packageGroup.OrderByDescending(n => n.NotificationData.CreatedTime);
                    foreach (var notification in sortedNotifications)
                    {
                        group.Add(notification);
                    }

                    GroupedPackageNotifications.Add(group);
                }

                // Also add packages that don't have notifications
                var packagesWithNotifications = notificationsByPackage.Select(g => g.Key).ToHashSet();
                foreach (var package in data.Packages.Where(p => !packagesWithNotifications.Contains(p.PackageFamilyName)))
                {
                    var group = new KPackageNotificationGroup(package.AppDisplayName, package.PackageFamilyName, package.LogoFilePath);
                    GroupedPackageNotifications.Add(group);
                }

                Logger.Info(LogManager.GetCallerInfo(), 
                    $"Created {GroupedPackageNotifications.Count} grouped package notifications");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error creating grouped package notifications: {ex.Message}");
            }
        }

        private class GetNotificationsByConditionPresenterCallback : IGetNotificationsByConditionPresenterCallback
        {
            private readonly NotificationListVM _viewModel;

            public GetNotificationsByConditionPresenterCallback(NotificationListVM viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnSuccess(ZResponse<GetNotificationsByConditionResponse> response)
            {
                try
                {
                    _viewModel.DispatcherQueue.TryEnqueue(() => { 
                    _viewModel.IsLoading = false;
                    var data = response.Data;

                    if (data.IsPackageView)
                    {
                        // Create grouped package notifications for package view
                        _viewModel.CreateGroupedPackageNotifications(data);
                        
                        _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                            $"Loaded {data.Packages.Count} packages with grouped notifications for {data.TargetType}: {data.TargetId}");
                    }
                    else
                    {
                        _viewModel.Notifications.Clear();
                        foreach (var notification in data.Notifications)
                        {
                            _viewModel.Notifications.Add(notification);
                        }
                        _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                            $"Loaded {data.Notifications.Count} notifications for {data.TargetType}: {data.TargetId}");
                    }

                    _viewModel.OnPropertyChanged(nameof(_viewModel.TotalCount));
                    _viewModel.OnPropertyChanged(nameof(_viewModel.ViewDisplayText));
                    _viewModel.OnPropertyChanged(nameof(_viewModel.NavigationPackages));
                    });
                }
                catch (Exception ex)
                {
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                        $"Error processing successful response: {ex.Message}");
                }
            }

            public void OnProgress(ZResponse<GetNotificationsByConditionResponse> response)
            {
                // Progress updates if needed
            }

            public void OnFailed(ZResponse<GetNotificationsByConditionResponse> response)
            {
                _viewModel.IsLoading = false;
                _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                    $"Failed to load data for {_viewModel.CurrentTargetType}: {_viewModel.SelectionTypeId}");
            }

            public void OnError(WinCommon.Error.ZError error)
            {
                _viewModel.IsLoading = false;
                var errorMessage = error?.ErrorObject?.ToString() ?? "Unknown error";
                _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error loading data: {errorMessage}");
            }

            public void OnCanceled(ZResponse<GetNotificationsByConditionResponse> response)
            {
                _viewModel.IsLoading = false;
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    "Load operation was canceled");
            }

            public void OnIgnored(ZResponse<GetNotificationsByConditionResponse> response)
            {
                _viewModel.IsLoading = false;
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    "Load operation was ignored");
            }
        }

        private class ClearPackageNotificationsPresenterCallback : IClearPackageNotificationsPresenterCallback
        {
            private readonly NotificationListVM _viewModel;
            private readonly KPackageNotificationGroup _group;

            public ClearPackageNotificationsPresenterCallback(NotificationListVM viewModel, KPackageNotificationGroup group)
            {
                _viewModel = viewModel;
                _group = group;
            }

            public void OnSuccess(ZResponse<ClearPackageNotificationsResponse> response)
            {
                try
                {
                    _viewModel.DispatcherQueue.TryEnqueue(() =>
                    {
                        var data = response.Data;
                        
                        // Remove the package group from the UI
                        if (_group != null)
                        {
                            _viewModel.GroupedPackageNotifications.Remove(_group);
                        }

                        // Update UI counts
                        _viewModel.OnPropertyChanged(nameof(_viewModel.TotalCount));
                        _viewModel.OnPropertyChanged(nameof(_viewModel.ViewDisplayText));
                        _viewModel.OnPropertyChanged(nameof(_viewModel.NavigationPackages));

                        _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                            $"Successfully cleared {data.ClearedCount} notifications for package {data.PackageFamilyName}");
                    });
                }
                catch (Exception ex)
                {
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                        $"Error processing clear notifications success response: {ex.Message}");
                }
            }

            public void OnProgress(ZResponse<ClearPackageNotificationsResponse> response)
            {
                // Progress updates if needed
            }

            public void OnFailed(ZResponse<ClearPackageNotificationsResponse> response)
            {
                _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                    $"Failed to clear notifications for package {_group?.PackageFamilyName}");
            }

            public void OnError(ZError error)
            {
                var errorMessage = error?.ErrorObject?.ToString() ?? "Unknown error";
                _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error clearing notifications: {errorMessage}");
            }

            public void OnCanceled(ZResponse<ClearPackageNotificationsResponse> response)
            {
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    "Clear notifications operation was canceled");
            }

            public void OnIgnored(ZResponse<ClearPackageNotificationsResponse> response)
            {
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    "Clear notifications operation was ignored");
            }
        }
    }
}