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
    /// Simple dialog for adding apps to priority lists - Code-behind only approach
    /// </summary>
    public sealed partial class AddAppDialog : ContentDialog
    {
        private ListView _appsList;
        private TextBox _searchBox;
        
        public ObservableCollection<DndService.PriorityApp> AvailableApps { get; set; } = new();
        public DndService.PriorityApp? SelectedApp { get; private set; }

        public AddAppDialog()
        {
            this.PrimaryButtonText = "Add to Priority";
            this.CloseButtonText = "Cancel";
            this.Title = "Add App to Priority List";
            this.DefaultButton = ContentDialogButton.Primary;
            this.IsPrimaryButtonEnabled = false;
            
            CreateContent();
            LoadAvailableApps();
        }

        private void CreateContent()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Instructions
            var instructions = new TextBlock
            {
                Text = "Select an app to add to your priority notification list:",
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(instructions, 0);
            mainGrid.Children.Add(instructions);

            // Search Box
            _searchBox = new TextBox
            {
                PlaceholderText = "Search apps...",
                Margin = new Thickness(0, 0, 0, 15)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            Grid.SetRow(_searchBox, 1);
            mainGrid.Children.Add(_searchBox);

            // Apps List
            _appsList = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single
            };
            _appsList.SelectionChanged += AppsList_SelectionChanged;
            _appsList.ItemsSource = AvailableApps;
            
            // Create item template programmatically
            var itemTemplate = new DataTemplate();
            _appsList.ItemTemplate = itemTemplate;
            
            Grid.SetRow(_appsList, 2);
            mainGrid.Children.Add(_appsList);

            this.Content = mainGrid;
        }

        private async void LoadAvailableApps()
        {
            try
            {
                var installedApps = await InstalledAppsService.GetAllInstalledAppsAsync();
                var dndService = new DndService();
                var availableApps = dndService.GetAvailableAppsForPriority(installedApps);

                AvailableApps.Clear();
                foreach (var app in availableApps.Take(50)) // Limit for performance
                {
                    if (!string.IsNullOrEmpty(app.DisplayName))
                    {
                        AvailableApps.Add(app);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available apps: {ex.Message}");
            }
        }

        private void AppsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is DndService.PriorityApp app)
            {
                SelectedApp = app;
                this.IsPrimaryButtonEnabled = true;
            }
            else
            {
                SelectedApp = null;
                this.IsPrimaryButtonEnabled = false;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox searchBox)
            {
                string searchText = searchBox.Text.ToLower();
                
                if (string.IsNullOrEmpty(searchText))
                {
                    _appsList.ItemsSource = AvailableApps;
                }
                else
                {
                    var filteredApps = AvailableApps.Where(app => 
                        app.DisplayName.ToLower().Contains(searchText) ||
                        app.Publisher.ToLower().Contains(searchText)).ToList();
                    
                    var filteredCollection = new ObservableCollection<DndService.PriorityApp>(filteredApps);
                    _appsList.ItemsSource = filteredCollection;
                }
            }
        }
    }
}