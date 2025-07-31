using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using INotify.KToastView.Model;

namespace INotify.ViewModels
{
    /// <summary>
    /// ViewModel for space information with associated packages
    /// </summary>
    public class SpaceViewModel : INotifyPropertyChanged
    {
        private string _spaceId = string.Empty;
        private string _displayName = string.Empty;
        private string _description = string.Empty;
        private BitmapImage? _icon;
        private int _packageCount;
        private int _notificationCount;
        private bool _isActive = true;

        public string SpaceId
        {
            get => _spaceId;
            set { _spaceId = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public BitmapImage? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public int PackageCount
        {
            get => _packageCount;
            set { _packageCount = value; OnPropertyChanged(); }
        }

        public int NotificationCount
        {
            get => _notificationCount;
            set { _notificationCount = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public ObservableCollection<KPackageProfileVObj> Packages { get; set; } = new();
        public ObservableCollection<KToastVObj> Notifications { get; set; } = new();

        public string StatusText => $"{PackageCount} apps, {NotificationCount} notifications";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}