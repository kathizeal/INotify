using INotify.KToastView.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class AppSelectionViewModelBase : ToastViewModelBase
    {
        public ObservableCollection<KPackageProfileVObj> FilteredApps = new();
        public abstract void GetAppPackageProfile();
        public abstract void SyncAppPackageProfileWithInstalled();

        public abstract void AddSelectedAppsToCondition(AppSelectionEventArgs appSelectionEventArgs);
    }

    public class AppSelectionEventArgs : EventArgs
    {
        public SelectionTargetType TargetType { get; set; }
        public string CurrentTargetId { get; set; } 
        public IReadOnlyList<KPackageProfileVObj> SelectedApps { get; }

        public AppSelectionEventArgs(IEnumerable<KPackageProfileVObj> selectedApps, SelectionTargetType targetType,  string currentTargetId)
        {
            SelectedApps = selectedApps.ToList().AsReadOnly();
            TargetType = targetType;
            CurrentTargetId = currentTargetId;
        }
    }
    public enum SelectionTargetType
    {
        Priority,
        Space
    }

}
