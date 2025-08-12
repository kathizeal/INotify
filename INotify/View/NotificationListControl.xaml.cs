using INotify.Controls;
using INotify.KToastDI;
using INotify.KToastView.Model;
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
            set { SetValue(CurrentTargetTypeProperty, value);}
        }

        // Using a DependencyProperty as the backing store for CurrentTargetType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentTargetTypeProperty =
            DependencyProperty.Register("CurrentTargetType", typeof(SelectionTargetType), typeof(NotificationListControl), new PropertyMetadata(SelectionTargetType.Priority));

        public string SelectionTargetId
        {
            get { return (string)GetValue(SelectionTargetIdProperty); }
            set { SetValue(SelectionTargetIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionTargetId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionTargetIdProperty =
            DependencyProperty.Register("SelectionTargetId", typeof(string), typeof(NotificationListControl), new PropertyMetadata(default));

        public NotificationListControl()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _viewModel = KToastDIServiceProvider.Instance.GetService<NotificationListVMBase>();
            this.DataContext = _viewModel;
        }

        public void UpdateViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentTargetType = CurrentTargetType;
                _viewModel.SelectionTypeId = SelectionTargetId;
            }
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ToggleView();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.RefreshView();
        }

        private void TogglePackageGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is KPackageNotificationGroup group)
            {
                _viewModel?.TogglePackageGroup(group);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Dispose();
        }

        /// <summary>
        /// Converter function for toggling notification view visibility
        /// </summary>
        public Visibility ToggleNotificationView(bool isPackageView)
        {
            return isPackageView ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Converter function for toggling package view visibility
        /// </summary>
        public Visibility TogglePackageView(bool isPackageView)
        {
            return isPackageView ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
