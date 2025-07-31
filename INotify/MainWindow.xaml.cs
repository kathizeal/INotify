using AppList; // For DndService and InstalledAppsService
using INotify.Dialogs; // For dialog classes
using INotify.KToastView.Model;
using INotify.Services; // For custom services
using INotify.ViewModels; // For new ViewModels
using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Model.Entity;
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

namespace INotify
{
    public sealed partial class MainWindow : Window
    {
        UserNotificationListener _listener = default;
        private DndService _dndService;
        private StandaloneNotificationPositioner _notificationPositioner;
        private readonly SemaphoreSlim _fileAccessSemaphore = new SemaphoreSlim(1, 1);

        // Application ViewModel for UI state management
        public ApplicationViewModel ApplicationVM { get; set; } = new();

        // Priority-based collections implementing IKPriorityPackage
        public ObservableCollection<PriorityPackageViewModel> HighPriorityNotifications { get; set; } = new();
        public ObservableCollection<PriorityPackageViewModel> MediumPriorityNotifications { get; set; } = new();
        public ObservableCollection<PriorityPackageViewModel> LowPriorityNotifications { get; set; } = new();

        // Space-based collections using KSpaceMapper
        public ObservableCollection<SpaceViewModel> Space1Apps { get; set; } = new();
        public ObservableCollection<SpaceViewModel> Space2Apps { get; set; } = new();
        public ObservableCollection<SpaceViewModel> Space3Apps { get; set; } = new();

        // All applications collection
        public ObservableCollection<PriorityPackageViewModel> AllApplications { get; set; } = new();
        public ObservableCollection<InstalledAppInfo> AllApplicationsInfo { get; set; } = new();

        // Legacy collections for backward compatibility
        public ObservableCollection<PriorityAppViewModel> HighPriorityApps { get; set; } = new();
        public ObservableCollection<PriorityAppViewModel> MediumPriorityApps { get; set; } = new();
        public ObservableCollection<PriorityAppViewModel> LowPriorityApps { get; set; } = new();

        // Current view state
        private ViewType _currentViewType = ViewType.Priority;
        private StandaloneNotificationPositioner.NotificationPosition _currentNotificationPosition =
            StandaloneNotificationPositioner.NotificationPosition.TopRight;

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
                InstalledAppInfo.SetDndService(_dndService);

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
                LoadPriorityData();
                LoadSpaceData();
                LoadAllApplicationsData();
                UpdateStatistics();

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
            ApplicationVM.SwitchToNormalMode();
            ShowStatusMessage("Switched to Normal Mode", true);
        }

