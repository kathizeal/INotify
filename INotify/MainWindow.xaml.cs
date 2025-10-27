using AppList; // For DndService and InstalledAppsService
using INotify.KToastDI;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using SampleNotify; // For StandaloneNotificationPositioner
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using static SampleNotify.StandaloneNotificationPositioner;

namespace INotify
{
    public sealed partial class MainWindow : Window
    {
        private DndService _dndService;
        private StandaloneNotificationPositioner _notificationPositioner;
        private NotificationPosition _currentNotificationPosition = NotificationPosition.TopLeft;
        private readonly SemaphoreSlim _fileAccessSemaphore = new SemaphoreSlim(1, 1);
        // Application ViewModel for UI state management
        private KToastListVMBase _VM;

        public MainWindow()
        {
            _VM = KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();
            this.InitializeComponent();
            InitializeServices();

            // Subscribe to the Closed event for cleanup
            this.Closed += MainWindow_Closed;

            // Set up keyboard shortcuts after InitializeComponent
            SetupKeyboardShortcuts();

            // Initialize UI - use a delay to ensure everything is ready
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // Short delay to ensure UI is ready
                DispatcherQueue.TryEnqueue(() =>
                {
                    InitializeUI();
                });
            });
        }

        /// <summary>
        /// Sets up keyboard shortcuts for the application
        /// </summary>
        private void SetupKeyboardShortcuts()
        {
            try
            {
                // Set up keyboard handling on the main content - we'll handle this in the XAML with PreviewKeyDown
                this.Activated += MainWindow_Activated;
                Debug.WriteLine("Keyboard shortcuts ready: Ctrl+H or Escape to hide to tray");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up keyboard shortcuts: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles window activation to ensure focus for keyboard shortcuts
        /// </summary>
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            try
            {
                // Ensure the window content can receive keyboard input
                if (this.Content is FrameworkElement content)
                {
                    content.Focus(FocusState.Programmatic);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling window activation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts - this method will be called from XAML PreviewKeyDown
        /// </summary>
        public void HandleKeyboardShortcut(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                // Check for Ctrl+H
                if (isCtrlPressed && e.Key == VirtualKey.H)
                {
                    HideToTray();
                    e.Handled = true;
                    return;
                }

                // Check for Escape key
                if (e.Key == VirtualKey.Escape)
                {
                    HideToTray();
                    e.Handled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling keyboard shortcut: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the window to system tray
        /// </summary>
        private void HideToTray()
        {
            try
            {
                if (Application.Current is App app)
                {
                    app.ToggleWindowVisibility();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error hiding to tray: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes all required services
        /// </summary>
        private void InitializeServices()
        {
            InitializeDndService();
            InitializeNotificationPositioner();
        }

        /// <summary>
        /// Initializes the Do Not Disturb service
        /// </summary>
        private void InitializeDndService()
        {
            try
            {
                _dndService = new DndService();

                // Update DND status in UI
                UpdateDndStatusUI();

                Debug.WriteLine($"DND Service initialized. Current state: {_dndService.GetCurrentState()}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize DND Service: {ex.Message}");
                ShowStatusMessage("DND Service initialization failed", false);
            }
        }

        /// <summary>
        /// Initializes the notification positioning service
        /// </summary>
        private void InitializeNotificationPositioner()
        {
            try
            {
                _notificationPositioner = new StandaloneNotificationPositioner();
                _notificationPositioner.Position = _currentNotificationPosition;
                _notificationPositioner.StartPositioning();
                Debug.WriteLine("Notification Positioner initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize Notification Positioner: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes the UI components and loads data
        /// </summary>
        private void InitializeUI()
        {
            try
            {
                Debug.WriteLine("InitializeUI: Starting UI initialization");

                // Set default view to Welcome
                ShowWelcomeView();

                // Load all data in background
                LoadAllApplicationsData();

                Debug.WriteLine("InitializeUI: UI initialization completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeUI: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        #region UI Mode Management

        /// <summary>
        /// Switches to Normal UI mode
        /// </summary>
        private void SwitchToNormalMode_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage("Switched to Normal Mode", true);
        }

        /// <summary>
        /// Switches to Mini Widget mode
        /// </summary>
        private void SwitchToMiniWidget_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage("Switched to Mini Widget Mode", true);
        }

        /// <summary>
        /// Hides the window to system tray
        /// </summary>
        private void HideToTray_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        #endregion

        #region Navigation Handling

        /// <summary>
        /// Handles NavigationView selection changes
        /// </summary>
        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                if (args.SelectedItem is NavigationViewItem selectedItem)
                {
                    string tag = selectedItem.Tag?.ToString() ?? "";
                    HandleNavigation(tag);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in navigation selection: {ex.Message}");
                ShowStatusMessage($"Navigation error: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles navigation based on the selected tag
        /// </summary>
        private void HandleNavigation(string tag)
        {
            // Hide all views first
            HideAllViews();

            // Update content header based on selection
            switch (tag)
            {
                case "Priority":
                    ShowPriorityBoardView();
                    break;

                case "Priority_High":
                    ShowDetailListView("High Priority", "High", SelectionTargetType.Priority);
                    break;

                case "Priority_Medium":
                    ShowDetailListView("Medium Priority", "Medium", SelectionTargetType.Priority);
                    break;

                case "Priority_Low":
                    ShowDetailListView("Low Priority", "Low", SelectionTargetType.Priority);
                    break;

                case "Space":
                    ShowSpaceBoardView();
                    break;

                case "Space_1":
                    ShowDetailListView("Space 1", "Space1", SelectionTargetType.Space);
                    break;

                case "Space_2":
                    ShowDetailListView("Space 2", "Space2", SelectionTargetType.Space);
                    break;

                case "Space_3":
                    ShowDetailListView("Space 3", "Space3", SelectionTargetType.Space);
                    break;

                case "AllApps":
                    ShowAllAppsView();
                    break;

                case "AllNotifications":
                    ShowAllNotificationsView();
                    break;

                case "DND":
                    ShowDndControlView();
                    break;

                case "Status":
                    ShowStatusView();
                    break;

                case "Feedback":
                    ShowFeedbackView();
                    break;

                default:
                    ShowWelcomeView();
                    break;
            }

            Debug.WriteLine($"Navigation: {tag}");
        }

        /// <summary>
        /// Hides all content views
        /// </summary>
        private void HideAllViews()
        {
            if (PriorityBoardView != null) PriorityBoardView.Visibility = Visibility.Collapsed;
            if (SpaceBoardView != null) SpaceBoardView.Visibility = Visibility.Collapsed;
            if (DetailListView != null) DetailListView.Visibility = Visibility.Collapsed;
            if (AllAppsView != null) AllAppsView.Visibility = Visibility.Collapsed;
            if (AllNotificationsView != null) AllNotificationsView.Visibility = Visibility.Collapsed;
            if (DndControlView != null) DndControlView.Visibility = Visibility.Collapsed;
            if (StatusView != null) StatusView.Visibility = Visibility.Collapsed;
            if (FeedbackView != null) FeedbackView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Shows the Priority board view with three columns
        /// </summary>
        private void ShowPriorityBoardView()
        {
            if (ContentTitle != null) ContentTitle.Text = "Priority Management";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Manage notification priorities across High, Medium, and Low categories";

            if (PriorityBoardView != null)
            {
                PriorityBoardView.Visibility = Visibility.Visible;

            }
        }

        /// <summary>
        /// Shows the Space board view with three columns
        /// </summary>
        private void ShowSpaceBoardView()
        {
            if (ContentTitle != null) ContentTitle.Text = "Space Management";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Organize your notifications into custom spaces";

            if (SpaceBoardView != null)
            {
                SpaceBoardView.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows a detail list view for specific priority or space
        /// </summary>
        private void ShowDetailListView(string title, string category, SelectionTargetType selectionTargetType)
        {
            if (ContentTitle != null) ContentTitle.Text = title;
            if (ContentSubtitle != null) ContentSubtitle.Text = $"Notifications and apps in {title}";

            if (DetailListView != null)
            {
                DetailListView.Visibility = Visibility.Visible;
                DetailListViewContent.CurrentTargetType = selectionTargetType;
                DetailListViewContent.SelectionTargetId = category;
                DetailListViewContent.UpdateViewModel();
            }
        }

        /// <summary>
        /// Shows all apps view
        /// </summary>
        private void ShowAllAppsView()
        {
            if (ContentTitle != null) ContentTitle.Text = "All Applications";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Browse and manage all installed applications";

            if (AllAppsView != null)
            {
                AllAppsView.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows all notifications view
        /// </summary>
        private void ShowAllNotificationsView()
        {
            if (ContentTitle != null) ContentTitle.Text = "All Notifications";
            if (ContentSubtitle != null) ContentSubtitle.Text = "View and manage all system notifications";

            if (AllNotificationsView != null)
            {
                AllNotificationsView.Visibility = Visibility.Visible;
                // KToastListViewControl is already embedded, no need to load data
            }
        }

        /// <summary>
        /// Shows DND control view
        /// </summary>
        private void ShowDndControlView()
        {
            if (ContentTitle != null) ContentTitle.Text = "Do Not Disturb";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Manage focus assist and notification settings";

            if (DndControlView != null)
            {
                DndControlView.Visibility = Visibility.Visible;
                UpdateDndStatusUI();
            }
        }

        /// <summary>
        /// Shows status view
        /// </summary>
        private void ShowStatusView()
        {
            if (ContentTitle != null) ContentTitle.Text = "System Status";
            if (ContentSubtitle != null) ContentSubtitle.Text = "View system information and statistics";

            if (StatusView != null)
            {
                StatusView.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows feedback view
        /// </summary>
        private void ShowFeedbackView()
        {
            if (ContentTitle != null) ContentTitle.Text = "Submit Feedback";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Help us improve INotify by sharing your feedback";

            if (FeedbackView != null)
            {
                FeedbackView.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows welcome view (default)
        /// </summary>
        private void ShowWelcomeView()
        {
            if (ContentTitle != null) ContentTitle.Text = "Welcome to INotify";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Select a category from the menu to get started. Press Ctrl+H or Escape to minimize to tray.";

            // All views are already hidden, so we just show the welcome message
        }

        #endregion

        #region Data Loading Methods

        private async Task LoadAllApplicationsData()
        {
            try
            {
                // Data loading is now handled by the background service
                Debug.WriteLine("Application data loading delegated to background service");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading all applications data: {ex.Message}");
                ShowStatusMessage($"Error loading applications: {ex.Message}", false);
            }
        }

        private async Task LoadAllAppsData()
        {
            try
            {
                // This method is now handled by the background service
                Debug.WriteLine("App data loading delegated to background service");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading all apps data: {ex.Message}");
            }
        }

        #endregion

        #region DND Management

        /// <summary>
        /// Updates the DND status display in the UI
        /// </summary>
        private void UpdateDndStatusUI()
        {
            if (_dndService == null) return;

            try
            {
                string status = _dndService.GetStateDescription();
                if (DndStatusText != null)
                {
                    DndStatusText.Text = status;
                }
                else
                {
                    Debug.WriteLine("Warning: DndStatusText is null in UpdateDndStatusUI");
                }

                bool isDndEnabled = _dndService.IsDndEnabled();
                if (ToggleDndButton != null)
                {
                    ToggleDndButton.Content = isDndEnabled ? "Disable DND" : "Enable DND";
                }
                else
                {
                    Debug.WriteLine("Warning: ToggleDndButton is null in UpdateDndStatusUI");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating DND status UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggles Do Not Disturb mode
        /// </summary>
        private void ToggleDnd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsDoNotDisturbEnabled())
                {
                    DisableDoNotDisturb();
                }
                else
                {
                    EnableDoNotDisturb();
                }
                UpdateDndStatusUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling DND: {ex.Message}");
                ShowStatusMessage($"Error toggling Do Not Disturb: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Sets DND to Alarms Only mode
        /// </summary>
        private void SetAlarmsOnly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetAlarmsOnlyMode();
                UpdateDndStatusUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting Alarms Only: {ex.Message}");
                ShowStatusMessage($"Error setting Alarms Only: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Shows detailed DND status
        /// </summary>
        private void ShowDndStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string status = GetDoNotDisturbStatus();
                ShowStatusMessage(status, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting DND status: {ex.Message}");
                ShowStatusMessage($"Error getting DND status: {ex.Message}", false);
            }
        }

        public bool EnableDoNotDisturb()
        {
            try
            {
                if (_dndService == null) return false;

                bool success = _dndService.EnableDnd();

                if (success)
                {
                    Debug.WriteLine("Do Not Disturb mode enabled successfully");
                    ShowStatusMessage("Do Not Disturb enabled", true);
                }
                else
                {
                    Debug.WriteLine("Failed to enable Do Not Disturb mode");
                    ShowStatusMessage("Failed to enable Do Not Disturb", false);
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enabling Do Not Disturb: {ex.Message}");
                ShowStatusMessage($"Error: {ex.Message}", false);
                return false;
            }
        }

        public bool DisableDoNotDisturb()
        {
            try
            {
                if (_dndService == null) return false;

                bool success = _dndService.DisableDnd();

                if (success)
                {
                    Debug.WriteLine("Do Not Disturb mode disabled successfully");
                    ShowStatusMessage("Do Not Disturb disabled", true);
                }
                else
                {
                    Debug.WriteLine("Failed to disable Do Not Disturb mode");
                    ShowStatusMessage("Failed to disable Do Not Disturb", false);
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disabling Do Not Disturb: {ex.Message}");
                ShowStatusMessage($"Error: {ex.Message}", false);
                return false;
            }
        }

        public bool SetAlarmsOnlyMode()
        {
            try
            {
                if (_dndService == null) return false;

                bool success = _dndService.SetAlarmsOnly();

                if (success)
                {
                    Debug.WriteLine("Do Not Disturb set to Alarms Only mode");
                    ShowStatusMessage("Do Not Disturb set to Alarms Only", true);
                }
                else
                {
                    Debug.WriteLine("Failed to set Alarms Only mode");
                    ShowStatusMessage("Failed to set Alarms Only mode", false);
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting Alarms Only mode: {ex.Message}");
                ShowStatusMessage($"Error: {ex.Message}", false);
                return false;
            }
        }

        public string GetDoNotDisturbStatus()
        {
            try
            {
                if (_dndService == null) return "DND Service unavailable";

                return _dndService.GetStateDescription();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting DND status: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public bool IsDoNotDisturbEnabled()
        {
            try
            {
                if (_dndService == null) return false;

                return _dndService.IsDndEnabled();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking DND status: {ex.Message}");
                return false;
            }
        }

        public string GetDndDiagnosticInfo()
        {
            try
            {
                if (_dndService == null) return "DND Service is not available";

                return _dndService.GetDiagnosticInfo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting DND diagnostic info: {ex.Message}");
                return $"Error getting diagnostic info: {ex.Message}";
            }
        }

        #endregion

        #region Settings and Diagnostics

        /// <summary>
        /// Opens settings panel with comprehensive status information
        /// </summary>
        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var settingsPanel = new SettingsStatusPanel(_dndService, _notificationPositioner);
                //settingsPanel.XamlRoot = this.Content.XamlRoot;
                //await settingsPanel.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening settings: {ex.Message}");
                ShowStatusMessage($"Error opening settings: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Shows diagnostic information
        /// </summary>
        private async void Diagnostics_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    var settingsPanel = new SettingsStatusPanel(_dndService, _notificationPositioner);
            //    settingsPanel.XamlRoot = this.Content.XamlRoot;
            //    await settingsPanel.ShowAsync();

            //    string diagnostics = GetDndDiagnosticInfo();
            //    Debug.WriteLine($"Diagnostics:\n{diagnostics}");
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine($"Error getting diagnostics: {ex.Message}");
            //    ShowStatusMessage($"Error getting diagnostics: {ex.Message}", false);
            //}
        }

        #endregion

        #region Status Messages

        /// <summary>
        /// Shows a status message to the user
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="isSuccess">Whether this is a success message</param>
        private void ShowStatusMessage(string message, bool isSuccess)
        {
            try
            {
                // For now, just log to debug
                Debug.WriteLine($"Status: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing status message: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Clean up resources when window is closed
        /// </summary>
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                // Clean up local services (background service continues running)
                _dndService?.Dispose();
                _notificationPositioner = null;

                Debug.WriteLine("MainWindow resources cleaned up successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing services: {ex.Message}");
            }
        }

        /// <summary>
        /// Brings this window to foreground and ensures it's visible
        /// </summary>
        public void BringToForeground()
        {
            try
            {
                this.Activate();
                Debug.WriteLine("MainWindow brought to foreground");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error bringing MainWindow to foreground: {ex.Message}");
            }
        }

        private void ContentSubtitle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AppNotification notification = new AppNotificationBuilder()
.AddText("Welcome to WinUI 3 Gallery")
.AddText("Explore interactive samples and discover the power of modern Windows UI.")
.BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
