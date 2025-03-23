using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.KToastView.View
{
    public sealed partial class KToastListControl : UserControl, IKToastListView
    {
        private KToastListVMBase _VM;

        public CollectionViewSource KToastCollectionViewSource => CVS;

        public ListView KToastListView => ToastListView;

        public DataTemplate ToastTemplate => Resources["KToastTemplate"] as DataTemplate;

        public DataTemplate PackageTemplate => Resources["KPackageTemplate"] as DataTemplate;

        public DataTemplate SpaceTemplate => Resources["KSpaceTemplate"] as DataTemplate;

        public DataTemplate NotificationByPackageTemplate => Resources["KNotificationByPackageTemplate"] as DataTemplate;

        public DataTemplate NotificationBySpaceTemplate => Resources["KNotificationBySpaceTemplate"] as DataTemplate;

        public KToastListControl()
        {
            _VM = KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();
            _VM.View = this;
            this.InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _VM.LoadControl();
        }

        public void UpdateNotificationsList(ObservableCollection<KToastNotification> currentSystemNotifications)
        {
            _VM.UpdateKToastNotifications(currentSystemNotifications);
        }

        public void AddToastControl(KToastVObj notification)
        {
            _VM.UpdateKToastNotification(notification);
        }

        private void All_Click(object sender, RoutedEventArgs e)
        {
            _VM.UpdateViewType(ViewType.All);
        }

        private void Space_Click(object sender, RoutedEventArgs e)
        {
            _VM.UpdateViewType(ViewType.Space);
        }

        private void AppBy_Click(object sender, RoutedEventArgs e)
        {
            _VM.UpdateViewType(ViewType.Package);
        }

        private void Priority_Click(object sender, RoutedEventArgs e)
        {
            _VM.UpdateViewType(ViewType.Priority);
        }

        private void ToastListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            switch(_VM.CurrentViewType)
            {
                case ViewType.All:
                   if(e.ClickedItem is KToastVObj toast)
                    {

                    }
                    break;
                case ViewType.Package:
                    if(e.ClickedItem is KPackageProfileVObj package)
                    {
                        _VM.GetKToastNotificationByPackageId(package.PackageId);
                    }
                    break;
            }
        }

        private void StackPanel_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if(sender is StackPanel sp)
            {
                if(sp.Tag is KPackageProfileVObj pp)
                {
                    _VM.GetKToastNotificationByPackageId(pp.PackageId);
                }
            }
        }
    }
}
