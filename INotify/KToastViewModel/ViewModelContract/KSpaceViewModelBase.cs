using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotifyLibrary.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class KSpaceViewModelBase : KToastViewModelBase
    {

        public IKSpaceListView SpaceView { get; set; }

        private ObservableCollection<KSpaceVObj> _KSpaceList;

        public ObservableCollection<KSpaceVObj> KSpaceList
        {
            get
            {
                if (_KSpaceList == null)
                { 
                        _KSpaceList = new ObservableCollection<KSpaceVObj>();
                }
                return _KSpaceList;
            }
            set { _KSpaceList = value; OnPropertyChanged(); }
        }
	 

        public abstract void GetAllSpace(string userId);
        public abstract void AddToSpace(string packageId, string spaceId, string userId);
        public abstract void RemoveFromSpace(string packageId, string spaceId, string userId);
        public abstract void GetPackagesBySpace(string spaceId, string userId);

        protected KSpaceViewModelBase()
        {
        }
    }
}
