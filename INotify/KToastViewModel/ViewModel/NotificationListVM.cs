using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Util;
using WinLogger;
using WinCommon.Util;
using System;
using System.Linq;
using System.Collections.Generic;

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
                    var group = new KPackageNotificationGroup(packageProfile);
                    
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
                    var group = new KPackageNotificationGroup(package);
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
    }
}