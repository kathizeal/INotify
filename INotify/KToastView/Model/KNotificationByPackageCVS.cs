using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinCommon.Util;

namespace INotify.KToastView.Model
{
    public class KNotificationByPackageCVS : ObservableCollection<KToastVObj>, INotifyPropertyChanged
    {       

        private KPackageProfileVObj _Profile;

        public KPackageProfileVObj Profile
        {
            get { return _Profile; }
            set => SetProperty(ref _Profile, value);
        }

        public ViewType ViewType { get; set; }
        public string HeaderId { get; set; }


        private string _DisplayName;

        public string DisplayName
        {
            get { return _DisplayName; }
            set => SetProperty(ref _DisplayName, value);        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected void SetProperty<T>(ref T field, T val, [CallerMemberName] string propertyName = null)
        {
            field = val;
            OnPropertyChanged(propertyName);
        }

    }
}
