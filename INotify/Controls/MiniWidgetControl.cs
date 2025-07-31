using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using INotify.ViewModels;

namespace INotify.Controls
{
    /// <summary>
    /// Mini widget control for compact notification management
    /// </summary>
    public sealed partial class MiniWidgetControl : UserControl
    {
        public ApplicationViewModel ApplicationVM { get; set; }
        
        public event EventHandler<RoutedEventArgs>? SwitchToNormalMode;
        public event EventHandler<RoutedEventArgs>? ToggleDnd;
        public event EventHandler<RoutedEventArgs>? OpenSettings;
        public event EventHandler<RoutedEventArgs>? OpenQuickActions;

        public MiniWidgetControl()
        {
            CreateContent();
        }

        private void CreateContent()
        {
            var mainBorder = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(20),
                Width = 300,
                Height = 200
            };

            var mainStack = new StackPanel { Spacing = 10 };

            // Header
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "INotify Widget",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 16
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var expandButton = new Button
            {
                Content = "??",
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 16,
                Padding = new Thickness(5)
            };
            expandButton.Click += ExpandButton_Click;
            Grid.SetColumn(expandButton, 1);
            headerGrid.Children.Add(expandButton);

            mainStack.Children.Add(headerGrid);

            // Stats Section
            var statsStack = new StackPanel { Spacing = 5 };

            var notificationsText = new TextBlock
            {
                Text = "?? Notifications: Loading...",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 12
            };
            statsStack.Children.Add(notificationsText);

            var appsText = new TextBlock
            {
                Text = "?? Apps: Loading...",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 12
            };
            statsStack.Children.Add(appsText);

            var spacesText = new TextBlock
            {
                Text = "?? Spaces: Loading...",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 12
            };
            statsStack.Children.Add(spacesText);

            var dndStatusText = new TextBlock
            {
                Text = "?? DND: OFF",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            statsStack.Children.Add(dndStatusText);

            mainStack.Children.Add(statsStack);

            // Quick Actions
            var actionsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var dndButton = new Button
            {
                Content = "??",
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)
            };
            ToolTipService.SetToolTip(dndButton, "Toggle Do Not Disturb");
            dndButton.Click += DndButton_Click;
            actionsStack.Children.Add(dndButton);

            var settingsButton = new Button
            {
                Content = "??",
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)
            };
            ToolTipService.SetToolTip(settingsButton, "Settings");
            settingsButton.Click += SettingsButton_Click;
            actionsStack.Children.Add(settingsButton);

            var quickActionsButton = new Button
            {
                Content = "?",
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)
            };
            ToolTipService.SetToolTip(quickActionsButton, "Quick Actions");
            quickActionsButton.Click += QuickActionsButton_Click;
            actionsStack.Children.Add(quickActionsButton);

            var refreshButton = new Button
            {
                Content = "??",
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4)
            };
            ToolTipService.SetToolTip(refreshButton, "Refresh");
            refreshButton.Click += RefreshButton_Click;
            actionsStack.Children.Add(refreshButton);

            mainStack.Children.Add(actionsStack);

            mainBorder.Child = mainStack;
            this.Content = mainBorder;

            // Store references for updates
            this.Tag = new
            {
                NotificationsText = notificationsText,
                AppsText = appsText,
                SpacesText = spacesText,
                DndStatusText = dndStatusText,
                DndButton = dndButton
            };
        }

        /// <summary>
        /// Updates the widget statistics
        /// </summary>
        public void UpdateStatistics(int notifications, int apps, int spaces, bool isDndEnabled)
        {
            try
            {
                var refs = this.Tag as dynamic;
                if (refs != null)
                {
                    refs.NotificationsText.Text = $"?? Notifications: {notifications}";
                    refs.AppsText.Text = $"?? Apps: {apps}";
                    refs.SpacesText.Text = $"?? Spaces: {spaces}";
                    refs.DndStatusText.Text = isDndEnabled ? "?? DND: ON" : "?? DND: OFF";
                    refs.DndButton.Content = isDndEnabled ? "??" : "??";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating widget statistics: {ex.Message}");
            }
        }

        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToNormalMode?.Invoke(this, e);
        }

        private void DndButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleDnd?.Invoke(this, e);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings?.Invoke(this, e);
        }

        private void QuickActionsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenQuickActions?.Invoke(this, e);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Animate refresh button
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    await Task.Delay(500); // Simulate refresh time
                    button.IsEnabled = true;
                }
                
                // Trigger refresh event if needed
                OpenQuickActions?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in refresh: {ex.Message}");
            }
        }

        /// <summary>
        /// Animates the widget appearance
        /// </summary>
        public async Task AnimateIn()
        {
            try
            {
                this.Opacity = 0;
                this.Visibility = Visibility.Visible;
                
                // Simple fade in animation
                for (double opacity = 0; opacity <= 1; opacity += 0.1)
                {
                    this.Opacity = opacity;
                    await Task.Delay(30);
                }
                
                this.Opacity = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error animating widget: {ex.Message}");
                this.Opacity = 1;
            }
        }

        /// <summary>
        /// Animates the widget disappearance
        /// </summary>
        public async Task AnimateOut()
        {
            try
            {
                // Simple fade out animation
                for (double opacity = 1; opacity >= 0; opacity -= 0.1)
                {
                    this.Opacity = opacity;
                    await Task.Delay(30);
                }
                
                this.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error animating widget out: {ex.Message}");
                this.Visibility = Visibility.Collapsed;
            }
        }
    }
}