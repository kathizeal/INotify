using AppList;
using INotify.Util;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Media.Imaging;

namespace INotify.KToastView.Model
{
    /// <summary>
    /// Enhanced package profile view object with priority and selection support for app selection flyouts
    /// </summary>
    public class KPackageProfileVObj : KPackageProfile
    {
        private BitmapImage _appIcon;
        private Priority _priority = Priority.None;
        private bool _isSelected;
        private int _notificationCount;
        private string _publisher = string.Empty;

        public BitmapImage AppIcon
        {
            get { return _appIcon; }
            set { SetIfDifferent(ref _appIcon, value); }
        }

        /// <summary>
        /// Priority level for this package
        /// </summary>
        public Priority Priority
        {
            get => _priority;
            set { SetIfDifferent(ref _priority, value); OnPropertyChanged(nameof(PriorityText)); }
        }

        /// <summary>
        /// Whether this package is selected in multi-select scenarios
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { SetIfDifferent(ref _isSelected, value); }
        }

        /// <summary>
        /// Number of notifications for this package
        /// </summary>
        public int NotificationCount
        {
            get => _notificationCount;
            set { SetIfDifferent(ref _notificationCount, value); }
        }

        /// <summary>
        /// Publisher/Developer of the application
        /// </summary>
        public string Publisher
        {
            get => _publisher;
            set { SetIfDifferent(ref _publisher, value); }
        }


        private string _DisplayName;
        /// <summary>
        /// User-friendly display name (uses AppDisplayName as fallback)
        /// </summary>
        public string DisplayName
        {
            get => _DisplayName;
            set => SetIfDifferent(ref _DisplayName, value);
        }

        private void SetDisplayName()
        {
            if (string.IsNullOrEmpty(_DisplayName))
            {
                _DisplayName = AppDisplayName;
            }
        }
        /// <summary>
        /// Text representation of priority level
        /// </summary>
        public string PriorityText => Priority switch
        {
            Priority.High => "High",
            Priority.Medium => "Medium",
            Priority.Low => "Low",
            _ => "None"
        };

        /// <summary>
        /// Creates a KPackageProfileVObj from InstalledAppInfo
        /// </summary>
        public void PopulateInstalledAppInfo(InstalledAppInfo appInfo, int notificationCount = 0)
        {
            PackageFamilyName = appInfo.PackageFamilyName ?? string.Empty;
            AppDisplayName = appInfo.DisplayName;
            AppDescription = $"Application: {appInfo.DisplayName}";
            Publisher = appInfo.Publisher ?? "Unknown";
            Priority = Priority.None;
            NotificationCount = notificationCount;
            IsSelected = false;
            AppIcon = appInfo.Icon;
            LogoFilePath = string.Empty; // Will be set when icon is saved
            SetDisplayName();
        }

        public void PopulateInstalledAppInfo(KPackageProfile packageProfile, int notificationCount = 0)
        {
            PackageFamilyName = packageProfile.PackageFamilyName ?? string.Empty;
            AppDisplayName = packageProfile.AppDisplayName;
            AppDescription = $"Application: {packageProfile.AppDescription}";
            Publisher = packageProfile.Publisher ?? "Unknown";
            Priority = Priority.None;
            NotificationCount = notificationCount;
            IsSelected = false;
            AppIcon = default; ;
            LogoFilePath = string.Empty; // Will be set when icon is saved
            SetDisplayName();
        }


    }
}
