using INotify.KToastView.Model;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinCommon.Util;

namespace INotify.KToastView.Model
{
    /// <summary>
    /// Grouped model for package with its notifications - used in CollectionViewSource for grouping
    /// </summary>
    public class KPackageNotificationGroup : ObservableCollection<KToastBObj>, INotifyPropertyChanged
    {
        private KPackageProfile _packageProfile;
        private bool _isExpanded = false;
        private int _notificationCount;

        /// <summary>
        /// The package profile (header)
        /// </summary>
        public KPackageProfile PackageProfile
        {
            get => _packageProfile;
            set => SetProperty(ref _packageProfile, value);
        }

        /// <summary>
        /// Whether the group is expanded to show notifications
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// Number of notifications in this package
        /// </summary>
        public int NotificationCount
        {
            get => _notificationCount;
            set => SetProperty(ref _notificationCount, value);
        }

        /// <summary>
        /// Display name for the package
        /// </summary>
        public string DisplayName => PackageProfile?.AppDisplayName ?? "Unknown Package";

        /// <summary>
        /// Package Family Name for identification
        /// </summary>
        public string PackageFamilyName => PackageProfile?.PackageFamilyName ?? "";

        /// <summary>
        /// Notification count display text
        /// </summary>
        public string NotificationCountText => NotificationCount switch
        {
            0 => "No notifications",
            1 => "1 notification",
            _ => $"{NotificationCount} notifications"
        };

        /// <summary>
        /// Icon path for the package
        /// </summary>
        public string IconPath => PackageProfile?.LogoFilePath ?? "";

        public KPackageNotificationGroup(KPackageProfile packageProfile)
        {
            PackageProfile = packageProfile;
            UpdateNotificationCount();

            // Subscribe to collection changes to update count
            CollectionChanged += (s, e) => UpdateNotificationCount();
        }

        private void UpdateNotificationCount()
        {
            NotificationCount = Count;
            OnPropertyChanged(nameof(NotificationCountText));
        }

        /// <summary>
        /// Toggle the expanded state
        /// </summary>
        public void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}