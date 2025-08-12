using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using INotify.KToastDI;
using INotify.KToastViewModel.ViewModelContract;
using INotify.KToastView.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System.Collections.ObjectModel;
using INotify.KToastView.View.ViewContract;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.KToastView.View
{
    public sealed partial class KSpaceControl : UserControl , IKSpaceListView
    {
        public KSpaceViewModelBase _VM;
        private KSpaceVObj _selectedSpace;

        public event Action<string> SpaceSelected;

        public KSpaceControl()
        {
            this.InitializeComponent();
            _VM = KToastDIServiceProvider.Instance.GetService<KSpaceViewModelBase>();
            _VM.SpaceView = this;

        }

        public string PackageFamilyName { get; set; }

        private void SpaceList_ItemClick(object sender, ItemClickEventArgs e)
        {
            _selectedSpace = e.ClickedItem as KSpaceVObj;
            if (_selectedSpace != null)
            {
                SpaceSelected?.Invoke(_selectedSpace.SpaceId);
                Flyout flyout = new Flyout();
                StackPanel panel = new StackPanel();

                Button addButton = new Button { Content = "Add to this space" };
                addButton.Click += AddButton_Click;
                panel.Children.Add(addButton);

                Button removeButton = new Button { Content = "Remove from this space" };
                removeButton.Click += RemoveButton_Click;
                panel.Children.Add(removeButton);

                Button listPackagesButton = new Button { Content = "List all packages from this space" };
                listPackagesButton.Click += ListPackagesButton_Click;
                panel.Children.Add(listPackagesButton);

                flyout.Content = panel;
                flyout.ShowAt(SpaceList);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PackageFamilyName))
            {
                _VM.AddToSpace(PackageFamilyName, _selectedSpace.SpaceId,INotifyConstant.CurrentUser);
            }
            // Logic to add a package to the selected space
            // You can open another flyout or dialog to get the package details
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PackageFamilyName))
            {
                _VM.RemoveFromSpace(PackageFamilyName, _selectedSpace.SpaceId, INotifyConstant.CurrentUser);
            }
            // Logic to remove a package from the selected space
            // You can open another flyout or dialog to get the package details
        }

        private void ListPackagesButton_Click(object sender, RoutedEventArgs e)
        {
            _VM.GetPackagesBySpace(_selectedSpace.SpaceId, INotifyConstant.CurrentUser);
        }

        public void ShowPackagesFlyout(ObservableCollection<KPackageProfile> packages)
        {
            Flyout flyout = new Flyout();
            ListView listView = new ListView
            {
                ItemsSource = packages,
                ItemTemplate = (DataTemplate)Resources["PackageDataTemplate"]
            };
            flyout.Content = listView;
            flyout.ShowAt(SpaceList);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _VM.GetAllSpace(INotifyConstant.CurrentUser);
        }
    }
}
