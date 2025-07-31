using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AppList;

namespace INotify
{
    /// <summary>
    /// Simple dialog for comprehensive app management - Code-behind only approach
    /// </summary>
    public sealed partial class AppManagementDialog : ContentDialog
    {
        private ListView _allAppsList;
        private ListView _priorityAppsList;
        private TextBox _searchAllApps;
        private TextBox _searchPriorityApps;
        
        public ObservableCollection<DndService.PriorityApp> AllApps { get; set; } = new();
        public ObservableCollection<DndService.PriorityApp> PriorityApps { get; set; } = new();

        private DndService _dndService;

        public AppManagementDialog()
        {
            this.CloseButtonText = "Close";
            this.Title = "App Management";
            
            _dndService = new DndService();
            CreateContent();
            LoadApps();
        }

        private void CreateContent()
        {
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // All Apps Section
            var allAppsSection = CreateAllAppsSection();
            Grid.SetColumn(allAppsSection, 0);
            mainGrid.Children.Add(allAppsSection);

            // Priority Apps Section
            var priorityAppsSection = CreatePriorityAppsSection();
            Grid.SetColumn(priorityAppsSection, 1);
            mainGrid.Children.Add(priorityAppsSection);

            this.Content = mainGrid;
        }

        private Border CreateAllAppsSection()
        {
            var border = new Border
            {
                Margin = new Thickness(0, 0, 10, 0),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            var header = new TextBlock
            {
                Text = "All Installed Apps",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Search Box
            _searchAllApps = new TextBox
            {
                PlaceholderText = "Search all apps...",
                Margin = new Thickness(0, 0, 0, 10)
            };
            _searchAllApps.TextChanged += SearchAllApps_TextChanged;
            Grid.SetRow(_searchAllApps, 1);
            grid.Children.Add(_searchAllApps);

            // Stats Panel
            var statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var refreshButton = new Button { Content = "?? Refresh" };
            refreshButton.Click += Refresh_Click;
            statsPanel.Children.Add(refreshButton);
            
            Grid.SetRow(statsPanel, 2);
            grid.Children.Add(statsPanel);

            // Apps List
            _allAppsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.None
            };
            _allAppsList.ItemsSource = AllApps;
            Grid.SetRow(_allAppsList, 3);
            grid.Children.Add(_allAppsList);

            border.Child = grid;
            return border;
        }

        private Border CreatePriorityAppsSection()
        {
            var border = new Border
            {
                Margin = new Thickness(10, 0, 0, 0),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            var header = new TextBlock
            {
                Text = "Priority Apps",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Search Box
            _searchPriorityApps = new TextBox
            {
                PlaceholderText = "Search priority apps...",
                Margin = new Thickness(0, 0, 0, 10)
            };
            _searchPriorityApps.TextChanged += SearchPriorityApps_TextChanged;
            Grid.SetRow(_searchPriorityApps, 1);
            grid.Children.Add(_searchPriorityApps);

            // Stats Panel
            var statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            Grid.SetRow(statsPanel, 2);
            grid.Children.Add(statsPanel);

            // Priority Apps List
            _priorityAppsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.None
            };
            _priorityAppsList.ItemsSource = PriorityApps;
            Grid.SetRow(_priorityAppsList, 3);
            grid.Children.Add(_priorityAppsList);

            border.Child = grid;
            return border;
        }

        private async void LoadApps()
        {
            try
            {
                var installedApps = await InstalledAppsService.GetAllInstalledAppsAsync();
                var availableApps = _dndService.GetAvailableAppsForPriority(installedApps);
                var priorityApps = _dndService.GetPriorityApplications();

                AllApps.Clear();
                PriorityApps.Clear();

                foreach (var app in availableApps.Take(50)) // Limit for performance
                {
                    if (!string.IsNullOrEmpty(app.DisplayName))
                    {
                        AllApps.Add(app);
                    }
                }

                foreach (var app in priorityApps)
                {
                    PriorityApps.Add(app);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading apps: {ex.Message}");
            }
        }

        private void AddToPriority_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DndService.PriorityApp app)
            {
                try
                {
                    _dndService.AddToPriorityList(app.AppId, app.DisplayName);
                    LoadApps(); // Refresh
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding to priority: {ex.Message}");
                }
            }
        }

        private void RemoveFromPriority_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DndService.PriorityApp app)
            {
                try
                {
                    _dndService.RemoveFromPriorityList(app.AppId);
                    LoadApps(); // Refresh
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error removing from priority: {ex.Message}");
                }
            }
        }

        private void SearchAllApps_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                string searchText = searchBox.Text.ToLower();
                
                if (string.IsNullOrEmpty(searchText))
                {
                    _allAppsList.ItemsSource = AllApps;
                }
                else
                {
                    var filteredApps = AllApps.Where(app => 
                        app.DisplayName.ToLower().Contains(searchText) ||
                        app.Publisher.ToLower().Contains(searchText)).ToList();
                    
                    var filteredCollection = new ObservableCollection<DndService.PriorityApp>(filteredApps);
                    _allAppsList.ItemsSource = filteredCollection;
                }
            }
        }

        private void SearchPriorityApps_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                string searchText = searchBox.Text.ToLower();
                
                if (string.IsNullOrEmpty(searchText))
                {
                    _priorityAppsList.ItemsSource = PriorityApps;
                }
                else
                {
                    var filteredApps = PriorityApps.Where(app => 
                        app.DisplayName.ToLower().Contains(searchText) ||
                        app.Publisher.ToLower().Contains(searchText)).ToList();
                    
                    var filteredCollection = new ObservableCollection<DndService.PriorityApp>(filteredApps);
                    _priorityAppsList.ItemsSource = filteredCollection;
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadApps();
        }
    }
}