using INotify.KToastView.Model;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using System.Collections.ObjectModel;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class NotificationListVMBase : ToastViewModelBase
    {
        private SelectionTargetType _currentTargetType;
        private string _selectionTypeId;
        private bool _isPackageView;
        private ObservableCollection<KToastBObj> _notifications = new();
        private ObservableCollection<KPackageProfile> _packages = new();
        private bool _isLoading;

        /// <summary>
        /// Current target type (Priority or Space)
        /// </summary>
        public SelectionTargetType CurrentTargetType
        {
            get => _currentTargetType;
            set { SetIfDifferent(ref _currentTargetType, value); LoadNotifications(); }
        }

        /// <summary>
        /// Selection type ID (e.g., "High", "Medium", "Low" for Priority; "Space1", "Space2", "Space3" for Space)
        /// </summary>
        public string SelectionTypeId
        {
            get => _selectionTypeId;
            set { SetIfDifferent(ref _selectionTypeId, value); LoadNotifications(); }
        }

        /// <summary>
        /// Whether to show package view (true) or notification view (false)
        /// </summary>
        public bool IsPackageView
        {
            get => _isPackageView;
            set { SetIfDifferent(ref _isPackageView, value); LoadNotifications(); }
        }

        /// <summary>
        /// Collection of notifications (used when IsPackageView is false)
        /// </summary>
        public ObservableCollection<KToastBObj> Notifications
        {
            get => _notifications;
            set { SetIfDifferent(ref _notifications, value); }
        }

        /// <summary>
        /// Collection of packages (used when IsPackageView is true)
        /// </summary>
        public ObservableCollection<KPackageProfile> Packages
        {
            get => _packages;
            set { SetIfDifferent(ref _packages, value); }
        }

        /// <summary>
        /// Whether data is currently being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { SetIfDifferent(ref _isLoading, value); }
        }

        /// <summary>
        /// Toggle button text for view switching
        /// </summary>
        public string ToggleButtonText => IsPackageView ? "Show Notifications" : "Show Packages";

        /// <summary>
        /// Total count for display
        /// </summary>
        public int TotalCount => IsPackageView ? Packages?.Count ?? 0 : Notifications?.Count ?? 0;

        /// <summary>
        /// Display text for current view
        /// </summary>
        public string ViewDisplayText => IsPackageView 
            ? $"{TotalCount} Package(s)" 
            : $"{TotalCount} Notification(s)";

        /// <summary>
        /// Abstract method to load notifications based on current settings
        /// </summary>
        public abstract void LoadNotifications();

        /// <summary>
        /// Abstract method to toggle between package and notification view
        /// </summary>
        public abstract void ToggleView();

        /// <summary>
        /// Abstract method to refresh the current view
        /// </summary>
        public abstract void RefreshView();

        protected NotificationListVMBase()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            Notifications?.Clear();
            Packages?.Clear();
        }
    }
}