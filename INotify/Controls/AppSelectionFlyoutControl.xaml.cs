using AppList; // For InstalledAppsService
using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Domain;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;

namespace INotify.Controls
{
    /// <summary>
    /// Reusable component for app selection in flyouts with full installed apps list and priority awareness
    /// Uses existing KPackageProfileVObj instead of redundant PriorityPackageViewModel
    /// UI defined in XAML following WinUI best practices
    /// </summary>
    public sealed partial class AppSelectionFlyoutControl : UserControl
    {

        private AppSelectionViewModelBase _VM;

        public SelectionTargetType CurrentTargetType
        {
            get { return (SelectionTargetType)GetValue(CurrentTargetTypeProperty); }
            set { SetValue(CurrentTargetTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentTargetType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentTargetTypeProperty =
            DependencyProperty.Register("CurrentTargetType", typeof(SelectionTargetType), typeof(AppSelectionFlyoutControl), new PropertyMetadata(default));



        public string SelectionTypeId
        {
            get { return (string)GetValue(SelectionTypeIdProperty); }
            set { SetValue(SelectionTypeIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionTypeId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionTypeIdProperty =
            DependencyProperty.Register("SelectionTypeId", typeof(string), typeof(AppSelectionFlyoutControl), new PropertyMetadata(default));




        public AppSelectionFlyoutControl()
        {
            try
            {
                this.InitializeComponent();
                IntilizeDI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AppSelectionFlyoutControl constructor: {ex.Message}");
            }
        }

        public void IntilizeDI()
        {
            try
            {
                _VM = KToastDIServiceProvider.Instance.GetService<AppSelectionViewModelBase>();
                if (_VM == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: AppSelectionViewModelBase service not available from DI container");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing DI in AppSelectionFlyoutControl: {ex.Message}");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if(_VM == null)
                {
                    IntilizeDI();
                }

                if (_VM != null)
                {
                    _VM.FilteredApps.Clear();
                    _VM.GetInstalledApps();
                    _VM.GetAppPackageProfile();
                    AppsList.ItemsSource = _VM.PackageProfiles;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UserControl_Loaded: {ex.Message}");
            }
        }
        #region Events

        /// <summary>
        /// Event fired when apps are selected and Add button is clicked
        /// </summary>
        public event EventHandler<AppSelectionEventArgs>? AppsSelected;

        /// <summary>
        /// Event fired when Cancel button is clicked
        /// </summary>
        public event EventHandler? Cancelled;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the header text for the flyout
        /// </summary>
        public string HeaderTextValue
        {
            get => HeaderText.Text;
            set => HeaderText.Text = value;
        }

        /// <summary>
        /// Gets the selected apps
        /// </summary>
        public IEnumerable<KPackageProfileVObj> SelectedApps =>
            _VM.PackageProfiles.Where(app => app.IsSelected);

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads all installed applications with their current priority status
        /// </summary>
        /// <param name="customPriorityService">Service to load apps</param>
        /// <param name="targetType">Target type (Priority or Space)</param>
        /// <param name="targetValue">Target value (High/Medium/Low for Priority, Space1/Space2/Space3 for Space)</param>
        //public async Task LoadAppsAsync(CustomPriorityService customPriorityService, SelectionTargetType targetType, string targetValue)
        //{
        //    try
        //    {
        //        _currentTargetType = targetType;
        //        _currentTargetValue = targetValue;

        //        // Load all installed applications

        //        _VM.PackageProfiles.Clear();

               

        //        UpdateSelectionStatus();

        //        // Set appropriate header text
        //        HeaderTextValue = targetType switch
        //        {
        //            SelectionTargetType.Priority => $"Add Apps to {targetValue} Priority",
        //            SelectionTargetType.Space => $"Add Apps to {GetSpaceDisplayName(targetValue)}",
        //            _ => "Add Apps"
        //        };

        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error loading flyout apps data: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// Clears all selections
        /// </summary>
        public void ClearSelections()
        {
            foreach (var app in _VM.PackageProfiles)
            {
                app.IsSelected = false;
            }
            UpdateSelectionStatus();
        }

        #endregion

        #region Private Methods

        private string GetSpaceDisplayName(string spaceId) => spaceId switch
        {
            "Space1" => "Space 1",
            "Space2" => "Space 2",
            "Space3" => "Space 3",
            _ => spaceId
        };

        private void UpdateSelectionStatus()
        {
            int selectedCount = _VM.PackageProfiles.Count(app => app.IsSelected);

            var statusText = $"{selectedCount} app{(selectedCount != 1 ? "s" : "")} selected";

            if (CurrentTargetType == SelectionTargetType.Priority && selectedCount > 0)
            {
                var appsWithExistingPriority = _VM.PackageProfiles
                    .Where(app => app.IsSelected && app.Priority != Priority.None)
                    .Count();

                if (appsWithExistingPriority > 0)
                {
                    statusText += $" ({appsWithExistingPriority} will replace existing priority)";
                }
            }

            SelectionStatus.Text = statusText;
            AddButton.IsEnabled = selectedCount > 0;
        }

        private void FilterApps(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                _VM.FilteredApps.Clear();
                AppsList.ItemsSource = _VM.PackageProfiles;
               
            }
            else
            {
                var filtered = _VM.PackageProfiles
                    .Where(app =>
                        app.DisplayName.ToLower().Contains(searchText.ToLower()) ||
                        app.Publisher.ToLower().Contains(searchText.ToLower()))
                    .ToList();

                _VM.FilteredApps.Clear();
                foreach (var app in filtered)
                {
                    _VM.FilteredApps.Add(app);
                }
            }

            AppsList.ItemsSource = _VM.FilteredApps;
        }

        /// <summary>
        /// Shows an internal in-app toast notification when apps are successfully added
        /// </summary>
        private void ShowInAppToast(int appCount, SelectionTargetType targetType, string targetId)
        {
            try
            {
                // Create success message
                var targetName = targetType switch
                {
                    SelectionTargetType.Priority => $"{targetId} Priority",
                    SelectionTargetType.Space => GetSpaceDisplayName(targetId),
                    _ => "Category"
                };

                var message = appCount == 1 
                    ? $"? 1 app successfully added to {targetName}"
                    : $"? {appCount} apps successfully added to {targetName}";

                // Find the main window through the visual tree
                var mainWindow = GetMainWindow();
                if (mainWindow != null)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ShowToastInMainWindow(mainWindow, message, targetName);
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"? Apps added: {message} (Main window not found for in-app toast)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error showing in-app toast: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows toast notification within the main window
        /// </summary>
        private void ShowToastInMainWindow(Window mainWindow, string message, string targetName)
        {
            try
            {
                if (mainWindow.Content is FrameworkElement rootElement)
                {
                    // Find the main content area
                    var contentArea = FindElementByName(rootElement, "ContentArea") as Grid;
                    
                    if (contentArea != null)
                    {
                        // Create in-app toast using InfoBar
                        var infoBar = new InfoBar
                        {
                            Title = "?? Success!",
                            Message = message,
                            Severity = InfoBarSeverity.Success,
                            IsOpen = true,
                            IsClosable = true,
                            Margin = new Thickness(24, 16, 24, 0),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top
                        };

                        // Set high Z-index to ensure it appears on top
                        Canvas.SetZIndex(infoBar, 9999);

                        // Auto-close after 4 seconds
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            infoBar.IsOpen = false;
                            
                            // Remove from parent after close animation
                            var removeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                            removeTimer.Tick += (s2, e2) =>
                            {
                                removeTimer.Stop();
                                try
                                {
                                    contentArea.Children.Remove(infoBar);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error removing toast: {ex.Message}");
                                }
                            };
                            removeTimer.Start();
                        };
                        timer.Start();

                        // Add to the content area at the top
                        contentArea.Children.Add(infoBar);
                        Grid.SetRow(infoBar, 0);
                        Grid.SetColumnSpan(infoBar, 10); // Span across all columns
                        
                        System.Diagnostics.Debug.WriteLine($"? In-app toast displayed: {message}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("?? Could not find ContentArea in main window for in-app toast");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error showing toast in main window: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the main window from the current context
        /// </summary>
        private Window GetMainWindow()
        {
            try
            {
                // Try to get the main window through App.Current
                if (Application.Current is App app)
                {
                    // Get the main window using reflection (adjust based on your App class structure)
                    var windowField = typeof(App).GetField("m_window", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (windowField != null)
                    {
                        return windowField.GetValue(app) as Window;
                    }

                    // Alternative: try public property if available
                    var windowProperty = typeof(App).GetProperty("MainWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (windowProperty != null)
                    {
                        return windowProperty.GetValue(app) as Window;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting main window: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Finds an element by name in the visual tree
        /// </summary>
        private FrameworkElement FindElementByName(FrameworkElement parent, string name)
        {
            try
            {
                if (parent.Name == name)
                    return parent;

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
                    if (child != null)
                    {
                        var result = FindElementByName(child, name);
                        if (result != null)
                            return result;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding element by name: {ex.Message}");
            }
            
            return null;
        }

        #endregion

        #region Event Handlers

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                FilterApps(searchBox.Text);
            }
        }

        private void AppCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is KPackageProfileVObj app)
            {
                app.IsSelected = true;
                UpdateSelectionStatus();
            }
        }

        private void AppCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is KPackageProfileVObj app)
            {
                app.IsSelected = false;
                UpdateSelectionStatus();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApps = SelectedApps.ToList();
            if (selectedApps.Count > 0)
            {
                var appSelectionEventArgs = new AppSelectionEventArgs(selectedApps, CurrentTargetType, SelectionTypeId);
                _VM.AddSelectedAppsToCondition(appSelectionEventArgs);

                // Show internal in-app toast notification
                ShowInAppToast(selectedApps.Count, CurrentTargetType, SelectionTypeId);

                AppsSelected?.Invoke(this, appSelectionEventArgs);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        #endregion

       
    }

    #region Supporting Classes

    /// <summary>
    /// Event arguments for app selection
    /// </summary>
    
    /// <summary>
    /// Target type for selection
    /// </summary>
    

    #endregion
}