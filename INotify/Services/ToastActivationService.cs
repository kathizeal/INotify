using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;

namespace INotify.Services
{
    /// <summary>
    /// Handles activation events from INotify toast notifications
    /// </summary>
    public static class ToastActivationService
    {
        private static Window? _mainWindow;

        /// <summary>
        /// Sets the main window reference for activation handling
        /// </summary>
        public static void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        /// <summary>
        /// Handles toast notification activation
        /// </summary>
        public static void HandleToastActivation(ToastNotificationActivatedEventArgs args)
        {
            try
            {
                var arguments = ParseArguments(args.Argument);
                
                Debug.WriteLine($"Toast activation received with arguments: {args.Argument}");

                switch (arguments.GetValueOrDefault("action", ""))
                {
                    case "view":
                        HandleViewAction(arguments);
                        break;
                    case "dismiss":
                        HandleDismissAction(arguments);
                        break;
                    default:
                        Debug.WriteLine($"Unknown toast action: {arguments.GetValueOrDefault("action", "")}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling toast activation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the "View in INotify" action
        /// </summary>
        private static void HandleViewAction(Dictionary<string, string> arguments)
        {
            try
            {
                var notificationId = arguments.GetValueOrDefault("notificationId", "");
                var originalAppName = arguments.GetValueOrDefault("originalAppName", "");
                var originalPackage = arguments.GetValueOrDefault("originalPackage", "");
                var priority = arguments.GetValueOrDefault("priority", "");
                var spaces = arguments.GetValueOrDefault("spaces", "");

                Debug.WriteLine($"Handling view action for notification {notificationId} from {originalAppName}");

                // Bring the main window to foreground
                BringMainWindowToForeground();

                // TODO: Navigate to specific notification or filter view
                // This could navigate to:
                // - Specific priority view if priority is set
                // - Specific space view if spaces are set  
                // - All notifications view filtered by the app
                // - Directly to the notification if possible

                Debug.WriteLine($"Brought INotify to foreground for notification from {originalAppName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling view action: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the "Dismiss" action
        /// </summary>
        private static void HandleDismissAction(Dictionary<string, string> arguments)
        {
            try
            {
                var notificationId = arguments.GetValueOrDefault("notificationId", "");
                Debug.WriteLine($"Dismissing toast notification {notificationId}");
                
                // Could implement notification dismissal logic here
                // For example, mark as read in database, remove from UI, etc.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling dismiss action: {ex.Message}");
            }
        }

        /// <summary>
        /// Brings the main window to foreground
        /// </summary>
        private static void BringMainWindowToForeground()
        {
            try
            {
                if (_mainWindow != null)
                {
                    // Activate the window
                    _mainWindow.Activate();
                    
                    // Get the window handle and use Win32 APIs to restore
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
                    if (hwnd != IntPtr.Zero)
                    {
                        Win32APIs.ShowWindow(hwnd, Win32APIs.SW_RESTORE);
                        Win32APIs.SetForegroundWindow(hwnd);
                    }

                    Debug.WriteLine("Main window brought to foreground");
                }
                else
                {
                    Debug.WriteLine("Main window reference not available");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error bringing main window to foreground: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses toast activation arguments
        /// </summary>
        private static Dictionary<string, string> ParseArguments(string arguments)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(arguments))
                return result;

            try
            {
                var pairs = arguments.Split('&');
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(keyValue[0]);
                        var value = Uri.UnescapeDataString(keyValue[1]);
                        result[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing toast arguments: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Win32 APIs for window management
        /// </summary>
        private static class Win32APIs
        {
            public const int SW_RESTORE = 9;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }
    }
}