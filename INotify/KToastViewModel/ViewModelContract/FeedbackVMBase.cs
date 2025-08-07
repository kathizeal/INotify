using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class FeedbackVMBase : ToastViewModelBase
    {
        private string _title;
        private string _message;
        private string _email;
        private FeedbackCategory _selectedCategory;
        private bool _isSubmitting;
        private string _submitStatusMessage;
        private bool _isSubmitStatusVisible;
        private ObservableCollection<KFeedback> _userFeedbackHistory = new();

        /// <summary>
        /// Feedback title
        /// </summary>
        public string Title
        {
            get => _title;
            set { SetIfDifferent(ref _title, value); ValidateForm(); }
        }

        /// <summary>
        /// Feedback message content
        /// </summary>
        public string Message
        {
            get => _message;
            set { SetIfDifferent(ref _message, value); ValidateForm(); }
        }

        /// <summary>
        /// User's email for follow-up
        /// </summary>
        public string Email
        {
            get => _email;
            set { SetIfDifferent(ref _email, value); ValidateForm(); }
        }

        /// <summary>
        /// Selected feedback category
        /// </summary>
        public FeedbackCategory SelectedCategory
        {
            get => _selectedCategory;
            set { SetIfDifferent(ref _selectedCategory, value); ValidateForm(); }
        }

        /// <summary>
        /// Whether feedback is currently being submitted
        /// </summary>
        public bool IsSubmitting
        {
            get => _isSubmitting;
            set { SetIfDifferent(ref _isSubmitting, value); OnPropertyChanged(nameof(CanSubmit)); }
        }

        /// <summary>
        /// Status message after submission attempt
        /// </summary>
        public string SubmitStatusMessage
        {
            get => _submitStatusMessage;
            set { SetIfDifferent(ref _submitStatusMessage, value); }
        }

        /// <summary>
        /// Whether to show the submit status message
        /// </summary>
        public bool IsSubmitStatusVisible
        {
            get => _isSubmitStatusVisible;
            set { SetIfDifferent(ref _isSubmitStatusVisible, value); }
        }

        /// <summary>
        /// User's feedback history
        /// </summary>
        public ObservableCollection<KFeedback> UserFeedbackHistory
        {
            get => _userFeedbackHistory;
            set { SetIfDifferent(ref _userFeedbackHistory, value); }
        }

        /// <summary>
        /// Whether the form can be submitted
        /// </summary>
        public bool CanSubmit => !IsSubmitting && IsFormValid;

        /// <summary>
        /// Whether the form is valid for submission
        /// </summary>
        public bool IsFormValid => !string.IsNullOrWhiteSpace(Title) && 
                                   !string.IsNullOrWhiteSpace(Message);

        /// <summary>
        /// Available feedback categories
        /// </summary>
        public FeedbackCategory[] AvailableCategories => new[]
        {
            FeedbackCategory.BugReport,
            FeedbackCategory.FeatureRequest,
            FeedbackCategory.General,
            FeedbackCategory.Performance,
            FeedbackCategory.UI_UX,
            FeedbackCategory.Documentation
        };

        /// <summary>
        /// Command to submit feedback
        /// </summary>
        public ICommand SubmitFeedbackCommand { get; protected set; }

        /// <summary>
        /// Command to clear the form
        /// </summary>
        public ICommand ClearFormCommand { get; protected set; }

        /// <summary>
        /// Command to load feedback history
        /// </summary>
        public ICommand LoadFeedbackHistoryCommand { get; protected set; }

        protected FeedbackVMBase()
        {
            _selectedCategory = FeedbackCategory.General;
        }

        /// <summary>
        /// Abstract method to submit feedback
        /// </summary>
        public abstract void SubmitFeedback();

        /// <summary>
        /// Abstract method to clear the form
        /// </summary>
        public abstract void ClearForm();

        /// <summary>
        /// Abstract method to load feedback history
        /// </summary>
        public abstract void LoadFeedbackHistory();

        /// <summary>
        /// Validates the form and updates CanSubmit property
        /// </summary>
        protected virtual void ValidateForm()
        {
            OnPropertyChanged(nameof(IsFormValid));
            OnPropertyChanged(nameof(CanSubmit));
        }

        /// <summary>
        /// Shows status message for a specified duration
        /// </summary>
        protected virtual void ShowStatusMessage(string message, bool isError = false)
        {
            SubmitStatusMessage = message;
            IsSubmitStatusVisible = true;

            // Auto-hide after 5 seconds
            //TODO: Verify this implementation
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                DispatcherQueue.TryEnqueue(() =>
                {
                    IsSubmitStatusVisible = false;
                });
            });
        }

        public override void Dispose()
        {
            base.Dispose();
            UserFeedbackHistory?.Clear();
        }
    }
}