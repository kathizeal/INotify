using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace INotify.KToastViewModel.ViewModel
{
    /// <summary>
    /// Main application state and UI mode management
    /// </summary>
    public class ApplicationViewModel : INotifyPropertyChanged
    {
        private bool _isNormalMode = true;
        private bool _isMiniWidgetMode = false;
        private string _selectedMenuItem = "Priority";
        private int _totalNotifications;
        private int _totalApps;
        private int _totalSpaces;

        // View visibility properties
        private bool _isPriorityViewVisible = true;
        private bool _isSpaceViewVisible = false;
        private bool _isAllNotificationsViewVisible = false;
        private bool _isAllApplicationsViewVisible = false;

        public bool IsNormalMode
        {
            get => _isNormalMode;
            set 
            { 
                _isNormalMode = value; 
                _isMiniWidgetMode = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMiniWidgetMode));
            }
        }

        public bool IsMiniWidgetMode
        {
            get => _isMiniWidgetMode;
            set 
            { 
                _isMiniWidgetMode = value; 
                _isNormalMode = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNormalMode));
            }
        }

        public string SelectedMenuItem
        {
            get => _selectedMenuItem;
            set { _selectedMenuItem = value; OnPropertyChanged(); }
        }

        public int TotalNotifications
        {
            get => _totalNotifications;
            set { _totalNotifications = value; OnPropertyChanged(); }
        }

        public int TotalApps
        {
            get => _totalApps;
            set { _totalApps = value; OnPropertyChanged(); }
        }

        public int TotalSpaces
        {
            get => _totalSpaces;
            set { _totalSpaces = value; OnPropertyChanged(); }
        }

        // View visibility properties
        public bool IsPriorityViewVisible
        {
            get => _isPriorityViewVisible;
            set { _isPriorityViewVisible = value; OnPropertyChanged(); }
        }

        public bool IsSpaceViewVisible
        {
            get => _isSpaceViewVisible;
            set { _isSpaceViewVisible = value; OnPropertyChanged(); }
        }

        public bool IsAllNotificationsViewVisible
        {
            get => _isAllNotificationsViewVisible;
            set { _isAllNotificationsViewVisible = value; OnPropertyChanged(); }
        }

        public bool IsAllApplicationsViewVisible
        {
            get => _isAllApplicationsViewVisible;
            set { _isAllApplicationsViewVisible = value; OnPropertyChanged(); }
        }

        public void SwitchToNormalMode() => IsNormalMode = true;
        public void SwitchToMiniWidget() => IsMiniWidgetMode = true;

        // View switching methods
        public void ShowPriorityView()
        {
            IsPriorityViewVisible = true;
            IsSpaceViewVisible = false;
            IsAllNotificationsViewVisible = false;
            IsAllApplicationsViewVisible = false;
            SelectedMenuItem = "Priority";
        }

        public void ShowSpaceView()
        {
            IsPriorityViewVisible = false;
            IsSpaceViewVisible = true;
            IsAllNotificationsViewVisible = false;
            IsAllApplicationsViewVisible = false;
            SelectedMenuItem = "Space";
        }

        public void ShowAllNotificationsView()
        {
            IsPriorityViewVisible = false;
            IsSpaceViewVisible = false;
            IsAllNotificationsViewVisible = true;
            IsAllApplicationsViewVisible = false;
            SelectedMenuItem = "Notifications";
        }

        public void ShowAllApplicationsView()
        {
            IsPriorityViewVisible = false;
            IsSpaceViewVisible = false;
            IsAllNotificationsViewVisible = false;
            IsAllApplicationsViewVisible = true;
            SelectedMenuItem = "Applications";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}