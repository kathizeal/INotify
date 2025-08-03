using AppList; // For InstalledAppsService
using INotify.KToastView.Model;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<KPackageProfileVObj> _allApps = new();
        private ObservableCollection<KPackageProfileVObj> _filteredApps = new();
        private SelectionTargetType _currentTargetType;
        private string _currentTargetValue = string.Empty;

        public AppSelectionFlyoutControl()
        {
            this.InitializeComponent();
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
            _allApps.Where(app => app.IsSelected);

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
        //        var installedApps = await InstalledAppsService.GetAllInstalledAppsAsync();
        //        var customPriorityApps = await customPriorityService.GetCustomPriorityAppsAsync();

        //        // Create lookup for existing priorities
        //        var priorityLookup = new Dictionary<string, Priority>();
        //        foreach (var app in customPriorityApps)
        //        {
        //            priorityLookup[app.PackageId] = app.Priority;
        //        }

        //        _allApps.Clear();
        //        _filteredApps.Clear();

        //        foreach (var app in installedApps)
        //        {
        //            if (string.IsNullOrWhiteSpace(app.DisplayName))
        //                continue;

        //            var packageId = GeneratePackageId(app);
        //            var currentPriority = priorityLookup.GetValueOrDefault(packageId, Priority.None);
        //            var notificationCount = await GetNotificationCountForApp(customPriorityService, packageId);

        //            var packageProfileVObj = KPackageProfileVObj.FromInstalledAppInfo(app, currentPriority, notificationCount);

        //            _allApps.Add(packageProfileVObj);
        //            _filteredApps.Add(packageProfileVObj);
        //        }

        //        AppsList.ItemsSource = _filteredApps;
        //        UpdateSelectionStatus();

        //        // Set appropriate header text
        //        HeaderTextValue = targetType switch
        //        {
        //            SelectionTargetType.Priority => $"Add Apps to {targetValue} Priority",
        //            SelectionTargetType.Space => $"Add Apps to {GetSpaceDisplayName(targetValue)}",
        //            _ => "Add Apps"
        //        };

        //        Debug.WriteLine($"Loaded {_allApps.Count} apps for {targetType} {targetValue} flyout");
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
            foreach (var app in _allApps)
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
            int selectedCount = _allApps.Count(app => app.IsSelected);

            var statusText = $"{selectedCount} app{(selectedCount != 1 ? "s" : "")} selected";

            if (_currentTargetType == SelectionTargetType.Priority && selectedCount > 0)
            {
                var appsWithExistingPriority = _allApps
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
                _filteredApps.Clear();
                foreach (var app in _allApps)
                {
                    _filteredApps.Add(app);
                }
            }
            else
            {
                var filtered = _allApps
                    .Where(app =>
                        app.DisplayName.ToLower().Contains(searchText.ToLower()) ||
                        app.Publisher.ToLower().Contains(searchText.ToLower()))
                    .ToList();

                _filteredApps.Clear();
                foreach (var app in filtered)
                {
                    _filteredApps.Add(app);
                }
            }

            AppsList.ItemsSource = _filteredApps;
        }

        private string GeneratePackageId(InstalledAppInfo app)
        {
            switch (app.Type)
            {
                case AppType.UWPApplication:
                    return !string.IsNullOrEmpty(app.PackageFamilyName) ? app.PackageFamilyName : app.Name;

                case AppType.Win32Application:
                    if (!string.IsNullOrEmpty(app.ExecutablePath))
                    {
                        return System.IO.Path.GetFileNameWithoutExtension(app.ExecutablePath);
                    }
                    return app.DisplayName.Replace(" ", "").Replace(".", "").Replace("-", "");

                default:
                    return app.Name;
            }
        }

        //private async Task<int> GetNotificationCountForApp(CustomPriorityService customPriorityService, string packageId)
        //{
        //    try
        //    {
        //        // This would need to be implemented in CustomPriorityService
        //        // For now, return 0
        //        return 0;
        //    }
        //    catch
        //    {
        //        return 0;
        //    }
        //}

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
                AppsSelected?.Invoke(this, new AppSelectionEventArgs(selectedApps));
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
    public class AppSelectionEventArgs : EventArgs
    {
        public IReadOnlyList<KPackageProfileVObj> SelectedApps { get; }

        public AppSelectionEventArgs(IEnumerable<KPackageProfileVObj> selectedApps)
        {
            SelectedApps = selectedApps.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Target type for selection
    /// </summary>
    public enum SelectionTargetType
    {
        Priority,
        Space
    }

    #endregion
}