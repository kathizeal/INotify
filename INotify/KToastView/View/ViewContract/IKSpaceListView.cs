using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastView.View.ViewContract
{
    public interface IKSpaceListView
    {
        void ShowPackagesFlyout(ObservableCollection<KPackageProfile> packages);
    }
}
