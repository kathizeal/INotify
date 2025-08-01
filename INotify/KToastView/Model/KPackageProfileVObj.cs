using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// User-friendly display name (uses AppDisplayName as fallback)
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(AppDisplayName) ? AppDisplayName : PackageId;

        /// <summary>
        /// Text representation of priority level
        /// </summary>
        public string PriorityText => Priority switch
        {
            Priority.High => "🔴 High",
            Priority.Medium => "🟡 Medium", 
            Priority.Low => "🟢 Low",
            _ => "⚫ None"
        };

        /// <summary>
        /// Creates a KPackageProfileVObj from InstalledAppInfo
        /// </summary>
        public static KPackageProfileVObj FromInstalledAppInfo(AppList.InstalledAppInfo appInfo, Priority priority = Priority.None, int notificationCount = 0)
        {
            return new KPackageProfileVObj
            {
                PackageId = GeneratePackageId(appInfo),
                PackageFamilyName = appInfo.PackageFamilyName ?? string.Empty,
                AppDisplayName = appInfo.DisplayName,
                AppDescription = $"Application: {appInfo.DisplayName}",
                Publisher = appInfo.Publisher ?? "Unknown",
                Priority = priority,
                NotificationCount = notificationCount,
                IsSelected = false,
                AppIcon = appInfo.Icon,
                LogoFilePath = string.Empty // Will be set when icon is saved
            };
        }

        private static string GeneratePackageId(AppList.InstalledAppInfo app)
        {
            switch (app.Type)
            {
                case AppList.AppType.UWPApplication:
                    return !string.IsNullOrEmpty(app.PackageFamilyName) ? app.PackageFamilyName : app.Name;

                case AppList.AppType.Win32Application:
                    if (!string.IsNullOrEmpty(app.ExecutablePath))
                    {
                        return System.IO.Path.GetFileNameWithoutExtension(app.ExecutablePath);
                    }
                    return app.DisplayName.Replace(" ", "").Replace(".", "").Replace("-", "");

                default:
                    return app.Name;
            }
        }
    }
}
