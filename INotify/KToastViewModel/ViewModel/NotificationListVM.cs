using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Util;
using WinLogger;
using WinCommon.Util;
using System;

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
                    _viewModel.DispatcherQueue.TryEnqueue(() =>
                    {
                        _viewModel.IsLoading = false;
                        var data = response.Data;

                        if (data.IsPackageView)
                        {
                            _viewModel.Packages.Clear();
                            foreach (var package in data.Packages)
                            {
                                _viewModel.Packages.Add(package);
                            }
                            _viewModel.Logger.Info(LogManager.GetCallerInfo(),
                                $"Loaded {data.Packages.Count} packages for {data.TargetType}: {data.TargetId}");
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