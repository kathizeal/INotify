using AppList;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using SampleNotify;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace INotify
{
    /// <summary>
    /// Settings and status panel for the notification management system
    /// </summary>
    public sealed partial class SettingsStatusPanel : ContentDialog
    {
        private DndService _dndService;
        private StandaloneNotificationPositioner _notificationPositioner;
        private TextBlock _statusDisplay;
        private ScrollViewer _diagnosticsScroll;
        private TextBlock _diagnosticsText;

        public SettingsStatusPanel(DndService dndService, StandaloneNotificationPositioner notificationPositioner)
        {
            _dndService = dndService;
            _notificationPositioner = notificationPositioner;
            
            this.Title = "Settings & Status";
            this.CloseButtonText = "Close";
            this.PrimaryButtonText = "Refresh Status";
            this.DefaultButton = ContentDialogButton.Close;
            
            CreateContent();
            UpdateStatus();
        }

        private void CreateContent()
        {
            var mainGrid = new Grid { Width = 700, Height = 500 };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Status Section
            var statusSection = CreateStatusSection();
            Grid.SetRow(statusSection, 0);
            mainGrid.Children.Add(statusSection);

            // Diagnostics Section
            var diagnosticsSection = CreateDiagnosticsSection();
            Grid.SetRow(diagnosticsSection, 1);
            mainGrid.Children.Add(diagnosticsSection);

            this.Content = mainGrid;
        }

        private StackPanel CreateStatusSection()
        {
            var section = new StackPanel { Spacing = 15, Margin = new Thickness(0, 0, 0, 20) };

            // Header
            var header = new TextBlock
            {
                Text = "System Status",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            section.Children.Add(header);

            // Status Display
            _statusDisplay = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                Margin = new Thickness(10),
                Padding = new Thickness(10)
            };
            
            var statusBorder = new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                CornerRadius = new CornerRadius(5),
                Child = _statusDisplay
            };
            section.Children.Add(statusBorder);

            // Quick Actions
            var actionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var testDndButton = new Button { Content = "Test DND Toggle" };
            testDndButton.Click += TestDndToggle_Click;
            actionsPanel.Children.Add(testDndButton);

            var testPositionButton = new Button { Content = "Test Positioning" };
            testPositionButton.Click += TestPositioning_Click;
            actionsPanel.Children.Add(testPositionButton);

            section.Children.Add(actionsPanel);

            return section;
        }

        private StackPanel CreateDiagnosticsSection()
        {
            var section = new StackPanel { Spacing = 10 };

            // Header
            var header = new TextBlock
            {
                Text = "Diagnostic Information",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            section.Children.Add(header);

            // Diagnostics ScrollViewer
            _diagnosticsScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                Padding = new Thickness(10)
            };

            _diagnosticsText = new TextBlock
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 11,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen),
                TextWrapping = TextWrapping.Wrap
            };

            _diagnosticsScroll.Content = _diagnosticsText;
            section.Children.Add(_diagnosticsScroll);

            return section;
        }

        private void UpdateStatus()
        {
            try
            {
                var statusText = "";
                
                // DND Status
                if (_dndService != null)
                {
                    statusText += $"🔔 DND Status: {_dndService.GetStateDescription()}\n";
                    statusText += $"📱 Priority Apps: {_dndService.GetPriorityApplications().Count}\n";
                }
                else
                {
                    statusText += "🔔 DND Service: Not Available\n";
                }

                // Notification Position
                if (_notificationPositioner != null)
                {
                    statusText += $"📍 Notification Position: {_notificationPositioner.Position}\n";
                    var monitors = _notificationPositioner.GetAvailableMonitors();
                    statusText += $"🖥️ Available Monitors: {monitors.Count}\n";
                }
                else
                {
                    statusText += "📍 Positioning Service: Not Available\n";
                }

                // System Info
                statusText += $"💻 Windows Version: {Environment.OSVersion}\n";
                statusText += $"⏰ Last Updated: {DateTime.Now:HH:mm:ss}\n";

                _statusDisplay.Text = statusText;

                // Update Diagnostics
                if (_dndService != null)
                {
                    _diagnosticsText.Text = _dndService.GetDiagnosticInfo();
                }
            }
            catch (Exception ex)
            {
                _statusDisplay.Text = $"Error updating status: {ex.Message}";
            }
        }

        private void TestDndToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dndService != null)
                {
                    if (_dndService.IsDndEnabled())
                    {
                        _dndService.DisableDnd();
                    }
                    else
                    {
                        _dndService.EnableDnd();
                    }
                    UpdateStatus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing DND toggle: {ex.Message}");
            }
        }

        private void TestPositioning_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowSampleToast("Test Notification", $"This is a test notification sent at {DateTime.Now:HH:mm:ss}");

                if (_notificationPositioner != null)
                {
                    _notificationPositioner.PositionCurrentNotifications();
                    UpdateStatus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing positioning: {ex.Message}");
            }
        }

        private void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus();
        }
        private void ShowSampleToast(string title, string message)
        {
            // Create the toast content as XML string
            string toastXmlString =
       $@"<toast>
            <visual>
                <binding template='ToastGeneric'>
                    <text>{title}</text>
                    <text>{message}</text>
                </binding>
            </visual>
        </toast>";

            var notification = new AppNotification(toastXmlString);
            AppNotificationManager.Default.Show(notification);
        }
    }
}