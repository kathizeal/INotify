using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;

namespace INotify.KToastViewModel.ViewModel
{
    public class AppSelectionViewModel : AppSelectionViewModelBase
    {
        public override void AddSelectedAppsToCondition(AppSelectionEventArgs appSelectionEventArgs)
        {
            try
            {
                if (appSelectionEventArgs?.SelectedApps == null || !appSelectionEventArgs.SelectedApps.Any())
                {
                    Logger.Info(LogManager.GetCallerInfo(), "No apps selected to add to condition");
                    return;
                }

                // Convert from UI selection args to domain request
                var selectedApps = appSelectionEventArgs.SelectedApps.Select(app => 
                    new AppConditionData(app.PackageFamilyName, app.DisplayName, app.Publisher)).ToList();

                var request = new AddAppsToConditionRequest(
                    (INotifyLibrary.Domain.SelectionTargetType)appSelectionEventArgs.TargetType,
                    appSelectionEventArgs.CurrentTargetId,
                    selectedApps,
                    INotifyConstant.CurrentUser);

                var presenterCallback = new AddAppsToConditionPresenterCallback(this, appSelectionEventArgs);
                var useCase = new AddAppsToCondition(request, presenterCallback);
                useCase.Execute();

                Logger.Info(LogManager.GetCallerInfo(), 
                    $"Initiated adding {appSelectionEventArgs.SelectedApps.Count} apps to {appSelectionEventArgs.TargetType} condition: {appSelectionEventArgs.CurrentTargetId}");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error initiating add selected apps to condition: {ex.Message}");
            }
        }

       

        private class AddAppsToConditionPresenterCallback : IAddAppsToConditionPresenterCallback
        {
            private readonly AppSelectionViewModel _viewModel;
            private readonly AppSelectionEventArgs _originalArgs;

            public AddAppsToConditionPresenterCallback(AppSelectionViewModel viewModel, AppSelectionEventArgs originalArgs)
            {
                _viewModel = viewModel;
                _originalArgs = originalArgs;
            }

            public void OnSuccess(ZResponse<AddAppsToConditionResponse> response)
            {
                var data = response.Data;
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    $"Successfully added {data.SuccessCount}/{data.TotalCount} apps to {data.TargetType} condition: {data.TargetId}");

                if (data.FailedApps?.Any() == true)
                {
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                        $"Failed to add {data.FailedApps.Count} apps: {string.Join(", ", data.FailedApps)}");
                }
            }

            public void OnProgress(ZResponse<AddAppsToConditionResponse> response)
            {
                // Progress updates if needed
            }

            public void OnFailed(ZResponse<AddAppsToConditionResponse> response)
            {
                _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                    $"Failed to add apps to {_originalArgs.TargetType} condition: {_originalArgs.CurrentTargetId}");
            }

            public void OnError(WinCommon.Error.ZError error)
            {
                var errorMessage = error?.ErrorObject?.ToString() ?? "Unknown error";
                _viewModel.Logger.Error(LogManager.GetCallerInfo(), 
                    $"Error adding apps to condition: {errorMessage}");
            }

            public void OnCanceled(ZResponse<AddAppsToConditionResponse> response)
            {
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    "Add apps to condition operation was canceled");
            }

            public void OnIgnored(ZResponse<AddAppsToConditionResponse> response)
            {
                _viewModel.Logger.Info(LogManager.GetCallerInfo(), 
                    "Add apps to condition operation was ignored");
            }
        }
        public class GetAllKPackageProfilesPresenterCallback : IGetAllKPackageProfilesPresenterCallback
        {
            private AppSelectionViewModelBase _presenter;

            public GetAllKPackageProfilesPresenterCallback(AppSelectionViewModelBase presenter)
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
