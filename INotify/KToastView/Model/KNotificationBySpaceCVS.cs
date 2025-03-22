using INotifyLibrary.Util.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinCommon.Util;

namespace INotify.KToastView.Model
{
    public class KNotificationBySpaceCVS : ObservableCollection<KPackageProfileVObj> , INotifyPropertyChanged
    {

        private KSpaceVObj _Space;

        public KSpaceVObj Space
        {
            get { return _Space; }
            set => SetProperty(ref _Space, value);
        }

        public ViewType ViewType { get; set; }
        public string HeaderId { get; set; }


        private string _DisplayName;

        public string DisplayName
        {
            get { return _DisplayName; }
            set => SetProperty(ref _DisplayName, value);
        }

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
