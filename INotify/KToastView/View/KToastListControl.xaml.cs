using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotify.KToastViewModel.ViewModelContract;
using INotify.Services;
using INotify.Util;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify.KToastView.View
{
    public sealed partial class KToastListControl : UserControl, IKToastListView
    {
        private KToastListVMBase _VM;
        private bool _isListeningToRealTimeNotifications = false;

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
            
            // Subscribe to visibility changed to manage real-time subscription
            this.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, OnVisibilityChanged);
        }

        public Visibility EmptyState(int count)
        {
            return count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public Visibility FilterPanelVisibility(bool visible)
        {
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Subscribes to real-time notification events for the "All Notifications" view
        /// </summary>
        private void SubscribeToRealTimeNotifications()
        {
            if (!_isListeningToRealTimeNotifications)
            {
                NotificationEventInokerUtil.NotificationReceived += OnRealTimeNotificationReceived;
                _isListeningToRealTimeNotifications = true;
                System.Diagnostics.Debug.WriteLine("KToastListControl subscribed to real-time notifications for All Notifications view");
            }
        }

        /// <summary>
        /// Unsubscribes from real-time notification events
        /// </summary>
        private void UnsubscribeFromRealTimeNotifications()
        {
            if (_isListeningToRealTimeNotifications)
            {
                NotificationEventInokerUtil.NotificationReceived -= OnRealTimeNotificationReceived;
                _isListeningToRealTimeNotifications = false;
                System.Diagnostics.Debug.WriteLine("KToastListControl unsubscribed from real-time notifications");
            }
        }

        /// <summary>
        /// Handles real-time notification received events with filter consideration
        /// </summary>
        private void OnRealTimeNotificationReceived(NotificationReceivedEventArgs args)
        {
            try
            {
                if (args?.Notification == null || _VM == null)
                    return;

                var notification = args.Notification;

                // Ensure UI updates happen on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        // Check if this notification passes the current filters
                        if (ShouldNotificationBeDisplayed(notification))
                        {
                            HandleRealTimeNotificationOnUIThread(notification);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Real-time notification filtered out: {notification.NotificationData?.NotificationTitle}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error handling real-time notification on UI thread: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnRealTimeNotificationReceived: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if a notification should be displayed based on current filter settings
        /// </summary>
        private bool ShouldNotificationBeDisplayed(KToastVObj notification)
        {
            try
            {
                if (notification?.NotificationData == null || notification.ToastPackageProfile == null)
                    return false;

                // Check search keyword filter
                if (!string.IsNullOrEmpty(_VM.SearchKeyword))
                {
                    var searchTerm = _VM.SearchKeyword.ToLowerInvariant();
                    var title = notification.NotificationData.NotificationTitle?.ToLowerInvariant() ?? "";
                    var message = notification.NotificationData.NotificationMessage?.ToLowerInvariant() ?? "";
                    var appName = notification.ToastPackageProfile.AppDisplayName?.ToLowerInvariant() ?? "";

                    if (!title.Contains(searchTerm) && !message.Contains(searchTerm) && !appName.Contains(searchTerm))
                    {
                        return false;
                    }
                }

                // Check app filter
                if (!string.IsNullOrEmpty(_VM.SelectedAppFilter) && _VM.SelectedAppFilter != "")
                {
                    if (notification.ToastPackageProfile.PackageFamilyName != _VM.SelectedAppFilter)
                    {
                        return false;
                    }
                }

                // Check date filters
                var notificationDate = notification.NotificationData.CreatedTime;

                // Specific date filter
                if (_VM.SelectedDate.HasValue)
                {
                    var selectedDate = _VM.SelectedDate.Value.Date;
                    if (notificationDate.Date != selectedDate)
                    {
                        return false;
                    }
                }
                // Date range filter
                else if (_VM.FromDate.HasValue || _VM.ToDate.HasValue)
                {
                    if (_VM.FromDate.HasValue && notificationDate.Date < _VM.FromDate.Value.Date)
                    {
                        return false;
                    }

                    if (_VM.ToDate.HasValue && notificationDate.Date > _VM.ToDate.Value.Date)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking notification filter criteria: {ex.Message}");
                // On error, allow the notification to be displayed
                return true;
            }
        }

        /// <summary>
        /// Handles real-time notification updates on the UI thread
        /// </summary>
        private void HandleRealTimeNotificationOnUIThread(KToastVObj notification)
        {
            try
            {
                // Check if notification already exists (duplicate prevention)
                var existingNotification = _VM.KToastNotifications.FirstOrDefault(n => 
                    n.NotificationData.NotificationId == notification.NotificationData.NotificationId);

                if (existingNotification != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Duplicate notification detected in KToastListControl, skipping: {notification.NotificationData.NotificationId}");
                    return;
                }

                // Insert notification in chronological order (most recent first)
                var insertIndex = 0;
                for (int i = 0; i < _VM.KToastNotifications.Count; i++)
                {
                    if (_VM.KToastNotifications[i].NotificationData.CreatedTime < notification.NotificationData.CreatedTime)
                    {
                        insertIndex = i;
                        break;
                    }
                    insertIndex = i + 1;
                }

                _VM.KToastNotifications.Insert(insertIndex, notification);

                // Note: TotalCountText will be updated automatically through property binding
                // since it's computed from TotalCount which reflects the collection count

                System.Diagnostics.Debug.WriteLine($"Added real-time notification to KToastListControl: {notification.NotificationData.NotificationTitle}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling real-time notification on UI thread: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the control's visibility changes to manage real-time subscription
        /// </summary>
        private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            try
            {
                if (this.Visibility == Visibility.Visible)
                {
                    SubscribeToRealTimeNotifications();
                }
                else
                {
                    UnsubscribeFromRealTimeNotifications();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnVisibilityChanged: {ex.Message}");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load initial notifications when control is loaded
                _VM.LoadInitialNotifications();

                // Subscribe to real-time notifications when the control is loaded and visible
                SubscribeToRealTimeNotifications();

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
                // Unsubscribe from real-time notifications when the control is unloaded
                UnsubscribeFromRealTimeNotifications();

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
                
                // After applying filters, we need to re-evaluate existing real-time notifications
                // This ensures that notifications added in real-time are also subject to the new filters
                FilterExistingNotifications();
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

        /// <summary>
        /// Filters existing notifications when filter criteria change
        /// This ensures real-time notifications are also subject to current filters
        /// </summary>
        private void FilterExistingNotifications()
        {
            try
            {
                // Create a list of notifications that should be removed based on current filters
                var notificationsToRemove = _VM.KToastNotifications
                    .Where(notification => !ShouldNotificationBeDisplayed(notification))
                    .ToList();

                // Remove notifications that no longer match the filters
                foreach (var notification in notificationsToRemove)
                {
                    _VM.KToastNotifications.Remove(notification);
                }

                // Note: TotalCountText will be updated automatically through property binding
                // since it's computed from TotalCount which reflects the collection count

                System.Diagnostics.Debug.WriteLine($"Filtered out {notificationsToRemove.Count} existing notifications based on new filter criteria");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering existing notifications: {ex.Message}");
            }
        }

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
        #endregion
       
    }
}
