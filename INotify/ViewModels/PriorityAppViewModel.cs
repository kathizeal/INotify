using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace INotify
{
    /// <summary>
    /// ViewModel for Priority Apps in the UI
    /// </summary>
    public class PriorityAppViewModel : INotifyPropertyChanged
    {
        private string _appId = string.Empty;
        private string _displayName = string.Empty;
        private string _publisher = string.Empty;
        private BitmapImage? _icon;
        private bool _isEnabled;

        public string AppId
        {
            get => _appId;
            set
            {
                _appId = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        public string Publisher
        {
            get => _publisher;
            set
            {
                _publisher = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}