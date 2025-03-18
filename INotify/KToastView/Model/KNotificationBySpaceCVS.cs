using INotifyLibrary.Util.Enums;
using System.Collections.ObjectModel;
using WinCommon.Util;

namespace INotify.KToastView.Model
{
    public class KNotificationBySpaceCVS : ObservableObject
    {
        private ObservableCollection<KPackageProfileVObj> _kPackageProfileList;

        public ObservableCollection<KPackageProfileVObj> KPackageProfileList
        {
            get { return _kPackageProfileList; }
            set { SetProperty(ref _kPackageProfileList, value); }
        }


        private KSpaceVObj _Space;

        public KSpaceVObj Space
        {
            get { return _Space; }
            set => SetIfDifferent(ref _Space, value);
        }

        public ViewType ViewType { get; set; }
        public string HeaderId { get; set; }


        private string _DisplayName;

        public string DisplayName
        {
            get { return _DisplayName; }
            set => SetIfDifferent(ref _DisplayName, value);
        }
    }
}
