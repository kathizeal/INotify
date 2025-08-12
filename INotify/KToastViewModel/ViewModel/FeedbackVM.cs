using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading;
using System.Windows.Input;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;

namespace INotify.KToastViewModel.ViewModel
{
    public class FeedbackVM : FeedbackVMBase, IDisposable
    {
        public FeedbackVM()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            SubmitFeedbackCommand = new RelayCommand(SubmitFeedback, () => CanSubmit);
            ClearFormCommand = new RelayCommand(ClearForm);
            LoadFeedbackHistoryCommand = new RelayCommand(LoadFeedbackHistory);
        }

        public override void SubmitFeedback()
        {
            if (!CanSubmit) return;

            IsSubmitting = true;
            IsSubmitStatusVisible = false;

            try
            {
                var appVersion = GetAppVersion();
                var osVersion = GetOSVersion();
                
                var request = new SubmitFeedbackRequest(
                    Title,
                    Message,
                    SelectedCategory,
                    Email,
                    appVersion,
                    osVersion,
                    INotifyConstant.CurrentUser
                );

                var callback = new SubmitFeedbackPresenterCallback(this);
                var usecase = new SubmitFeedback(request, callback);
                usecase.Execute();

                Logger.Info(LogManager.GetCallerInfo(), $"Submitting feedback: {Title} - Category: {SelectedCategory}");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error submitting feedback: {ex.Message}");
                ShowStatusMessage("An error occurred while submitting feedback. Please try again.", true);
                IsSubmitting = false;
            }
        }

        public override void ClearForm()
        {
            Title = string.Empty;
            Message = string.Empty;
            Email = string.Empty;
            SelectedCategory = FeedbackCategory.General;
            IsSubmitStatusVisible = false;
            
            Logger.Info(LogManager.GetCallerInfo(), "Feedback form cleared");
        }

        public override void LoadFeedbackHistory()
        {
            try
            {
                //TODO: Implement feedback history loading when GetUserFeedback DataManager is created
                Logger.Info(LogManager.GetCallerInfo(), "Loading feedback history");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error loading feedback history: {ex.Message}");
            }
        }

        private string GetAppVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetOSVersion()
        {
            try
            {
                return Environment.OSVersion.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        public new void Dispose()
        {
            base.Dispose();
        }

        private sealed class SubmitFeedbackPresenterCallback : ISubmitFeedbackPresenterCallback
        {
            private readonly FeedbackVM _viewModel;

            public SubmitFeedbackPresenterCallback(FeedbackVM viewModel)
            {
                _viewModel = viewModel;
            }

            public void OnSuccess(ZResponse<SubmitFeedbackResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsSubmitting = false;
                    _viewModel.ShowStatusMessage("Feedback submitted successfully! Thank you for your input.");
                    _viewModel.ClearForm();
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), "Feedback submitted successfully");
                });
            }

            public void OnProgress(ZResponse<SubmitFeedbackResponse> response)
            {
                // Progress updates if needed
            }

            public void OnFailed(ZResponse<SubmitFeedbackResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsSubmitting = false;
                    _viewModel.ShowStatusMessage("Failed to submit feedback. Please try again.", true);
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), "Failed to submit feedback");
                });
            }

            public void OnError(ZError error)
            {
                var errorMessage = error?.ErrorObject?.ToString() ?? "Unknown error";
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsSubmitting = false;
                    _viewModel.ShowStatusMessage($"Error submitting feedback: {errorMessage}", true);
                    _viewModel.Logger.Error(LogManager.GetCallerInfo(), $"Error submitting feedback: {errorMessage}");
                });
            }

            public void OnCanceled(ZResponse<SubmitFeedbackResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsSubmitting = false;
                    _viewModel.ShowStatusMessage("Feedback submission was canceled.", true);
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), "Feedback submission canceled");
                });
            }

            public void OnIgnored(ZResponse<SubmitFeedbackResponse> response)
            {
                _viewModel.DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.IsSubmitting = false;
                    _viewModel.Logger.Info(LogManager.GetCallerInfo(), "Feedback submission ignored");
                });
            }
        }
    }

    // RelayCommand implementation for ICommand
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}