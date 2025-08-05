using AppList; // For DndService and InstalledAppsService
using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SampleNotify; // For StandaloneNotificationPositioner
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using static SampleNotify.StandaloneNotificationPositioner;

namespace INotify
{
    public sealed partial class MainWindow : Window
    {
        UserNotificationListener _listener = default;
        private DndService _dndService;
        private StandaloneNotificationPositioner _notificationPositioner;
        private NotificationPosition _currentNotificationPosition = NotificationPosition.TopLeft;
        private readonly SemaphoreSlim _fileAccessSemaphore = new SemaphoreSlim(1, 1);
        // Application ViewModel for UI state management
      
        public MainWindow()
        {
            this.InitializeComponent();
            InitializeServices();
            CheckFeatureSupport();
            if (_listener != null)
                _listener.NotificationChanged += _listener_NotificationChanged;

            // Subscribe to the Closed event for cleanup
            this.Closed += MainWindow_Closed;

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

                // Initialize the DND service instance for InstalledAppInfo
                //InstalledAppInfo.SetDndService(_dndService);

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
                    ShowDetailListView("High Priority", "High");
                    break;

                case "Priority_Medium":
                    ShowDetailListView("Medium Priority", "Medium");
                    break;

                case "Priority_Low":
                    ShowDetailListView("Low Priority", "Low");
                    break;

                case "Space":
                    ShowSpaceBoardView();
                    break;

                case "Space_1":
                    ShowDetailListView("Space 1", "Space1");
                    break;

                case "Space_2":
                    ShowDetailListView("Space 2", "Space2");
                    break;

                case "Space_3":
                    ShowDetailListView("Space 3", "Space3");
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
        private void ShowDetailListView(string title, string category)
        {
            if (ContentTitle != null) ContentTitle.Text = title;
            if (ContentSubtitle != null) ContentSubtitle.Text = $"Notifications and apps in {title}";
            
            if (DetailListView != null)
            {
                DetailListView.Visibility = Visibility.Visible;
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
        /// Shows welcome view (default)
        /// </summary>
        private void ShowWelcomeView()
        {
            if (ContentTitle != null) ContentTitle.Text = "Welcome to INotify";
            if (ContentSubtitle != null) ContentSubtitle.Text = "Select a category from the menu to get started";
            
            // All views are already hidden, so we just show the welcome message
        }

        #endregion

        #region Data Loading Methods


        private async Task LoadAllApplicationsData()
        {
            try
            {

                //ObservableCollection<InstalledAppInfo>? allApps = await InstalledAppsService.GetAllInstalledAppsAsync();
                //ObservableCollection<InstalledAppInfo>? allApps = await InstalledAppsService.GetAllInstalledAppsAsync();

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
                //if (AllAppsListView == null) return;

                //AllAppsListView.Items.Clear();

                //await LoadAllApplicationsData();

                ////foreach (var app in AllApplicationsInfo)
                ////{
                ////    var item = new TextBlock { Text = $"{app.DisplayName} - {app.Name}" };
                ////    AllAppsListView.Items.Add(item);
                ////}

                //if (AllAppsListView.Items.Count == 0)
                //{
                //    var placeholder = new TextBlock { Text = "No applications found", FontStyle = Windows.UI.Text.FontStyle.Italic };
                //    AllAppsListView.Items.Add(placeholder);
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading all apps data: {ex.Message}");
            }
        }

        #endregion

        #region Service Factory Methods

        /// </summary>

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles adding apps to priority from board view buttons - Now opens flyout with reusable component
        /// </summary>
        private async void AddToPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string priorityLevel)
                {

                    // Get the appropriate app selector control
                    var appSelector = priorityLevel switch
                    {
                        "High" => HighPriorityAppSelector,
                        "Medium" => MediumPriorityAppSelector,
                        "Low" => LowPriorityAppSelector,
                        _ => null
                    };

                    if (appSelector != null)
                    {
                        // Set up event handlers
                     //   appSelector.AppsSelected -= OnPriorityAppsSelected;
                        appSelector.Cancelled -= OnFlyoutCancelled;
                       // appSelector.AppsSelected += OnPriorityAppsSelected;
                        appSelector.Cancelled += OnFlyoutCancelled;

                        // Store the priority level for later use
                        appSelector.Tag = priorityLevel;

                      
                    }

                    Debug.WriteLine($"Opened {priorityLevel} priority flyout");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddToPriority_Click: {ex.Message}");
                ShowStatusMessage($"Error opening priority flyout: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles adding apps to space from board view buttons - Now opens flyout with reusable component
        /// </summary>
        private async void AddToSpace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string spaceId)
                {
                    // Get the appropriate app selector control
                    var appSelector = spaceId switch
                    {
                        "Space1" => Space1AppSelector,
                        "Space2" => Space2AppSelector,
                        "Space3" => Space3AppSelector,
                        _ => null
                    };

                    if (appSelector != null)
                    {
                        // Set up event handlers
                       // appSelector.AppsSelected -= OnSpaceAppsSelected;
                        appSelector.Cancelled -= OnFlyoutCancelled;
                       // appSelector.AppsSelected += OnSpaceAppsSelected;
                        appSelector.Cancelled += OnFlyoutCancelled;

                        // Store the space ID for later use
                        appSelector.Tag = spaceId;

               
                    }

                    Debug.WriteLine($"Opened {spaceId} space flyout");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddToSpace_Click: {ex.Message}");
                ShowStatusMessage($"Error opening space flyout: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles when apps are selected in priority flyouts
        /// </summary>
        private async void OnPriorityAppsSelected(object? sender, AppSelectionEventArgs e)
        {
            try
            {
                if (sender is Controls.AppSelectionFlyoutControl appSelector && 
                    appSelector.Tag is string priorityLevel)
                {
                    //await ProcessSelectedAppsForPriority(priorityLevel, e.SelectedApps);
                    
                    // Close the flyout
                    var flyout = priorityLevel switch
                    {
                        "High" => HighPriorityFlyout,
                        "Medium" => MediumPriorityFlyout,
                        "Low" => LowPriorityFlyout,
                        _ => null
                    };
                    flyout?.Hide();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnPriorityAppsSelected: {ex.Message}");
                ShowStatusMessage($"Error adding apps to priority: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles when apps are selected in space flyouts
        /// </summary>
        private async void OnSpaceAppsSelected(object? sender, AppSelectionEventArgs e)
        {
            try
            {
                if (sender is Controls.AppSelectionFlyoutControl appSelector && 
                    appSelector.Tag is string spaceId)
                {
                    //await ProcessSelectedAppsForSpace(spaceId, e.SelectedApps);
                    
                    // Close the flyout
                    var flyout = spaceId switch
                    {
                        "Space1" => Space1Flyout,
                        "Space2" => Space2Flyout,
                        "Space3" => Space3Flyout,
                        _ => null
                    };
                    flyout?.Hide();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnSpaceAppsSelected: {ex.Message}");
                ShowStatusMessage($"Error adding apps to space: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles flyout cancellation
        /// </summary>
        private void OnFlyoutCancelled(object? sender, EventArgs e)
        {
            if (sender is Controls.AppSelectionFlyoutControl appSelector)
            {
                // Find and close the appropriate flyout
                if (appSelector == HighPriorityAppSelector) HighPriorityFlyout?.Hide();
                else if (appSelector == MediumPriorityAppSelector) MediumPriorityFlyout?.Hide();
                else if (appSelector == LowPriorityAppSelector) LowPriorityFlyout?.Hide();
                else if (appSelector == Space1AppSelector) Space1Flyout?.Hide();
                else if (appSelector == Space2AppSelector) Space2Flyout?.Hide();
                else if (appSelector == Space3AppSelector) Space3Flyout?.Hide();
            }
        }

    
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

        #region Notification Methods

        private void CheckFeatureSupport()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
            {
                GetAccessFromUser();
                FetchToastNotifications();
            }
            else
            {
                // Listener not supported!
                ShowStatusMessage("UserNotificationListener not supported on this system", false);
            }
        }

        public async void GetAccessFromUser()
        {
            _listener = UserNotificationListener.Current;
            UserNotificationListenerAccessStatus accessStatus = await _listener.RequestAccessAsync();
            switch (accessStatus)
            {
                case UserNotificationListenerAccessStatus.Allowed:
                    ShowStatusMessage("Notification access granted", true);
                    break;
                case UserNotificationListenerAccessStatus.Denied:
                    ShowStatusMessage("Notification access denied. Please grant access in Settings.", false);
                    break;
                case UserNotificationListenerAccessStatus.Unspecified:
                    ShowStatusMessage("Notification access status unspecified", false);
                    break;
            }
        }

        private async void FetchToastNotifications()
        {
            var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            foreach (UserNotification notification in notifications)
            {
                CreateKToastModel(notification);
            }
        }

        HashSet<string> appsname = new();
        public async void CreateKToastModel(UserNotification notif)
        {
            try
            {
                string appDisplayName = notif.AppInfo.DisplayInfo.DisplayName;
                string appId = notif.AppInfo.AppUserModelId;
                uint notificationId = notif.Id;
                string packageName = string.IsNullOrWhiteSpace(notif.AppInfo.PackageFamilyName) ?  appId : notif.AppInfo.PackageFamilyName;

                           NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                string iconLocation = string.Empty;
                try
                {
                    // Get the app's logo
                    BitmapImage appLogo = new BitmapImage();
                    RandomAccessStreamReference appLogoStream = notif.AppInfo?.DisplayInfo?.GetLogo(new Windows.Foundation.Size(64, 64));
                    if (appLogoStream != null)
                    {
                        iconLocation = await SaveAppIconToLocalFolder(appLogo, appLogoStream, appDisplayName);
                    }
                }
                catch (COMException exe)
                {
                    Debug.WriteLine($"Error getting app logo: {exe.Message}");
                }


                // Get the toast notification content

                if (toastBinding != null)
                {
                    IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();
                    string titleText = textElements.FirstOrDefault()?.Text ?? "No Title";
                    string bodyText = "\n";
                    foreach (var text in textElements)
                    {
                        bodyText += "\n" + text.Text;
                    }

                    KToastNotification data = new KToastNotification
                    {
                        NotificationTitle = titleText?.Trim(),
                        NotificationMessage = bodyText?.Trim(),
                        NotificationId = notificationId.ToString(),
                        CreatedTime = notif.CreationTime,
                        PackageFamilyName = packageName
                    };

                    KPackageProfile packageProfile = new KPackageProfile()
                    {
                        PackageFamilyName = packageName,
                        LogoFilePath = iconLocation,
                        AppDescription = string.Empty,
                        AppDisplayName = appDisplayName
                    };
                    KToastVObj kToastViewData = new KToastVObj(data, packageProfile);

                    // Add null check before calling AddToastControl
                    if (KToastListViewControl != null)
                    {
                        KToastListViewControl.AddToastControl(kToastViewData);
                    }
                    else
                    {
                        Debug.WriteLine("Warning: KToastListViewControl is null in CreateKToastModel");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating toast model: {ex.Message}");
            }

        }

        private async Task<string> SaveAppIconToLocalFolder(
            BitmapImage appLogo,
            RandomAccessStreamReference inputStream,
            string appName)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string fileName = $"{appName}.png";

            await _fileAccessSemaphore.WaitAsync();
            try
            {
                StorageFile existingFile = await localFolder.TryGetItemAsync(fileName) as StorageFile;
                if (existingFile != null)
                {
                    return existingFile.Path;
                }

                StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                using (IRandomAccessStream input = await inputStream.OpenReadAsync())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(input);

                    var pixelData = await decoder.GetPixelDataAsync();
                    encoder.SetPixelData(
                        decoder.BitmapPixelFormat,
                        decoder.BitmapAlphaMode,
                        decoder.PixelWidth,
                        decoder.PixelHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixelData.DetachPixelData());

                    await encoder.FlushAsync();
                }

                using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await appLogo.SetSourceAsync(readStream);
                }

                return file.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving app icon: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                _fileAccessSemaphore.Release();
            }
        }

        private void _listener_NotificationChanged(UserNotificationListener sender, Windows.UI.Notifications.UserNotificationChangedEventArgs args)
        {
            var notification = sender.GetNotification(args.UserNotificationId);
            DispatcherQueue.TryEnqueue(() =>
            {
                if (notification != null)
                {
                    CreateKToastModel(notification);
                }
            });
        }

        #endregion

        /// <summary>
        /// Clean up resources when window is closed
        /// </summary>
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                _dndService?.Dispose();
                _notificationPositioner = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing services: {ex.Message}");
            }
        }
        #endregion
    }
}
