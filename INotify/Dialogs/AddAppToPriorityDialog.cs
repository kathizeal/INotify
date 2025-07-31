using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using INotify.Services;
using INotify.ViewModels;
using INotifyLibrary.Util.Enums;

namespace INotify.Dialogs
{
    /// <summary>
    /// Dialog for adding apps to a specific priority level
    /// </summary>
    public sealed partial class AddAppToPriorityDialog : ContentDialog
    {
        private readonly CustomPriorityService _customPriorityService;
        private readonly Priority _targetPriority;
        private ListView _availableAppsList;
        private TextBox _searchBox;
        
        public ObservableCollection<PriorityPackageViewModel> AvailableApps { get; set; } = new();
        public ObservableCollection<PriorityPackageViewModel> SelectedApps { get; set; } = new();

        public AddAppToPriorityDialog(CustomPriorityService customPriorityService, Priority targetPriority)
        {
            _customPriorityService = customPriorityService ?? throw new ArgumentNullException(nameof(customPriorityService));
            _targetPriority = targetPriority;
            
            this.Title = $"Add Apps to {targetPriority} Priority";
            this.PrimaryButtonText = "Add Selected Apps";
            this.SecondaryButtonText = "Cancel";
            this.DefaultButton = ContentDialogButton.Primary;
            
            CreateContent();
            LoadAvailableApps();
        }

        private void CreateContent()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerText = new TextBlock
            {
                Text = $"Select applications to add to {_targetPriority} priority:",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(headerText, 0);
            mainGrid.Children.Add(headerText);

            // Search Box
            _searchBox = new TextBox
            {
                PlaceholderText = "Search applications...",
                Margin = new Thickness(0, 0, 0, 15)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            Grid.SetRow(_searchBox, 1);
            mainGrid.Children.Add(_searchBox);

            // Available Apps List
            _availableAppsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Multiple,
                Height = 400
            };
            _availableAppsList.ItemsSource = AvailableApps;
            _availableAppsList.SelectionChanged += AvailableAppsList_SelectionChanged;
            Grid.SetRow(_availableAppsList, 2);
            mainGrid.Children.Add(_availableAppsList);

            // Status
            var statusText = new TextBlock
            {
                Text = "Select multiple apps using Ctrl+Click or Shift+Click",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(statusText, 3);
            mainGrid.Children.Add(statusText);

            this.Content = mainGrid;
        }

        private async void LoadAvailableApps()
        {
            try
            {
                var allApps = await _customPriorityService.GetAllApplicationsWithPriorityAsync();
                
                // Filter out apps that already have the target priority
                var availableApps = allApps
                    .Where(app => app.Priority != _targetPriority)
                    .OrderBy(app => app.DisplayName)
                    .ToList();

                AvailableApps.Clear();
                foreach (var app in availableApps)
                {
                    AvailableApps.Add(app);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available apps: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                string searchText = searchBox.Text.ToLower();
                
                if (string.IsNullOrEmpty(searchText))
                {
                    _availableAppsList.ItemsSource = AvailableApps;
                }
                else
                {
                    var filteredApps = AvailableApps
                        .Where(app => 
                            app.DisplayName.ToLower().Contains(searchText) ||
                            app.Publisher.ToLower().Contains(searchText))
                        .ToList();
                    
                    var filteredCollection = new ObservableCollection<PriorityPackageViewModel>(filteredApps);
                    _availableAppsList.ItemsSource = filteredCollection;
                }
            }
        }

        private void AvailableAppsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedApps.Clear();
            
            foreach (PriorityPackageViewModel item in _availableAppsList.SelectedItems)
            {
                SelectedApps.Add(item);
            }

            // Update primary button state
            this.IsPrimaryButtonEnabled = SelectedApps.Count > 0;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true; // Cancel the close, we'll close manually after processing
            
            try
            {
                this.IsPrimaryButtonEnabled = false;
                this.IsSecondaryButtonEnabled = false;

                bool allSuccessful = true;
                int successCount = 0;

                foreach (var app in SelectedApps)
                {
                    var success = await _customPriorityService.SetAppPriorityAsync(
                        app.PackageId, app.DisplayName, app.Publisher, _targetPriority);
                    
                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        allSuccessful = false;
                    }
                }

                if (allSuccessful)
                {
                    // Close with Primary result
                    this.Hide();
                }
                else
                {
                    // Show error but allow retry
                    var errorMessage = $"Added {successCount} of {SelectedApps.Count} apps successfully. Some apps failed to be added.";
                    // You could show an error dialog here
                    System.Diagnostics.Debug.WriteLine(errorMessage);
                    
                    this.IsPrimaryButtonEnabled = true;
                    this.IsSecondaryButtonEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding apps to priority: {ex.Message}");
                this.IsPrimaryButtonEnabled = true;
                this.IsSecondaryButtonEnabled = true;
            }
        }
    }
}