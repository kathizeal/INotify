using INotify.Controls;
using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotify.Services;
using INotify.Util;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
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
        private bool _isListeningToRealTimeNotifications = false;
        private readonly NotificationFilterCacheService _cacheService;

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
                
                // Initialize cache service
                _cacheService = NotificationFilterCacheService.Instance;
                
                // Subscribe to visibility changed to manage real-time subscription
                this.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, OnVisibilityChanged);
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

        /// <summary>
        /// Subscribes to real-time notification events
        /// </summary>
        private void SubscribeToRealTimeNotifications()
        {
            if (!_isListeningToRealTimeNotifications)
            {
                NotificationEventInokerUtil.NotificationReceived += OnRealTimeNotificationReceived;
                _isListeningToRealTimeNotifications = true;
                System.Diagnostics.Debug.WriteLine($"NotificationListControl subscribed to real-time notifications for {CurrentTargetType}:{SelectionTargetId}");
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
                System.Diagnostics.Debug.WriteLine($"NotificationListControl unsubscribed from real-time notifications for {CurrentTargetType}:{SelectionTargetId}");
            }
        }

        /// <summary>
        /// Handles real-time notification received events
        /// </summary>
        private void OnRealTimeNotificationReceived(NotificationReceivedEventArgs args)
        {
            try
            {
                if (args?.Notification == null || _viewModel == null)
                    return;

                var notification = args.Notification;
                var packageFamilyName = notification.ToastPackageProfile?.PackageFamilyName;

               
                // Ensure UI updates happen on the UI thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        // Check if this notification is relevant to the current view
                        if (!IsNotificationRelevantToCurrentView(notification))
                            return;

                        HandleRealTimeNotificationOnUIThread(notification);
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
        /// Checks if the received notification is relevant to the current view configuration
        /// </summary>
        private bool IsNotificationRelevantToCurrentView(KToastVObj notification)
        {
            try
            {
                if (string.IsNullOrEmpty(SelectionTargetId) || notification?.ToastPackageProfile == null)
                    return false;

                var packageFamilyName = notification.ToastPackageProfile.PackageFamilyName;

                // Check if the package is relevant based on the current target type and selection
                switch (CurrentTargetType)
                {
                    case SelectionTargetType.Priority:
                        return IsPackageInPriorityCategory(packageFamilyName, SelectionTargetId);

                    case SelectionTargetType.Space:
                        return IsPackageInSpace(packageFamilyName, SelectionTargetId);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking notification relevance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a package belongs to a specific priority category using cached data
        /// </summary>
        private bool IsPackageInPriorityCategory(string packageFamilyName, string priorityLevel)
        {
            try
            {
                return _cacheService?.IsPackageInPriorityCategory(packageFamilyName, priorityLevel) ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking package priority: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a package belongs to a specific space using cached data
        /// </summary>
        private bool IsPackageInSpace(string packageFamilyName, string spaceId)
        {
            try
            {
                return _cacheService?.IsPackageInSpace(packageFamilyName, spaceId) ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking package space membership: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles real-time notification updates on the UI thread
        /// </summary>
        private void HandleRealTimeNotificationOnUIThread(KToastVObj notification)
        {
            try
            {
                // Convert KToastVObj to KToastBObj for consistency with the ViewModel
                var toastBObj = new KToastBObj(notification.NotificationData, notification.ToastPackageProfile);

                if (_viewModel.IsPackageView)
                {
                    HandleRealTimeNotificationForPackageView(toastBObj);
                }
                else
                {
                    HandleRealTimeNotificationForNotificationView(toastBObj);
                }

                // Trigger property change notifications on the ViewModel through reflection or by calling LoadNotifications
                // Since OnPropertyChanged is protected, we need to refresh the counts in another way
                RefreshViewCounts();

                System.Diagnostics.Debug.WriteLine($"Successfully processed real-time notification from {notification.ToastPackageProfile?.AppDisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling real-time notification on UI thread: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the view counts by setting properties that will trigger property change notifications
        /// </summary>
        private void RefreshViewCounts()
        {
            try
            {
                // Force property change notifications by updating a related property
                var currentTargetType = _viewModel.CurrentTargetType;
                _viewModel.CurrentTargetType = currentTargetType; // This will trigger the property setter
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing view counts: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles real-time notifications for notification list view
        /// </summary>
        private void HandleRealTimeNotificationForNotificationView(KToastBObj notification)
        {
            try
            {
                // Check if notification already exists (duplicate prevention)
                var existingNotification = _viewModel.Notifications.FirstOrDefault(n => 
                    n.NotificationData.NotificationId == notification.NotificationData.NotificationId);

                if (existingNotification != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Duplicate notification detected, skipping: {notification.NotificationData.NotificationId}");
                    return;
                }

                // Insert notification in chronological order (most recent first)
                var insertIndex = 0;
                for (int i = 0; i < _viewModel.Notifications.Count; i++)
                {
                    if (_viewModel.Notifications[i].NotificationData.CreatedTime < notification.NotificationData.CreatedTime)
                    {
                        insertIndex = i;
                        break;
                    }
                    insertIndex = i + 1;
                }

                _viewModel.Notifications.Insert(insertIndex, notification);

                System.Diagnostics.Debug.WriteLine($"Added real-time notification to notification view: {notification.NotificationData.NotificationTitle}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling real-time notification for notification view: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles real-time notifications for package view
        /// </summary>
        private void HandleRealTimeNotificationForPackageView(KToastBObj notification)
        {
            try
            {
                var packageFamilyName = notification.ToastPackageProfile?.PackageFamilyName;
                if (string.IsNullOrEmpty(packageFamilyName))
                    return;

                // Find existing package group
                var existingGroup = _viewModel.GroupedPackageNotifications.FirstOrDefault(g => 
                    g.PackageFamilyName == packageFamilyName);

                if (existingGroup != null)
                {
                    // Check if notification already exists in the group (duplicate prevention)
                    var existingNotification = existingGroup.FirstOrDefault(n => 
                        n.NotificationData.NotificationId == notification.NotificationData.NotificationId);

                    if (existingNotification != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Duplicate notification in package group detected, skipping: {notification.NotificationData.NotificationId}");
                        return;
                    }

                    // Add notification to existing group in correct chronological position
                    var insertIndex = 0;
                    for (int i = 0; i < existingGroup.Count; i++)
                    {
                        if (existingGroup[i].NotificationData.CreatedTime < notification.NotificationData.CreatedTime)
                        {
                            insertIndex = i;
                            break;
                        }
                        insertIndex = i + 1;
                    }

                    existingGroup.Insert(insertIndex, notification);
                    existingGroup.NotificationCount = existingGroup.Count;

                    System.Diagnostics.Debug.WriteLine($"Added real-time notification to existing package group {existingGroup.DisplayName}: {notification.NotificationData.NotificationTitle}");
                }
                else
                {
                    // Create new package group
                    var newGroup = new KPackageNotificationGroup(
                        notification.ToastPackageProfile.AppDisplayName,
                        packageFamilyName,
                        notification.ToastPackageProfile.LogoFilePath);

                    newGroup.Add(notification);
                    _viewModel.GroupedPackageNotifications.Add(newGroup);

                    System.Diagnostics.Debug.WriteLine($"Created new package group for real-time notification: {newGroup.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling real-time notification for package view: {ex.Message}");
            }
        }

        #region Helper Functions for XAML Binding

        /// <summary>
        /// Determines if the Add Apps button should be visible based on SelectionTargetId
        /// </summary>
        public Visibility IsAddAppButtonVisible(string selectionTargetId)
        {
            return string.IsNullOrEmpty(selectionTargetId) ? Visibility.Collapsed : Visibility.Visible;
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

        #endregion

        #region Event Handlers

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

        private async void ClearPackageNotifications_Click(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            
            if (sender is Button button && button.Tag is KPackageNotificationGroup group)
            {
                try
                {
                    // Get counts for the confirmation dialog
                    int dbCount = group.NotificationCount;
                    int windowsCount = 0;
                    
                    try
                    {
                        // Try to get Windows notification count
                        var windowsService = WindowsNotificationManagerService.Instance;
                        windowsCount = await windowsService.GetNotificationCountForPackageAsync(group.PackageFamilyName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"?? Could not get Windows notification count: {ex.Message}");
                    }

                    // Build detailed confirmation message
                    var contentText = $"This will clear all notifications for '{group.DisplayName}':\n\n";
                    contentText += $"• {dbCount} notification(s) from INotify history\n";
                    contentText += $"• {windowsCount} notification(s) from Windows Action Center\n\n";
                    contentText += "This action cannot be undone. Do you wish to continue?";

                    // Show enhanced confirmation dialog
                    var dialog = new ContentDialog()
                    {
                        Title = "Clear All Notifications",
                        Content = contentText,
                        PrimaryButtonText = "Yes, Clear All",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Secondary,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // User confirmed - proceed with clearing
                        _viewModel?.ClearPackageNotifications(group);
                        
                        System.Diagnostics.Debug.WriteLine($"User confirmed clearing {dbCount} DB + {windowsCount} Windows notifications for: {group.DisplayName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"User canceled clearing notifications for package: {group.DisplayName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing clear notifications dialog: {ex.Message}");
                }
            }
        }

        private void NavigationPackagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnsureViewModelInitialized();
            
            if (sender is ListView listView && listView.SelectedItem is KPackageNotificationGroup selectedGroup)
            {
                try
                {
                    // Call ViewModel navigation method
                    _viewModel?.NavigateToPackage(selectedGroup.PackageFamilyName);
                    
                    // Scroll to the selected package group header in the ListView
                    ScrollToPackageGroup(selectedGroup);
                    
                    // Close the flyout by finding the button
                    var goToButton = this.FindName("GoToButton") as Button;
                    if (goToButton?.Flyout is Flyout flyout)
                    {
                        flyout.Hide();
                    }
                    
                    // Clear selection to allow reselection of the same item
                    listView.SelectedItem = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error navigating to package: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles adding apps button click
        /// </summary>
        private void AddAppsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set up header text based on target type and id
                if (AppSelectionControl != null)
                {
                    var headerText = CurrentTargetType switch
                    {
                        SelectionTargetType.Priority => $"Add Apps to {SelectionTargetId} Priority",
                        SelectionTargetType.Space => $"Add Apps to {GetSpaceDisplayName(SelectionTargetId)}",
                        _ => "Add Apps"
                    };
                    
                    AppSelectionControl.HeaderTextValue = headerText;
                }

                System.Diagnostics.Debug.WriteLine($"Opened Add Apps flyout for {CurrentTargetType}:{SelectionTargetId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddAppsButton_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles when apps are selected in the flyout
        /// </summary>
        private void AppSelectionControl_AppsSelected(object sender, AppSelectionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Apps selected for {e.TargetType}:{e.CurrentTargetId}, Count: {e.SelectedApps?.Count() ?? 0}");

                // Close the flyout
                AddAppsFlyout?.Hide();

                // Clear selections for next use
                AppSelectionControl?.ClearSelections();

                // Refresh the current view to show updated data
                _viewModel?.RefreshView();

                System.Diagnostics.Debug.WriteLine($"Successfully processed app selection for {e.TargetType}:{e.CurrentTargetId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AppSelectionControl_AppsSelected: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles when the app selection flyout is cancelled
        /// </summary>
        private void AppSelectionControl_Cancelled(object sender, EventArgs e)
        {
            try
            {
                // Close the flyout
                AddAppsFlyout?.Hide();

                // Clear selections
                AppSelectionControl?.ClearSelections();

                System.Diagnostics.Debug.WriteLine("App selection cancelled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AppSelectionControl_Cancelled: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles removing an app from its category via the package group header context menu
        /// </summary>
        private async void RemoveAppFromCategory_Click(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is KPackageNotificationGroup group && _viewModel != null)
            {
                try
                {
                    // Build confirmation message based on current target type
                    var categoryName = CurrentTargetType == SelectionTargetType.Priority 
                        ? $"{SelectionTargetId} Priority" 
                        : GetSpaceDisplayName(SelectionTargetId);

                    var contentText = $"Remove '{group.DisplayName}' from {categoryName}?\n\n";
                    contentText += "This will remove the app from this category but won't delete any notifications.";

                    // Show confirmation dialog
                    var dialog = new ContentDialog()
                    {
                        Title = "Remove from Category",
                        Content = contentText,
                        PrimaryButtonText = "Remove",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Secondary,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // User confirmed - proceed with removal
                        _viewModel.RemoveAppFromCategory(group.PackageFamilyName, group.DisplayName);
                        
                        System.Diagnostics.Debug.WriteLine($"User confirmed removing {group.DisplayName} from {categoryName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"User canceled removing {group.DisplayName} from {categoryName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error removing app from category: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles removing an app from its category via the notification item context menu
        /// </summary>
        private async void RemoveAppFromCategoryNotification_Click(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is KToastBObj notification && _viewModel != null)
            {
                try
                {
                    var packageFamilyName = notification.ToastPackageProfile?.PackageFamilyName;
                    var appDisplayName = notification.ToastPackageProfile?.AppDisplayName;

                    if (string.IsNullOrEmpty(packageFamilyName) || string.IsNullOrEmpty(appDisplayName))
                    {
                        System.Diagnostics.Debug.WriteLine("Cannot remove app: missing package or app information");
                        return;
                    }

                    // Build confirmation message based on current target type
                    var categoryName = CurrentTargetType == SelectionTargetType.Priority 
                        ? $"{SelectionTargetId} Priority" 
                        : GetSpaceDisplayName(SelectionTargetId);

                    var contentText = $"Remove '{appDisplayName}' from {categoryName}?\n\n";
                    contentText += "This will remove the app from this category but won't delete any notifications.";

                    // Show confirmation dialog
                    var dialog = new ContentDialog()
                    {
                        Title = "Remove from Category",
                        Content = contentText,
                        PrimaryButtonText = "Remove",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Secondary,
                        XamlRoot = this.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // User confirmed - proceed with removal
                        _viewModel.RemoveAppFromCategory(packageFamilyName, appDisplayName);
                        
                        System.Diagnostics.Debug.WriteLine($"User confirmed removing {appDisplayName} from {categoryName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"User canceled removing {appDisplayName} from {categoryName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error removing app from category via notification: {ex.Message}");
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets display name for space ID
        /// </summary>
        private string GetSpaceDisplayName(string spaceId) => spaceId switch
        {
            "Space1" => "Space 1",
            "Space2" => "Space 2", 
            "Space3" => "Space 3",
            _ => spaceId
        };

        /// <summary>
        /// Scrolls the grouped packages ListView to the specified package group
        /// </summary>
        private void ScrollToPackageGroup(KPackageNotificationGroup targetGroup)
        {
            try
            {
                if (GroupedPackagesListView == null || targetGroup == null)
                    return;

                // Find the group in the ListView and scroll to it
                var groupIndex = _viewModel.GroupedPackageNotifications.IndexOf(targetGroup);
                if (groupIndex >= 0)
                {
                    // Scroll to the group header
                    GroupedPackagesListView.ScrollIntoView(targetGroup, ScrollIntoViewAlignment.Leading);
                    
                    System.Diagnostics.Debug.WriteLine($"Scrolled to package group: {targetGroup.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scrolling to package group: {ex.Message}");
            }
        }

        #endregion

        #region Lifecycle Events

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureViewModelInitialized();
            UpdateViewModel();
            
            // Subscribe to real-time notifications when the control is loaded and visible
            SubscribeToRealTimeNotifications();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Unsubscribe from real-time notifications when the control is unloaded
                UnsubscribeFromRealTimeNotifications();
                
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing ViewModel: {ex.Message}");
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

        #endregion
    }
}
