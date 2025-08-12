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
        //private KPackageProfile _packageProfile;
        private bool _isExpanded = false;
        private int _notificationCount;
        private string _displayName = "Unknown Package";
        private string _packageFamilyName = "";
        private string _notificationCountText = "No notifications";
        private string _iconPath = "";

        /// <summary>
        /// Display name for the package
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>
        /// Package Family Name for identification
        /// </summary>
        public string PackageFamilyName
        {
            get => _packageFamilyName;
            set => SetProperty(ref _packageFamilyName, value);
        }

        /// <summary>
        /// Notification count display text
        /// </summary>
        public string NotificationCountText
        {
            get => _notificationCountText;
            set => SetProperty(ref _notificationCountText, value);
        }

        /// <summary>
        /// Icon path for the package
        /// </summary>
        public string IconPath
        {
            get => _iconPath;
            set => SetProperty(ref _iconPath, value);
        }

        /// <summary>
        /// The package profile (header)
        /// </summary>
        //public KPackageProfile PackageProfile
        //{
        //    get => _packageProfile;
        //    set 
        //    {
        //        SetProperty(ref _packageProfile, value);
        //        {
        //            UpdateDependentProperties();
        //        }
        //    }
        //}

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
                set 
                {
                    SetProperty(ref _notificationCount, value);
                    {
                        UpdateNotificationCountText();
                    }
                }
            }

            private void UpdateDependentProperties()
            {
               
            }

            private void UpdateNotificationCountText()
            {
                NotificationCountText = NotificationCount switch
                {
                    0 => "No notifications",
                    1 => "1 notification",
                    _ => $"{NotificationCount} notifications"
                };
            }

            private void UpdateNotificationCount()
            {
                NotificationCount = Count;
            }

        public KPackageNotificationGroup(string appDisplayName, string packageFamilyName, string logoFilePath)
        {
            DisplayName = appDisplayName ?? "Unknown Package";
            PackageFamilyName = packageFamilyName ?? "";
            IconPath = logoFilePath ?? ""; 
            UpdateNotificationCount();

            // Subscribe to collection changes to update count
            CollectionChanged += (s, e) => UpdateNotificationCount();
        }

        /// <summary>
        /// Toggle the expanded state
        /// </summary>
        public void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>To set a new value to the property and raise <see cref="PropertyChanged"/> event for the property</summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference property (ref type) which will contain the actual value</param>
        /// <param name="val">New value to update</param>
        /// <param name="propertyName">Name of the property for which <see cref="PropertyChanged"/> event will be raised</param>
        protected void SetProperty<T>(ref T field, T val, [CallerMemberName] string propertyName = null)
        {
            field = val;
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// To set new value to the property only if it is different from the previous value<para/>
        /// It raises <see cref="PropertyChanged"/> event only if the property value is updated
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Reference property (ref type) which will contain the actual value</param>
        /// <param name="val">New value to update</param>
        /// <param name="propertyName">Name of the property for which <see cref="PropertyChanged"/> event will be raised</param>
        protected bool SetIfDifferent<T>(ref T field, T val, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, val)) { return false; }
            field = val;
            OnPropertyChanged(propertyName);
            return true;
        }
        /// <summary>
        /// Sets the PropertyChanged event handler to null
        /// </summary>
        protected void ResetPropertyChangedHandler()
        {
            PropertyChanged = null;
        }
    }
}