using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class KToastListVMBase : KToastViewModelBase
    {

        #region

        private ObservableCollection<KToastNotification> _kToastNotifications;
                                                                                                                             
        public ObservableCollection<KToastNotification> KToastNotifications
        {
            get
            {
                if (_kToastNotifications == null)
                {
                    _kToastNotifications = new ObservableCollection<KToastNotification>();
                }
                return _kToastNotifications;
            }
            set
            {
                _kToastNotifications = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public abstract void LoadControl();
        public abstract void UpdateKToastNotifications(ObservableCollection<KToastNotification> kToastNotifications);

        #endregion


        public KToastListVMBase()
        {
        }
    }
}