        /// <summary>
        /// Switches to Mini Widget mode
        /// </summary>
        private void SwitchToMiniWidget_Click(object sender, RoutedEventArgs e)
        {
            ApplicationVM.SwitchToMiniWidget();
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
                LoadPriorityBoardData();
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
                LoadSpaceBoardData();
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
                LoadDetailListData(category);
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
                LoadAllAppsData();
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
                UpdateStatistics();
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

        /// <summary>
        /// Loads priority-based data for tabs using custom priority system
        /// </summary>
        private async void LoadPriorityData()
        {
            try
            {
                // Initialize custom priority service
                var customPriorityService = await GetCustomPriorityServiceAsync();
                if (customPriorityService == null) return;

                // Clear existing collections
                HighPriorityNotifications.Clear();
                MediumPriorityNotifications.Clear();
                LowPriorityNotifications.Clear();

                // Load custom priority apps
                var highApps = await customPriorityService.GetAppsByPriorityAsync(Priority.High);
                var mediumApps = await customPriorityService.GetAppsByPriorityAsync(Priority.Medium);
                var lowApps = await customPriorityService.GetAppsByPriorityAsync(Priority.Low);

                foreach (var app in highApps)
                {
                    HighPriorityNotifications.Add(app);
                }

                foreach (var app in mediumApps)
                {
                    MediumPriorityNotifications.Add(app);
                }

                foreach (var app in lowApps)
                {
                    LowPriorityNotifications.Add(app);
                }

                Debug.WriteLine($"Loaded Custom Priority Data: High={HighPriorityNotifications.Count}, Medium={MediumPriorityNotifications.Count}, Low={LowPriorityNotifications.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading priority data: {ex.Message}");
                ShowStatusMessage($"Error loading priority data: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Loads space-based data from database using custom space service
        /// </summary>
        private async void LoadSpaceData()
        {
            try
            {
                var spaceService = await GetSpaceManagementServiceAsync();
                var customPriorityService = await GetCustomPriorityServiceAsync();

                if (spaceService == null || customPriorityService == null) return;

                // Clear existing collections
                Space1Apps.Clear();
                Space2Apps.Clear();
                Space3Apps.Clear();

                // Initialize default spaces if needed
                await customPriorityService.InitializeDefaultSpacesAsync();

                // Get space statistics
                var stats = await customPriorityService.GetSpaceStatisticsAsync();

                // Create space view models
                var spaces = new[]
                {
                    new { Id = "work", Name = "Work & Productivity", Description = "Work-related applications", Collection = Space1Apps },
                    new { Id = "personal", Name = "Personal", Description = "Personal applications", Collection = Space2Apps },
                    new { Id = "entertainment", Name = "Entertainment", Description = "Games and media apps", Collection = Space3Apps }
                };

                foreach (var space in spaces)
                {
                    var spaceStats = stats.GetValueOrDefault(space.Id, (0, 0));
                    var spaceViewModel = new SpaceViewModel
                    {
                        SpaceId = space.Id,
                        DisplayName = space.Name,
                        Description = space.Description,
                        PackageCount = spaceStats.Item1,
                        NotificationCount = spaceStats.Item2,
                        IsActive = true
                    };

                    space.Collection.Add(spaceViewModel);
                }

                Debug.WriteLine($"Loaded Space Data: Space1={Space1Apps.Count}, Space2={Space2Apps.Count}, Space3={Space3Apps.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading space data: {ex.Message}");
                ShowStatusMessage($"Error loading space data: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Loads all applications data with custom priority status
        /// </summary>
        private async Task LoadAllApplicationsData()
        {
            try
            {
                AllApplicationsInfo.Clear();

                //var customPriorityService = await GetCustomPriorityServiceAsync();
                //if (customPriorityService == null) return;

                ObservableCollection<InstalledAppInfo>? allApps = await InstalledAppsService.GetAllInstalledAppsAsync();

                foreach (var app in allApps) 
                {
                    AllApplicationsInfo.Add(app);
                }

                Debug.WriteLine($"Loaded All Applications Data: {AllApplicationsInfo.Count} apps");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading all applications data: {ex.Message}");
                ShowStatusMessage($"Error loading applications: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Loads data for the priority board view
        /// </summary>
        private void LoadPriorityBoardData()
        {
            try
            {
                // Clear existing data
                if (HighPriorityListView != null) HighPriorityListView.Items.Clear();
                if (MediumPriorityListView != null) MediumPriorityListView.Items.Clear();
                if (LowPriorityListView != null) LowPriorityListView.Items.Clear();

                // For now, just show placeholder text if no data
                if (HighPriorityNotifications.Count == 0)
                {
                    if (HighPriorityListView != null)
                    {
                        var placeholder = new TextBlock { Text = "No high priority apps", FontStyle = Windows.UI.Text.FontStyle.Italic };
                        HighPriorityListView.Items.Add(placeholder);
                    }
                }
                else
                {
                    foreach (var app in HighPriorityNotifications)
                    {
                        if (HighPriorityListView != null)
                        {
                            var item = new TextBlock { Text = app.DisplayName };
                            HighPriorityListView.Items.Add(item);
                        }
                    }
                }

                // Similar for Medium and Low priority
                if (MediumPriorityNotifications.Count == 0)
                {
                    if (MediumPriorityListView != null)
                    {
                        var placeholder = new TextBlock { Text = "No medium priority apps", FontStyle = Windows.UI.Text.FontStyle.Italic };
                        MediumPriorityListView.Items.Add(placeholder);
                    }
                }
                else
                {
                    foreach (var app in MediumPriorityNotifications)
                    {
                        if (MediumPriorityListView != null)
                        {
                            var item = new TextBlock { Text = app.DisplayName };
                            MediumPriorityListView.Items.Add(item);
                        }
                    }
                }

                if (LowPriorityNotifications.Count == 0)
                {
                    if (LowPriorityListView != null)
                    {
                        var placeholder = new TextBlock { Text = "No low priority apps", FontStyle = Windows.UI.Text.FontStyle.Italic };
                        LowPriorityListView.Items.Add(placeholder);
                    }
                }
                else
                {
                    foreach (var app in LowPriorityNotifications)
                    {
                        if (LowPriorityListView != null)
                        {
                            var item = new TextBlock { Text = app.DisplayName };
                            LowPriorityListView.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading priority board data: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data for the space board view
        /// </summary>
        private void LoadSpaceBoardData()
        {
            try
            {
                // Clear existing data
                if (Space1ListView != null) Space1ListView.Items.Clear();
                if (Space2ListView != null) Space2ListView.Items.Clear();
                if (Space3ListView != null) Space3ListView.Items.Clear();

                // For now, just show placeholder text if no data
                if (Space1Apps.Count == 0)
                {
                    if (Space1ListView != null)
                    {
                        var placeholder = new TextBlock { Text = "No apps in Space 1", FontStyle = Windows.UI.Text.FontStyle.Italic };
                        Space1ListView.Items.Add(placeholder);
                    }
                }
                else
                {
                    foreach (var space in Space1Apps)
                    {
                        if (Space1ListView != null)
                        {
                            var item = new TextBlock { Text = space.DisplayName };
                            Space1ListView.Items.Add(item);
                        }
                    }
                }

                // Similar for Space 2 and Space 3
                if (Space2Apps.Count == 0)
                {
                    if (Space2ListView != null)
                    {
                        var placeholder = new TextBlock { Text = "No apps in Space 2", FontStyle = Windows.UI.Text.FontStyle.Italic };
                        Space2ListView.Items.Add(placeholder);
                    }
                }
                else
                {
                    foreach (var space in Space2Apps)
                    {
                        if (Space2ListView != null)
                        {
                            var item = new TextBlock { Text = space.DisplayName };
                            Space2ListView.Items.Add(item);
                        }
                    }
                }

                if (Space3Apps.Count == 0)
                {
                    if (Space3ListView != null)
                    {
                        var placeholder = new TextBlock { Text = "No apps in Space 3", FontStyle = Windows.UI.Text.FontStyle.Italic };
                        Space3ListView.Items.Add(placeholder);
                    }
                }
                else
                {
                    foreach (var space in Space3Apps)
                    {
                        if (Space3ListView != null)
                        {
                            var item = new TextBlock { Text = space.DisplayName };
                            Space3ListView.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading space board data: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data for detail list view based on category
        /// </summary>
        private void LoadDetailListData(string category)
        {
            try
            {
                if (DetailListViewContent == null) return;

                DetailListViewContent.Items.Clear();

                switch (category)
                {
                    case "High":
                        foreach (var app in HighPriorityNotifications)
                        {
                            var item = new TextBlock { Text = $"{app.DisplayName} - {app.NotificationCount} notifications" };
                            DetailListViewContent.Items.Add(item);
                        }
                        break;

                    case "Medium":
                        foreach (var app in MediumPriorityNotifications)
                        {
                            var item = new TextBlock { Text = $"{app.DisplayName} - {app.NotificationCount} notifications" };
                            DetailListViewContent.Items.Add(item);
                        }
                        break;

                    case "Low":
                        foreach (var app in LowPriorityNotifications)
                        {
                            var item = new TextBlock { Text = $"{app.DisplayName} - {app.NotificationCount} notifications" };
                            DetailListViewContent.Items.Add(item);
                        }
                        break;

                    case "Space1":
                        foreach (var space in Space1Apps)
                        {
                            var item = new TextBlock { Text = $"{space.DisplayName} - {space.NotificationCount} notifications" };
                            DetailListViewContent.Items.Add(item);
                        }
                        break;

                    case "Space2":
                        foreach (var space in Space2Apps)
                        {
                            var item = new TextBlock { Text = $"{space.DisplayName} - {space.NotificationCount} notifications" };
                            DetailListViewContent.Items.Add(item);
                        }
                        break;

                    case "Space3":
                        foreach (var space in Space3Apps)
                        {
                            var item = new TextBlock { Text = $"{space.DisplayName} - {space.NotificationCount} notifications" };
                            DetailListViewContent.Items.Add(item);
                        }
                        break;
                }

                if (DetailListViewContent.Items.Count == 0)
                {
                    var placeholder = new TextBlock { Text = $"No items in {category}", FontStyle = Windows.UI.Text.FontStyle.Italic };
                    DetailListViewContent.Items.Add(placeholder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading detail list data for {category}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data for all apps view
        /// </summary>
        private async Task LoadAllAppsData()
        {
            try
            {
                if (AllAppsListView == null) return;

                AllAppsListView.Items.Clear();

                await LoadAllApplicationsData();

                foreach (var app in AllApplicationsInfo)
                {
                    var item = new TextBlock { Text = $"{app.DisplayName} - {app.Name}" };
                    AllAppsListView.Items.Add(item);
                }

                if (AllAppsListView.Items.Count == 0)
                {
                    var placeholder = new TextBlock { Text = "No applications found", FontStyle = Windows.UI.Text.FontStyle.Italic };
                    AllAppsListView.Items.Add(placeholder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading all apps data: {ex.Message}");
            }
        }

        #endregion

        #region Service Factory Methods

        /// <summary>
        /// Gets or creates custom priority service instance
        /// </summary>
        private async Task<CustomPriorityService?> GetCustomPriorityServiceAsync()
        {
            try
            {
                // Get database handler (implement based on your DI setup)
                var dbHandler = await GetDatabaseHandlerAsync();
                if (dbHandler == null) return null;

                return new CustomPriorityService(dbHandler, "default_user");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating custom priority service: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets or creates space management service instance
        /// </summary>
        private async Task<SpaceManagementService?> GetSpaceManagementServiceAsync()
        {
            try
            {
                return new SpaceManagementService();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating space management service: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets database handler instance (implement based on your DI setup)
        /// </summary>
        private async Task<INotifyDBHandler?> GetDatabaseHandlerAsync()
        {
            try
            {
                // TODO: Implement based on your dependency injection setup
                // For now, return a basic implementation
                // You'll need to inject this properly in a real implementation
                return null; // Replace with actual implementation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting database handler: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles adding apps to priority from board view buttons
        /// </summary>
        private void AddToPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string priorityLevel)
                {
                    // For now, just show a message
                    ShowStatusMessage($"Add apps to {priorityLevel} priority feature coming soon", true);
                    
                    // TODO: Open app selection dialog
                    // var dialog = new AddAppToPriorityDialog(priorityLevel);
                    // await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddToPriority_Click: {ex.Message}");
                ShowStatusMessage($"Error adding to priority: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles adding apps to space from board view buttons
        /// </summary>
        private void AddToSpace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string spaceId)
                {
                    // For now, just show a message
                    ShowStatusMessage($"Add apps to {spaceId} feature coming soon", true);
                    
                    // TODO: Open app selection dialog
                    // var dialog = new AddAppToSpaceDialog(spaceId);
                    // await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddToSpace_Click: {ex.Message}");
                ShowStatusMessage($"Error adding to space: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles adding apps to priority - shows app selection dialog
        /// </summary>
        private async void AddAppToPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string priorityLevel)
                {
                    var priority = Enum.Parse<Priority>(priorityLevel);
                    await ShowAddAppToPriorityDialogAsync(priority);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding app to priority: {ex.Message}");
                ShowStatusMessage($"Error adding app to priority: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Handles adding apps to space - shows app selection dialog
        /// </summary>
        private async void AddAppToSpace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string spaceId)
                {
                    await ShowAddAppToSpaceDialogAsync(spaceId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding app to space: {ex.Message}");
                ShowStatusMessage($"Error adding app to space: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Shows dialog for adding apps to priority
        /// </summary>
        private async Task ShowAddAppToPriorityDialogAsync(Priority priority)
        {
            try
            {
                var customPriorityService = await GetCustomPriorityServiceAsync();
                if (customPriorityService == null)
                {
                    ShowStatusMessage("Custom priority service not available", false);
                    return;
                }

                var dialog = new AddAppToPriorityDialog(customPriorityService, priority);
                dialog.XamlRoot = this.Content.XamlRoot;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Refresh priority data
                    LoadPriorityData();
                    UpdateStatistics();
                    ShowStatusMessage($"Apps added to {priority} priority", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing add app to priority dialog: {ex.Message}");
                ShowStatusMessage($"Error showing dialog: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Shows dialog for adding apps to space
        /// </summary>
        private async Task ShowAddAppToSpaceDialogAsync(string spaceId)
        {
            try
            {
                var customPriorityService = await GetCustomPriorityServiceAsync();
                if (customPriorityService == null)
                {
                    ShowStatusMessage("Custom priority service not available", false);
                    return;
                }

                var dialog = new AddAppToSpaceDialog(customPriorityService, spaceId);
                dialog.XamlRoot = this.Content.XamlRoot;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Refresh space data
                    LoadSpaceData();
                    UpdateStatistics();
                    ShowStatusMessage("Apps added to space", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing add app to space dialog: {ex.Message}");
                ShowStatusMessage($"Error showing dialog: {ex.Message}", false);
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
                var settingsPanel = new SettingsStatusPanel(_dndService, _notificationPositioner);
                settingsPanel.XamlRoot = this.Content.XamlRoot;
                await settingsPanel.ShowAsync();
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
            try
            {
                var settingsPanel = new SettingsStatusPanel(_dndService, _notificationPositioner);
                settingsPanel.XamlRoot = this.Content.XamlRoot;
                await settingsPanel.ShowAsync();

                string diagnostics = GetDndDiagnosticInfo();
                Debug.WriteLine($"Diagnostics:\n{diagnostics}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting diagnostics: {ex.Message}");
                ShowStatusMessage($"Error getting diagnostics: {ex.Message}", false);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Updates the statistics display
        /// </summary>
        private void UpdateStatistics()
        {
            try
            {
                int totalNotifications = HighPriorityNotifications.Sum(a => a.NotificationCount) +
                                       MediumPriorityNotifications.Sum(a => a.NotificationCount) +
                                       LowPriorityNotifications.Sum(a => a.NotificationCount);

                int totalApps = AllApplications.Count;
                int activeSpaces = Space1Apps.Count + Space2Apps.Count + Space3Apps.Count;

                ApplicationVM.TotalNotifications = totalNotifications;
                ApplicationVM.TotalApps = totalApps;
                ApplicationVM.TotalSpaces = activeSpaces;

                if (TotalNotificationsText != null)
                    TotalNotificationsText.Text = $"Total Notifications: {totalNotifications}";
                else
                    Debug.WriteLine("Warning: TotalNotificationsText is null");

                if (PriorityAppsCountText != null)
                    PriorityAppsCountText.Text = $"Priority Apps: {HighPriorityNotifications.Count + MediumPriorityNotifications.Count + LowPriorityNotifications.Count}";
                else
                    Debug.WriteLine("Warning: PriorityAppsCountText is null");

                if (ActiveSpacesText != null)
                    ActiveSpacesText.Text = $"Active Spaces: {activeSpaces}";
                else
                    Debug.WriteLine("Warning: ActiveSpacesText is null");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
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
            UpdateStatistics();
        }

        public async void CreateKToastModel(UserNotification notif)
        {
            try
            {
                string appDisplayName = notif.AppInfo.DisplayInfo.DisplayName;
                string appId = notif.AppInfo.AppUserModelId;
                uint notificationId = notif.Id;

                string? s2 = notif.AppInfo.PackageFamilyName;
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
                        PackageId = appId
                    };

                    KPackageProfile packageProfile = new KPackageProfile()
                    {
                        PackageFamilyName = notif.AppInfo.PackageFamilyName,
                        PackageId = appId,
                        LogoFilePath = iconLocation,
                        AppDescription = string.Empty,
                        AppDisplayName = notif.AppInfo.DisplayInfo.DisplayName
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
                    UpdateStatistics();
                }
            });
        }

        #endregion

        #region Legacy Methods (for compatibility)

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ShowStatusMessage("Test button clicked", true);
        }

        private void SpaceControl_SpaceSelected(string spaceId)
        {
            try
            {
                if (KToastListViewControl != null)
                {
                    KToastListViewControl.GetPackageBySpace(spaceId);
                    ShowStatusMessage($"Space selected: {spaceId}", true);
                }
                else
                {
                    Debug.WriteLine("Warning: KToastListViewControl is null in SpaceControl_SpaceSelected");
                    ShowStatusMessage("Error: Notification control not available", false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SpaceControl_SpaceSelected: {ex.Message}");
                ShowStatusMessage($"Error selecting space: {ex.Message}", false);
            }
        }

        private void GetAllPackage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (KToastListViewControl != null)
                {
                    KToastListViewControl.UpdateToastView(ViewType.Package);
                    ShowStatusMessage("Showing all packages", true);
                }
                else
                {
                    Debug.WriteLine("Warning: KToastListViewControl is null in GetAllPackage_Click");
                    ShowStatusMessage("Error: Notification control not available", false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllPackage_Click: {ex.Message}");
                ShowStatusMessage($"Error showing packages: {ex.Message}", false);
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
                _dndService?.Dispose();
                _notificationPositioner = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing services: {ex.Message}");
            }
        }
    }
}
