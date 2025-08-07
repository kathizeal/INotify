using INotify.Controls;
using INotify.KToastDI;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.View
{
    public sealed partial class NotificationListControl : UserControl
    {
        private NotificationListVMBase _viewModel;

        public SelectionTargetType CurrentTargetType
        {
            get { return (SelectionTargetType)GetValue(CurrentTargetTypeProperty); }
            set { SetValue(CurrentTargetTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentTargetType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentTargetTypeProperty =
            DependencyProperty.Register("CurrentTargetType", typeof(SelectionTargetType), typeof(NotificationListControl), new PropertyMetadata(default));

        public string SelectionTargetId
        {
            get { return (string)GetValue(SelectionTypeIdProperty); }
            set { SetValue(SelectionTypeIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionTypeId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionTypeIdProperty =
            DependencyProperty.Register("SelectionTargetId", typeof(string), typeof(NotificationListControl), new PropertyMetadata(default));

        public NotificationListControl()
        {
            InitializeViewModel();
            this.InitializeComponent();
        }

        private void InitializeViewModel()
        {
            _viewModel = KToastDIServiceProvider.Instance.GetService<NotificationListVMBase>();
        }

        private void UpdateViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentTargetType = CurrentTargetType;
                _viewModel.SelectionTypeId = SelectionTargetId;
            }
        }

        public Visibility TogglePackageView(bool ispackageView)
        {
            
            return ispackageView ? Visibility.Visible : Visibility.Collapsed;
        }
        public Visibility ToggleNotificationView(bool ispackageView)
        {

            return ispackageView ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleView();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.RefreshView();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Dispose();
        }
    }
}
