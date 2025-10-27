using H.NotifyIcon;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;

namespace INotify.Util
{
    /// <summary>
    /// Diagnostic utility to test and validate system tray functionality
    /// </summary>
    public static class TrayDiagnostics
    {
        /// <summary>
        /// Runs a comprehensive test of the system tray functionality
        /// </summary>
        public static async Task<bool> RunTrayDiagnosticsAsync()
        {
            Debug.WriteLine("=== Starting Tray Diagnostics ===");
            
            bool allTestsPassed = true;
            
            // Test 1: Basic TaskbarIcon Creation
            allTestsPassed &= await TestBasicTrayCreation();
            
            // Test 2: Icon Setting
            allTestsPassed &= await TestIconSetting();
            
            // Test 3: Tooltip and Visibility
            allTestsPassed &= await TestTooltipAndVisibility();
            
            // Test 4: Notification Display
            allTestsPassed &= await TestNotificationDisplay();
            
            // Test 5: System Environment
            allTestsPassed &= TestSystemEnvironment();
            
            Debug.WriteLine($"=== Tray Diagnostics Complete: {(allTestsPassed ? "PASSED" : "FAILED")} ===");
            
            return allTestsPassed;
        }

        private static async Task<bool> TestBasicTrayCreation()
        {
            Debug.WriteLine("Test 1: Basic TaskbarIcon Creation");
            
            try
            {
                using var testIcon = new TaskbarIcon();
                Debug.WriteLine("? TaskbarIcon created successfully");
                
                testIcon.ToolTipText = "Test Icon";
                Debug.WriteLine("? ToolTipText set successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Basic creation failed: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestIconSetting()
        {
            Debug.WriteLine("Test 2: Icon Setting");
            
            try
            {
                using var testIcon = new TaskbarIcon();
                
                // Test system icon
                testIcon.Icon = SystemIcons.Information;
                Debug.WriteLine("? System icon set successfully");
                
                // Test programmatic icon
                using var bitmap = new Bitmap(16, 16);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Blue);
                graphics.FillRectangle(Brushes.White, 4, 4, 8, 8);
                
                IntPtr hIcon = bitmap.GetHicon();
                var customIcon = Icon.FromHandle(hIcon);
                testIcon.Icon = customIcon;
                Debug.WriteLine("? Custom icon set successfully");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Icon setting failed: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestTooltipAndVisibility()
        {
            Debug.WriteLine("Test 3: Tooltip and Visibility");
            
            try
            {
                using var testIcon = new TaskbarIcon
                {
                    Icon = SystemIcons.Application,
                    ToolTipText = "INotify Diagnostic Test"
                };
                
                // Force create to make visible
                testIcon.ForceCreate(false);
                Debug.WriteLine("? Icon forced to visible state");
                
                // Wait briefly to allow Windows to process
                await Task.Delay(500);
                
                Debug.WriteLine("? Tooltip and visibility test completed");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Tooltip/visibility test failed: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestNotificationDisplay()
        {
            Debug.WriteLine("Test 4: Notification Display");
            
            try
            {
                using var testIcon = new TaskbarIcon
                {
                    Icon = SystemIcons.Information,
                    ToolTipText = "Test Notifications"
                };
                
                testIcon.ForceCreate(false);
                
                // Test notification
                testIcon.ShowNotification("Test Title", "Test notification message");
                Debug.WriteLine("? Notification sent successfully");
                
                // Wait for notification to display
                await Task.Delay(2000);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Notification test failed: {ex.Message}");
                return false;
            }
        }

        private static bool TestSystemEnvironment()
        {
            Debug.WriteLine("Test 5: System Environment");
            
            try
            {
                Debug.WriteLine($"? OS Version: {Environment.OSVersion}");
                Debug.WriteLine($"? .NET Version: {Environment.Version}");
                Debug.WriteLine($"? Process Path: {Environment.ProcessPath}");
                Debug.WriteLine($"? User: {Environment.UserName}");
                Debug.WriteLine($"? Machine: {Environment.MachineName}");
                Debug.WriteLine($"? Current Directory: {Environment.CurrentDirectory}");
                
                // Check for known problematic conditions
                if (Environment.OSVersion.Version.Major < 10)
                {
                    Debug.WriteLine("? Warning: Windows version may not fully support modern tray icons");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? System environment check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Quick test to verify if tray icons are working at all on this system
        /// </summary>
        public static async Task<bool> QuickTrayTestAsync()
        {
            Debug.WriteLine("=== Quick Tray Test ===");
            
            try
            {
                using var quickTest = new TaskbarIcon
                {
                    Icon = SystemIcons.Information,
                    ToolTipText = "Quick Test - If you see this tooltip, tray is working!"
                };
                
                quickTest.ForceCreate(false);
                quickTest.ShowNotification("Quick Test", "If you see this notification, tray notifications work!");
                
                Debug.WriteLine("Quick test deployed - check your system tray!");
                Debug.WriteLine("Look for an 'i' icon with tooltip: 'Quick Test - If you see this tooltip, tray is working!'");
                
                // Keep test icon visible for 10 seconds
                await Task.Delay(10000);
                
                Debug.WriteLine("Quick test completed");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Quick test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks Windows notification area settings programmatically
        /// </summary>
        public static void CheckNotificationAreaSettings()
        {
            Debug.WriteLine("=== Notification Area Settings Check ===");
            
            try
            {
                // Check registry settings for notification area
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer");
                if (key != null)
                {
                    var noTrayItems = key.GetValue("NoTrayItemsDisplay");
                    if (noTrayItems != null && noTrayItems.ToString() == "1")
                    {
                        Debug.WriteLine("? ERROR: Tray items are disabled by group policy");
                    }
                    else
                    {
                        Debug.WriteLine("? No group policy blocking tray items");
                    }
                }
                else
                {
                    Debug.WriteLine("? No restrictive policies found");
                }

                // Additional system checks could be added here
                Debug.WriteLine("? Notification area settings check completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Could not check notification area settings: {ex.Message}");
            }
        }
    }
}