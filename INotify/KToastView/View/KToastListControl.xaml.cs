using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.KToastView.View
{
    public sealed partial class KToastListControl : UserControl, IKToastListView
    {
        private KToastListVMBase _VM;

        public CollectionViewSource KToastCollectionViewSource => CVS;

        public DataTemplate ToastTemplate => Resources["KToastTemplate"] as DataTemplate;

        public DataTemplate PackageTemplate => Resources["KPackageTemplate"] as DataTemplate;

        public DataTemplate SpaceTemplate => Resources["KSpaceTemplate"] as DataTemplate;

        public DataTemplate NotificationByPackageTemplate => Resources["KNotificationByPackageTemplate"] as DataTemplate;

        public DataTemplate NotificationBySpaceTemplate => Resources["KNotificationBySpaceTemplate"] as DataTemplate;

        public KToastListControl()
        {
            _VM = KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();
            _VM.View = this;
            _VM.CurrentViewType = ViewType.Package;
            this.InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _VM.UpdateViewType(ViewType.Package); // Temporary
            UpdateTemplatePanelAndSource(ViewType.Package);
            //TODO : Based on the user setting preference this should be done.
        }

        public void UpdateToastView(ViewType viewType)  // Temporary
        {
            _VM.UpdateViewType(viewType); 
            UpdateTemplatePanelAndSource(viewType);
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
            UpdateTemplatePanelAndSource(ViewType.Package);
        }

        private void Priority_Click(object sender, RoutedEventArgs e)
        {
            _VM.UpdateViewType(ViewType.Priority);
        }

        private void ToastListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            switch (_VM.CurrentViewType)
            {
                case ViewType.All:
                    if (e.ClickedItem is KToastVObj toast)
                    {

                    }
                    break;
                case ViewType.Package:
                    if (e.ClickedItem is KPackageProfileVObj package)
                    {
                        _VM.GetKToastNotificationByPackageId(package.PackageId);
                    }
                    break;
            }
        }

        private void StackPanel_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is StackPanel sp)
            {
                if (sp.Tag is KPackageProfileVObj pp)
                {
                    _VM.GetKToastNotificationByPackageId(pp.PackageId);
                }
            }
        }

        private void HorizontalContentLV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedItem = e.AddedItems[0];
                switch (_VM.CurrentViewType)
                {
                    case ViewType.Package:
                        if (selectedItem is KPackageProfileVObj package)
                        {
                            _VM.GetKToastNotificationByPackageId(package.PackageId);
                        }
                        break;
                    case ViewType.Space:
                        if (selectedItem is KSpaceVObj space)
                        {
                            _VM.GetPackagesBySpaceById(space.SpaceId);
                        }
                        break;
                        // Add other cases as needed
                }
            }
        }

        private void UpdateTemplatePanelAndSource(ViewType viewType)
        {

            switch (viewType)
            {
                case ViewType.Package:
                    {
                        TemplatesForPackageView();
                    }
                    break;

                case ViewType.Priority:
                    {

                    }
                    break;
                case ViewType.Filters:
                    {

                    }
                    break;
                case ViewType.Space:
                    {
                    }
                    break;
            }

            void TemplatesForPackageView()
            {
                HorizontalContentLV.ItemsSource = _VM.KPackageProfilesList;
            }
        }

        private void HorizontalContentLV_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            var clickedItem = (e.OriginalSource as FrameworkElement)?.DataContext as KPackageProfileVObj;
            if (clickedItem != null)
            {
                Flyout flyout = new Flyout();
                KSpaceControl kSpaceControl = new KSpaceControl();
                kSpaceControl.PackageId = clickedItem.PackageId;
                flyout.Content = kSpaceControl;
                flyout.ShowAt(HorizontalContentLV);
            }
        }

        public void GetPackageBySpace(string spaceId)
        {
            _VM.GetPackagesBySpaceById(spaceId);
        }
    }
}
