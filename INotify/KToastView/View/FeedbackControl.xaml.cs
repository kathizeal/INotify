using INotify.KToastDI;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace INotify.View
{
    public sealed partial class FeedbackControl : UserControl
    {
        public FeedbackVMBase ViewModel { get; private set; }

        public FeedbackControl()
        {
            InitializeViewModel();
            this.InitializeComponent();
            this.Loaded += UserControl_Loaded;
            this.Unloaded += UserControl_Unloaded;
        }

        private void InitializeViewModel()
        {
            ViewModel = KToastDIServiceProvider.Instance.GetService<FeedbackVMBase>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize data when control loads
            ViewModel?.LoadFeedbackHistory();
            
            // Set up UI bindings manually for now
            UpdateUIFromViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up when control unloads
            ViewModel?.Dispose();
        }

        private void UpdateUIFromViewModel()
        {
            if (ViewModel != null)
            {
                // Set up property change notifications
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update UI when ViewModel properties change
            switch (e.PropertyName)
            {
                case nameof(ViewModel.IsSubmitStatusVisible):
                    StatusInfoBar.IsOpen = ViewModel.IsSubmitStatusVisible;
                    break;
                case nameof(ViewModel.SubmitStatusMessage):
                    StatusInfoBar.Message = ViewModel.SubmitStatusMessage ?? "";
                    break;
                case nameof(ViewModel.IsSubmitting):
                    SubmitButton.Content = ViewModel.IsSubmitting ? "Submitting..." : "Submit Feedback";
                    SubmitButton.IsEnabled = ViewModel.CanSubmit;
                    ClearButton.IsEnabled = !ViewModel.IsSubmitting;
                    break;
                case nameof(ViewModel.Title):
                    if (TitleTextBox.Text != ViewModel.Title)
                        TitleTextBox.Text = ViewModel.Title ?? "";
                    break;
                case nameof(ViewModel.Message):
                    if (MessageTextBox.Text != ViewModel.Message)
                        MessageTextBox.Text = ViewModel.Message ?? "";
                    break;
                case nameof(ViewModel.Email):
                    if (EmailTextBox.Text != ViewModel.Email)
                        EmailTextBox.Text = ViewModel.Email ?? "";
                    break;
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Update ViewModel from UI
                ViewModel.Title = TitleTextBox.Text ?? "";
                ViewModel.Message = MessageTextBox.Text ?? "";
                ViewModel.Email = EmailTextBox.Text ?? "";
                ViewModel.SelectedCategory = (FeedbackCategory)CategoryComboBox.SelectedIndex;

                // Submit feedback
                ViewModel.SubmitFeedback();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ClearForm();
                
                // Clear UI
                TitleTextBox.Text = "";
                MessageTextBox.Text = "";
                EmailTextBox.Text = "";
                CategoryComboBox.SelectedIndex = 2; // General
                StatusInfoBar.IsOpen = false;
            }
        }
    }
}