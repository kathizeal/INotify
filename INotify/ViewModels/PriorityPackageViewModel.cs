using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;
using INotifyLibrary.Util.Enums;

namespace INotify.ViewModels
{
    /// <summary>
    /// ViewModel for priority package information with IKPriorityPackage implementation
    /// </summary>
    public class PriorityPackageViewModel : INotifyPropertyChanged, INotifyLibrary.Model.Contract.IKPriorityPackage
    {
        private string _packageId = string.Empty;
        private string _displayName = string.Empty;
        private string _publisher = string.Empty;
        private bool _isEnabled;
        private BitmapImage? _icon;
        private Priority _priority = Priority.None;
        private int _notificationCount;

        public string PackageId
        {
            get => _packageId;
            set { _packageId = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Publisher
        {
            get => _publisher;
            set { _publisher = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public BitmapImage? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public Priority Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }

        public int NotificationCount
        {
            get => _notificationCount;
            set { _notificationCount = value; OnPropertyChanged(); }
        }

        public string PriorityText => Priority switch
        {
            Priority.High => "?? High",
            Priority.Medium => "?? Medium", 
            Priority.Low => "?? Low",
            _ => "? None"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}