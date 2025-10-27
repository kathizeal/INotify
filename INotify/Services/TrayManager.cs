using H.NotifyIcon;
using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using System.Windows.Input;
using System.Drawing;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;

namespace INotify.Services
{
    /// <summary>
    /// Manages system tray functionality for the application
    /// </summary>
    public class TrayManager : IDisposable
    {
        private TaskbarIcon? _trayIcon;
        private bool _disposed = false;

        public event EventHandler? ShowMainWindowRequested;
        public event EventHandler? ExitApplicationRequested;

        public void Initialize()
        {
            try
            {
                _trayIcon = new TaskbarIcon
                {
                    ToolTipText = "INotify - Notification Manager"
                };

                // Try to set an icon - multiple fallback approaches
                if (!TrySetIcon())
                {
                    Debug.WriteLine("Warning: Using TaskbarIcon without custom icon");
                }

                // Explicitly set the icon to be visible
                _trayIcon.ForceCreate(false);

                // Add context menu
                SetupContextMenu();

                // Add click handlers
                SetupClickHandlers();

                Debug.WriteLine("Tray icon initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing tray icon: {ex.Message}");
            }
        }

        private bool TrySetIcon()
        {
            try
            {
                // Method 1: Try using application icon
                if (TrySetApplicationIcon()) return true;

                // Method 2: Try creating a simple programmatic icon
                if (TrySetProgrammaticIcon()) return true;

                // Method 3: Use Windows system icon as fallback
                if (TrySetSystemIcon()) return true;

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting tray icon: {ex.Message}");
                return false;
            }
        }

        private bool TrySetApplicationIcon()
        {
            try
            {
                // Try to use the application's executable icon
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    using var icon = Icon.ExtractAssociatedIcon(processPath);
                    if (icon != null)
                    {
                        _trayIcon.Icon = icon;
                        Debug.WriteLine("Successfully set application icon");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to extract application icon: {ex.Message}");
            }
            return false;
        }

        private bool TrySetProgrammaticIcon()
        {
            try
            {
                // Create a simple 16x16 icon programmatically
                using var bitmap = new Bitmap(16, 16);
                using var graphics = Graphics.FromImage(bitmap);
                
                // Create a simple notification-style icon
                graphics.Clear(Color.Transparent);
                
                // Draw a simple notification bell shape
                using var brush = new SolidBrush(Color.DodgerBlue);
                using var pen = new Pen(Color.White, 1);
                
                // Bell body
                graphics.FillEllipse(brush, 2, 3, 12, 10);
                graphics.DrawEllipse(pen, 2, 3, 12, 10);
                
                // Bell bottom
                graphics.FillRectangle(brush, 6, 12, 4, 2);
                
                // Convert to icon
                IntPtr hIcon = bitmap.GetHicon();
                var icon = Icon.FromHandle(hIcon);
                
                _trayIcon.Icon = icon;
                Debug.WriteLine("Successfully created programmatic icon");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create programmatic icon: {ex.Message}");
            }
            return false;
        }

        private bool TrySetSystemIcon()
        {
            try
            {
                // Use Windows system application icon as fallback
                _trayIcon.Icon = SystemIcons.Application;
                Debug.WriteLine("Successfully set system icon");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set system icon: {ex.Message}");
            }
            return false;
        }

        private void SetupContextMenu()
        {
            try
            {
                // Create context menu for tray icon
                var contextMenu = new Microsoft.UI.Xaml.Controls.MenuFlyout();
                
                // Show window menu item
                var showItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
                {
                    Text = "Show INotify"
                };
                showItem.Click += (s, e) => ShowMainWindow();
                contextMenu.Items.Add(showItem);

                // Separator
                contextMenu.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());

                // Exit menu item
                var exitItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
                {
                    Text = "Exit"
                };
                exitItem.Click += (s, e) => ExitApplication();
                contextMenu.Items.Add(exitItem);

                _trayIcon.ContextFlyout = contextMenu;

                Debug.WriteLine("Tray context menu setup completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up context menu: {ex.Message}");
            }
        }

        private void SetupClickHandlers()
        {
            try
            {
                if (_trayIcon != null)
                {
                    // Handle left click to show window
                    _trayIcon.LeftClickCommand = new RelayCommand(() =>
                    {
                        Debug.WriteLine("Tray icon left clicked");
                        ShowMainWindow();
                    });

                    // Handle double click to show window
                    _trayIcon.DoubleClickCommand = new RelayCommand(() =>
                    {
                        Debug.WriteLine("Tray icon double clicked");
                        ShowMainWindow();
                    });

                    Debug.WriteLine("Tray click handlers setup completed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up click handlers: {ex.Message}");
            }
        }

        public void ShowMainWindow()
        {
            ShowMainWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ExitApplication()
        {
            ExitApplicationRequested?.Invoke(this, EventArgs.Empty);
        }

        public void ShowBalloonTip(string title, string message)
        {
            try
            {
                _trayIcon?.ShowNotification(title, message);
                Debug.WriteLine($"Balloon tip shown: {title} - {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing balloon tip: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _trayIcon?.Dispose();
                Debug.WriteLine("Tray manager disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing tray manager: {ex.Message}");
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Simple relay command implementation for tray menu items
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}