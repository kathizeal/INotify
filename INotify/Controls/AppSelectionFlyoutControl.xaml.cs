using AppList; // For InstalledAppsService
using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            IntilizeDI();
            this.InitializeComponent();
        }

        public void IntilizeDI()
        {
            _VM = KToastDIServiceProvider.Instance.GetService<AppSelectionViewModelBase>();

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(_VM == null)
            {
                IntilizeDI();
            }

            _VM.FilteredApps.Clear();

            _VM.GetInstalledApps();
            AppsList.ItemsSource = _VM.PackageProfiles;
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