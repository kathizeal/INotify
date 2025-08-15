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
            try
            {
                InitializeComponent();
                InitializeViewModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in NotificationListControl constructor: {ex.Message}");
                // In case of error, we'll initialize the ViewModel later
            }
        }

        private void InitializeViewModel()
        {
            try
            {
                _viewModel = KToastDIServiceProvider.Instance.GetService<NotificationListVMBase>();
                if (_viewModel != null)
                {
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Warning: NotificationListVMBase service not available from DI container");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ViewModel in NotificationListControl: {ex.Message}");
                // ViewModel will be null, but the control won't crash
            }
        }

        private void EnsureViewModelInitialized()
        {
            if (_viewModel == null)
            {
                InitializeViewModel();
            }
        }

        public void UpdateViewModel()
        {
            EnsureViewModelInitialized();
            if (_viewModel != null)
            {
                _viewModel.CurrentTargetType = CurrentTargetType;
                _viewModel.SelectionTypeId = SelectionTargetId;
            }
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            _viewModel?.ToggleView();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            _viewModel?.RefreshView();
        }

        private void TogglePackageGroup_Click(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            if (sender is Button button && button.Tag is KPackageNotificationGroup group)
            {
                _viewModel?.TogglePackageGroup(group);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            UpdateViewModel();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing ViewModel: {ex.Message}");
            }
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
