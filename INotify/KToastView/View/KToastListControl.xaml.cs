using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.KToastView.View
{
    public sealed partial class KToastListControl : UserControl, IKToastListView
    {
        private KToastListVMBase _VM;

        public CollectionViewSource KToastCollectionViewSource => null; // Not used in this implementation

        public DataTemplate ToastTemplate => Resources["NotificationItemTemplate"] as DataTemplate;

        public DataTemplate PackageTemplate => null; // Not used in this implementation

        public DataTemplate SpaceTemplate => null; // Not used in this implementation

        public DataTemplate NotificationByPackageTemplate => null; // Not used in this implementation

        public DataTemplate NotificationBySpaceTemplate => null; // Not used in this implementation

        public KToastListControl()
        {
            this.InitializeComponent();
            _VM = KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();
            _VM.View = this;
        }

        public Visibility EmptyState(int count)
        {
            return count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public Visibility FilterPanelVisibility(bool visible)
        {
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load initial notifications when control is loaded
                _VM.LoadInitialNotifications();

                // Subscribe to scroll events for infinite scrolling - use this to get the ListView after InitializeComponent
                var scrollViewer = GetScrollViewer(this);
                if (scrollViewer != null)
                {
                    scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the UI
                System.Diagnostics.Debug.WriteLine($"Error in UserControl_Loaded: {ex.Message}");
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Unsubscribe from scroll events
                var scrollViewer = GetScrollViewer(this);
                if (scrollViewer != null)
                {
                    scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
                }

                // Dispose ViewModel if needed
                if (_VM is IDisposable disposableVM)
                {
                    disposableVM.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UserControl_Unloaded: {ex.Message}");
            }
        }

        #region Event Handlers

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _VM?.RefreshNotifications();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshButton_Click: {ex.Message}");
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _VM.IsFilterPanelVisible = !_VM.IsFilterPanelVisible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FilterButton_Click: {ex.Message}");
            }
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    _VM?.ApplyFilters();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SearchBox_KeyDown: {ex.Message}");
            }
        }

        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _VM?.ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyFiltersButton_Click: {ex.Message}");
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _VM?.ClearFilters();
                
                // Clear the date pickers
                if (SpecificDatePicker != null)
                    SpecificDatePicker.Date = null;

                if (FromDatePicker != null)
                    FromDatePicker.Date = null;

                if (ToDatePicker != null)
                    ToDatePicker.Date = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearFiltersButton_Click: {ex.Message}");
            }
        }

        private void SpecificDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            try
            {
                if (_VM != null)
                {
                    _VM.SelectedDate = args.NewDate;
                    
                    // Clear date range when specific date is selected
                    if (args.NewDate.HasValue)
                    {
                        _VM.FromDate = null;
                        _VM.ToDate = null;
                        
                        if (FromDatePicker != null)
                            FromDatePicker.Date = null;
                        if (ToDatePicker != null)
                            ToDatePicker.Date = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SpecificDatePicker_DateChanged: {ex.Message}");
            }
        }

        private void FromDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            try
            {
                if (_VM != null)
                {
                    _VM.FromDate = args.NewDate;
                    
                    // Clear specific date when range is used
                    if (args.NewDate.HasValue)
                    {
                        _VM.SelectedDate = null;
                        if (SpecificDatePicker != null)
                            SpecificDatePicker.Date = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FromDatePicker_DateChanged: {ex.Message}");
            }
        }

        private void ToDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            try
            {
                if (_VM != null)
                {
                    _VM.ToDate = args.NewDate;
                    
                    // Clear specific date when range is used
                    if (args.NewDate.HasValue)
                    {
                        _VM.SelectedDate = null;
                        if (SpecificDatePicker != null)
                            SpecificDatePicker.Date = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ToDatePicker_DateChanged: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            try
            {
                if (sender is ScrollViewer scrollViewer)
                {
                    // Check if user has scrolled to near the bottom (90% of the way)
                    var threshold = scrollViewer.ScrollableHeight * 0.9;
                    
                    if (scrollViewer.VerticalOffset >= threshold && !e.IsIntermediate)
                    {
                        _VM?.OnScrollToBottom();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ScrollViewer_ViewChanged: {ex.Message}");
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject parent)
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is ScrollViewer scrollViewer)
                    {
                        return scrollViewer;
                    }

                    var result = GetScrollViewer(child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetScrollViewer: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get appropriate empty state text based on filter status
        /// </summary>
        public string GetEmptyStateText(bool hasActiveFilters)
        {
            return hasActiveFilters 
                ? "No notifications match your current filters. Try adjusting your search criteria."
                : "Notifications will appear here when they are received";
        }

        #endregion

        #region Obsolete Methods - Kept for Interface Compatibility
        [Obsolete("This method is obsolete. Not supported in notification-only view.")]
        public void UpdateToastView(ViewType viewType)
        {
            // Not applicable for notification-only view
        }

        [Obsolete("This method is obsolete. Not supported in notification-only view.")]
        public void UpdateNotificationsList(ObservableCollection<KToastNotification> currentSystemNotifications)
        {
            // Not applicable for notification-only view
        }

        [Obsolete("This method is obsolete. Use AddNotification instead.")]
        public void AddToastControl(KToastVObj notification)
        {
            _VM?.AddNotification(notification);
        }
        #endregion
    }
}
