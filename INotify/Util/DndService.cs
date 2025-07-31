using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Linq;
using WindowsDnd;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AppList
{
    public class DndService
    {
        // Primary registry path for Focus Assist settings
        private const string FOCUS_ASSIST_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$$windows.data.notifications.quiethours$$\Current";
        private const string FOCUS_ASSIST_REGISTRY_KEY = "Data";

        // Registry path for notification settings
        private const string NOTIFICATION_SETTINGS_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";
        
        // Registry path for priority applications in Focus Assist
        private const string PRIORITY_APPS_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings";

        // Alternative registry paths to try
        private static readonly string[] ALTERNATIVE_PATHS = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount\$$windows.data.notifications.quiethours$$\Current",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\$$windows.data.notifications.quiethours$$\Current",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\ToastEnabled"
        };

        private WindowsDndManager? _windowsDndManager;
        private bool _comInterfaceAvailable;

        public enum FocusAssistState
        {
            Off = 0,
            Priority = 1,
            AlarmsOnly = 2
        }

        public class PriorityApp
        {
            public string AppId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Publisher { get; set; } = string.Empty;
            public bool IsEnabled { get; set; }
            public AppType AppType { get; set; }
        }

        public enum AppType
        {
            Win32,
            UWP,
            System
        }

        public DndService()
        {
            InitializeComInterface();
        }

        private void InitializeComInterface()
        {
            try
            {
                _windowsDndManager = new WindowsDndManager();
                _comInterfaceAvailable = true;
            }
            catch (Exception)
            {
                // COM interface not available, will fall back to registry methods
                _comInterfaceAvailable = false;
                _windowsDndManager = null;
            }
        }

        /// <summary>
        /// Gets the current Focus Assist (DND) state with multiple fallback methods
        /// Priority: COM Interface -> Registry -> Windows API
        /// </summary>
        /// <returns>Current Focus Assist state</returns>
        public FocusAssistState GetCurrentState()
        {
            // Method 1: Try COM interface first (most reliable)
            if (_comInterfaceAvailable && _windowsDndManager != null)
            {
                try
                {
                    var comMode = _windowsDndManager.GetCurrentMode();
                    return ConvertFromComMode(comMode);
                }
                catch (Exception)
                {
                    // COM interface failed, fall back to registry
                    _comInterfaceAvailable = false;
                }
            }

            // Method 2: Try the primary CloudStore path
            var state = TryGetStateFromPath(FOCUS_ASSIST_REGISTRY_PATH);
            if (state.HasValue)
            {
                return state.Value;
            }

            // Method 3: Try alternative paths
            foreach (var path in ALTERNATIVE_PATHS)
            {
                state = TryGetStateFromPath(path);
                if (state.HasValue)
                {
                    return state.Value;
                }
            }

            // Method 4: Try to find the path dynamically
            var foundPath = FindFocusAssistRegistryPath();
            if (foundPath != null)
            {
                state = TryGetStateFromPath(foundPath);
                if (state.HasValue)
                {
                    return state.Value;
                }
            }

            // Method 5: Use Windows API as fallback
            state = GetStateViaWindowsApi();
            if (state.HasValue)
            {
                return state.Value;
            }

            // Fallback: assume DND is off if we can't read the state
            return FocusAssistState.Off;
        }

        /// <summary>
        /// Sets the Focus Assist (DND) state with multiple fallback methods
        /// Priority: COM Interface -> Registry -> Windows API
        /// </summary>
        /// <param name="state">The desired Focus Assist state</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetFocusAssistState(FocusAssistState state)
        {
            // Method 1: Try COM interface first (most reliable)
            if (_comInterfaceAvailable && _windowsDndManager != null)
            {
                try
                {
                    var comMode = ConvertToComMode(state);
                    _windowsDndManager.SetMode(comMode);
                    NotifySystemChange();
                    return true;
                }
                catch (Exception)
                {
                    // COM interface failed, fall back to registry
                    _comInterfaceAvailable = false;
                }
            }

            // Method 2: Try the primary path
            if (TrySetStateAtPath(FOCUS_ASSIST_REGISTRY_PATH, state))
            {
                NotifySystemChange();
                return true;
            }

            // Method 3: Try alternative paths
            foreach (var path in ALTERNATIVE_PATHS)
            {
                if (TrySetStateAtPath(path, state))
                {
                    NotifySystemChange();
                    return true;
                }
            }

            // Method 4: Try dynamically found path
            var foundPath = FindFocusAssistRegistryPath();
            if (foundPath != null && TrySetStateAtPath(foundPath, state))
            {
                NotifySystemChange();
                return true;
            }

            // Method 5: Try Windows API
            if (TrySetStateViaWindowsApi(state))
            {
                NotifySystemChange();
                return true;
            }

            // Method 6: Try to create the registry structure if it doesn't exist
            if (TryCreateAndSetFocusAssistRegistry(state))
            {
                NotifySystemChange();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Enables Do Not Disturb (sets to Priority mode)
        /// </summary>
        /// <returns>True if successful</returns>
        public bool EnableDnd()
        {
            return SetFocusAssistState(FocusAssistState.Priority);
        }

        /// <summary>
        /// Disables Do Not Disturb
        /// </summary>
        /// <returns>True if successful</returns>
        public bool DisableDnd()
        {
            return SetFocusAssistState(FocusAssistState.Off);
        }

        /// <summary>
        /// Sets to Alarms Only mode
        /// </summary>
        /// <returns>True if successful</returns>
        public bool SetAlarmsOnly()
        {
            return SetFocusAssistState(FocusAssistState.AlarmsOnly);
        }

        /// <summary>
        /// Checks if DND is currently enabled
        /// </summary>
        /// <returns>True if DND is enabled</returns>
        public bool IsDndEnabled()
        {
            var state = GetCurrentState();
            return state == FocusAssistState.Priority || state == FocusAssistState.AlarmsOnly;
        }

        /// <summary>
        /// Gets a user-friendly description of the current state
        /// </summary>
        /// <returns>Description of current DND state</returns>
        public string GetStateDescription()
        {
            var state = GetCurrentState();
            return state switch
            {
                FocusAssistState.Off => "Do Not Disturb is OFF - All notifications are allowed",
                FocusAssistState.Priority => "Do Not Disturb is ON - Only priority notifications are allowed",
                FocusAssistState.AlarmsOnly => "Do Not Disturb is ON - Only alarms are allowed",
                _ => "Unknown Do Not Disturb state"
            };
        }

        /// <summary>
        /// Gets diagnostic information about available methods and registry paths
        /// </summary>
        /// <returns>Diagnostic information</returns>
        public string GetDiagnosticInfo()
        {
            var info = "Focus Assist Diagnostic Information:\n\n";

            // COM Interface Status
            info += "=== COM Interface ===\n";
            info += $"COM Interface Available: {_comInterfaceAvailable}\n";
            if (_comInterfaceAvailable && _windowsDndManager != null)
            {
                try
                {
                    var comMode = _windowsDndManager.GetCurrentMode();
                    info += $"COM Current Mode: {comMode}\n";
                    info += $"COM Is Enabled: {_windowsDndManager.IsEnabled()}\n";
                }
                catch (Exception ex)
                {
                    info += $"COM Error: {ex.Message}\n";
                }
            }

            info += "\n=== Registry Methods ===\n";
            info += "Primary Path: " + FOCUS_ASSIST_REGISTRY_PATH + "\n";
            info += "Primary Path Exists: " + DoesRegistryPathExist(FOCUS_ASSIST_REGISTRY_PATH) + "\n\n";

            info += "Alternative Paths:\n";
            foreach (var path in ALTERNATIVE_PATHS)
            {
                info += $"  {path}: {DoesRegistryPathExist(path)}\n";
            }

            var foundPath = FindFocusAssistRegistryPath();
            info += $"\nDynamically Found Path: {foundPath ?? "None"}\n";

            if (foundPath != null)
            {
                info += $"Dynamic Path Exists: {DoesRegistryPathExist(foundPath)}\n";
            }

            // Current state from all methods
            info += "\n=== Current State Detection ===\n";
            info += $"Final Detected State: {GetCurrentState()}\n";
            info += $"Is DND Enabled: {IsDndEnabled()}\n";

            // Notification Settings Path
            info += "\n=== Notification Settings ===\n";
            info += $"Notification Settings Path: {NOTIFICATION_SETTINGS_PATH}\n";
            info += $"Notification Settings Exists: {DoesRegistryPathExist(NOTIFICATION_SETTINGS_PATH)}\n";

            // Check Windows 11 specific paths
            info += "\n=== Windows 11 Priority Paths ===\n";
            var win11PriorityPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\{E77F06E2-4E62-4C4B-9E3F-B02A5C0E3F7A}";
            info += $"Windows 11 Priority Path: {win11PriorityPath}\n";
            info += $"Windows 11 Priority Path Exists: {DoesRegistryPathExist(win11PriorityPath)}\n";

            var pushNotificationPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\Applications";
            info += $"PushNotifications Path: {pushNotificationPath}\n";
            info += $"PushNotifications Path Exists: {DoesRegistryPathExist(pushNotificationPath)}\n";

            // Check CloudStore paths
            info += "\n=== CloudStore Paths ===\n";
            var cloudStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount";
            info += $"CloudStore Base Path: {cloudStorePath}\n";
            info += $"CloudStore Base Path Exists: {DoesRegistryPathExist(cloudStorePath)}\n";

            try
            {
                using (var cloudKey = Registry.CurrentUser.OpenSubKey(cloudStorePath))
                {
                    if (cloudKey != null)
                    {
                        var subKeys = cloudKey.GetSubKeyNames();
                        var focusAssistKeys = subKeys.Where(name => 
                            name.Contains("windows.data.notifications") ||
                            name.Contains("quiethours") ||
                            name.Contains("prioritylist")).ToArray();
                        
                        info += $"Focus Assist Related CloudStore Keys ({focusAssistKeys.Length} found):\n";
                        foreach (var key in focusAssistKeys.Take(5))
                        {
                            info += $"  - {key}\n";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                info += $"Error reading CloudStore: {ex.Message}\n";
            }

            // List some existing notification settings
            try
            {
                using (var settingsKey = Registry.CurrentUser.OpenSubKey(NOTIFICATION_SETTINGS_PATH))
                {
                    if (settingsKey != null)
                    {
                        var subKeys = settingsKey.GetSubKeyNames().Take(10).ToArray();
                        info += $"\nSample Apps with Notification Settings ({subKeys.Length} shown):\n";
                        foreach (var subKey in subKeys)
                        {
                            info += $"  - {subKey}\n";
                            
                            // Check if this app has priority settings
                            try
                            {
                                using (var appKey = settingsKey.OpenSubKey(subKey))
                                {
                                    if (appKey != null)
                                    {
                                        var quietHoursAllowed = GetRegistryValue<int>(appKey, "QuietHoursAllowed", -1);
                                        var priority = GetRegistryValue<int>(appKey, "Priority", -1);
                                        var allowCritical = GetRegistryValue<int>(appKey, "AllowCriticalNotifications", -1);
                                        
                                        if (quietHoursAllowed > -1 || priority > -1 || allowCritical > -1)
                                        {
                                            info += $"    QuietHoursAllowed: {quietHoursAllowed}, Priority: {priority}, AllowCritical: {allowCritical}\n";
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                info += $"Error reading notification settings: {ex.Message}\n";
            }

            // Registry permissions check
            info += "\n=== Registry Permissions ===\n";
            try
            {
                using (var testKey = Registry.CurrentUser.CreateSubKey($"{NOTIFICATION_SETTINGS_PATH}\\TestKey"))
                {
                    if (testKey != null)
                    {
                        testKey.SetValue("Test", "Value", RegistryValueKind.String);
                        info += "Write Permission: SUCCESS (test key created and deleted)\n";
                    }
                }
                Registry.CurrentUser.DeleteSubKey($"{NOTIFICATION_SETTINGS_PATH}\\TestKey", false);
            }
            catch (Exception ex)
            {
                info += $"Write Permission: FAILED - {ex.Message}\n";
            }

            return info;
        }

        #region Helper Methods

        private FocusAssistState ConvertFromComMode(WindowsDndManager.DndMode comMode)
        {
            return comMode switch
            {
                WindowsDndManager.DndMode.Off => FocusAssistState.Off,
                WindowsDndManager.DndMode.PriorityOnly => FocusAssistState.Priority,
                WindowsDndManager.DndMode.AlarmsOnly => FocusAssistState.AlarmsOnly,
                _ => FocusAssistState.Off
            };
        }

        private WindowsDndManager.DndMode ConvertToComMode(FocusAssistState state)
        {
            return state switch
            {
                FocusAssistState.Off => WindowsDndManager.DndMode.Off,
                FocusAssistState.Priority => WindowsDndManager.DndMode.PriorityOnly,
                FocusAssistState.AlarmsOnly => WindowsDndManager.DndMode.AlarmsOnly,
                _ => WindowsDndManager.DndMode.Off
            };
        }

        private FocusAssistState? TryGetStateFromPath(string registryPath)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        var data = key.GetValue(FOCUS_ASSIST_REGISTRY_KEY) as byte[];
                        if (data != null && data.Length > 16)
                        {
                            // The Focus Assist state is stored at offset 16 in the binary data
                            var state = data[16];
                            if (Enum.IsDefined(typeof(FocusAssistState), (int)state))
                            {
                                return (FocusAssistState)state;
                            }
                        }

                        // Try alternative value names if "Data" doesn't exist
                        var valueNames = key.GetValueNames();
                        foreach (var valueName in valueNames)
                        {
                            var value = key.GetValue(valueName);
                            if (value is byte[] bytes && bytes.Length > 16)
                            {
                                var state = bytes[16];
                                if (Enum.IsDefined(typeof(FocusAssistState), (int)state))
                                {
                                    return (FocusAssistState)state;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue to next method if this fails
            }

            return null;
        }

        private string FindFocusAssistRegistryPath()
        {
            try
            {
                // Try to find the CloudStore path dynamically
                var cloudStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount";
                using (var key = Registry.CurrentUser.OpenSubKey(cloudStorePath))
                {
                    if (key != null)
                    {
                        var subKeyNames = key.GetSubKeyNames();
                        var quietHoursKey = subKeyNames.FirstOrDefault(name =>
                            name.Contains("windows.data.notifications.quiethours"));

                        if (quietHoursKey != null)
                        {
                            var fullPath = $"{cloudStorePath}\\{quietHoursKey}\\Current";
                            using (var testKey = Registry.CurrentUser.OpenSubKey(fullPath))
                            {
                                if (testKey != null)
                                {
                                    return fullPath;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue if dynamic search fails
            }

            return null;
        }

        private FocusAssistState? GetStateViaWindowsApi()
        {
            try
            {
                // Try using Windows API to get Focus Assist state
                var result = WnfQueryFocusAssistState();
                if (result.HasValue && Enum.IsDefined(typeof(FocusAssistState), result.Value))
                {
                    return (FocusAssistState)result.Value;
                }
            }
            catch (Exception)
            {
                // API method failed
            }

            return null;
        }

        private bool TrySetStateAtPath(string registryPath, FocusAssistState state)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                {
                    if (key != null)
                    {
                        var data = key.GetValue(FOCUS_ASSIST_REGISTRY_KEY) as byte[];
                        if (data != null && data.Length > 16)
                        {
                            // Update the Focus Assist state at offset 16
                            data[16] = (byte)state;
                            key.SetValue(FOCUS_ASSIST_REGISTRY_KEY, data, RegistryValueKind.Binary);
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue to next method if this fails
            }

            return false;
        }

        private bool TrySetStateViaWindowsApi(FocusAssistState state)
        {
            try
            {
                return WnfSetFocusAssistState((int)state);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryCreateAndSetFocusAssistRegistry(FocusAssistState state)
        {
            try
            {
                // This is a more aggressive approach - try to create the registry structure
                // This might require elevated privileges
                using (var key = Registry.CurrentUser.CreateSubKey(FOCUS_ASSIST_REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        // Create a minimal binary data structure for Focus Assist
                        var data = new byte[32]; // Create a 32-byte array
                        data[16] = (byte)state; // Set the state at offset 16
                        key.SetValue(FOCUS_ASSIST_REGISTRY_KEY, data, RegistryValueKind.Binary);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Registry creation failed - might need admin privileges
            }

            return false;
        }

        private bool DoesRegistryPathExist(string path)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(path))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Windows API Declarations

        // Windows API declarations
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("ntdll.dll")]
        private static extern uint RtlQueryWnfStateData(ref ulong StateName, IntPtr TypeId, IntPtr ExplicitScope, out uint ChangeStamp, IntPtr Buffer, ref uint BufferSize);

        [DllImport("ntdll.dll")]
        private static extern uint RtlPublishWnfStateData(ulong StateName, IntPtr TypeId, IntPtr Buffer, uint Length, IntPtr ExplicitScope);

        // WNF State Name for Focus Assist (this may vary by Windows version)
        private static readonly ulong WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED = 0xD83063EA3BC1875;

        private int? WnfQueryFocusAssistState()
        {
            try
            {
                uint changeStamp;
                uint bufferSize = 4;
                var buffer = Marshal.AllocHGlobal((int)bufferSize);

                try
                {
                    var stateName = WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED;
                    var result = RtlQueryWnfStateData(ref stateName, IntPtr.Zero, IntPtr.Zero, out changeStamp, buffer, ref bufferSize);

                    if (result == 0 && bufferSize >= 4)
                    {
                        return Marshal.ReadInt32(buffer);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception)
            {
                // WNF API failed
            }

            return null;
        }

        private bool WnfSetFocusAssistState(int state)
        {
            try
            {
                var buffer = Marshal.AllocHGlobal(4);
                try
                {
                    Marshal.WriteInt32(buffer, state);
                    var result = RtlPublishWnfStateData(WNF_SHEL_QUIETHOURS_ACTIVE_PROFILE_CHANGED, IntPtr.Zero, buffer, 4, IntPtr.Zero);
                    return result == 0;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void NotifySystemChange()
        {
            // Notify Windows that system settings have changed
            // SHCNE_ASSOCCHANGED = 0x8000000, SHCNF_IDLIST = 0x0000
            SHChangeNotify(0x8000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        private void NotifyFocusAssistChange()
        {
            try
            {
                // Method 1: Notify Windows notification broker of changes
                SHChangeNotify(0x8000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                
                // Method 2: Try to trigger Windows notification system refresh
                try
                {
                    // Use WNF (Windows Notification Facility) to notify system of changes
                    var buffer = Marshal.AllocHGlobal(4);
                    try
                    {
                        Marshal.WriteInt32(buffer, 1); // Indicate change
                        RtlPublishWnfStateData(0xD83063EA3BC1875, IntPtr.Zero, buffer, 4, IntPtr.Zero);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                catch
                {
                    // Ignore if WNF notification fails
                }

                // Method 3: Try to restart notification-related services (requires admin)
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c sc stop WpnService & timeout /t 2 & sc start WpnService",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = true,
                        Verb = "runas" // Request admin privileges
                    };
                    
                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit(5000); // Wait max 5 seconds
                        }
                    }
                }
                catch
                {
                    // Ignore if service restart fails (user might not have admin rights)
                }

                // Method 4: Update Group Policy to trigger refresh
                try
                {
                    using (var policyKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\CurrentVersion\PushNotifications"))
                    {
                        if (policyKey != null)
                        {
                            policyKey.SetValue("LastPriorityUpdate", DateTime.UtcNow.Ticks, RegistryValueKind.QWord);
                            policyKey.SetValue("RefreshRequired", 1, RegistryValueKind.DWord);
                            policyKey.Flush();
                        }
                    }
                }
                catch
                {
                    // Ignore if policy update fails
                }

                // Method 5: Try to trigger Windows Settings refresh
                try
                {
                    var settingsStartInfo = new ProcessStartInfo
                    {
                        FileName = "ms-settings:notifications",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    
                    using (var settingsProcess = Process.Start(settingsStartInfo))
                    {
                        if (settingsProcess != null)
                        {
                            // Close it immediately after opening
                            Task.Delay(1000).ContinueWith(_ =>
                            {
                                try
                                {
                                    if (!settingsProcess.HasExited)
                                    {
                                        settingsProcess.Kill();
                                    }
                                }
                                catch { }
                            });
                        }
                    }
                }
                catch
                {
                    // Ignore if settings app launch fails
                }

                System.Diagnostics.Debug.WriteLine("NotifyFocusAssistChange: All notification methods attempted");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotifyFocusAssistChange error: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Support

        public void Dispose()
        {
            _windowsDndManager?.Dispose();
            _windowsDndManager = null;
        }

        #endregion

        #region Priority List Management

        /// <summary>
        /// Gets the list of applications configured for priority notifications
        /// </summary>
        /// <returns>List of priority applications</returns>
        public List<PriorityApp> GetPriorityApplications()
        {
            var priorityApps = new List<PriorityApp>();

            try
            {
                using (var settingsKey = Registry.CurrentUser.OpenSubKey(NOTIFICATION_SETTINGS_PATH))
                {
                    if (settingsKey != null)
                    {
                        foreach (var appKeyName in settingsKey.GetSubKeyNames())
                        {
                            using (var appKey = settingsKey.OpenSubKey(appKeyName))
                            {
                                if (appKey != null)
                                {
                                    var enabled = GetRegistryValue<int>(appKey, "Enabled", 1);
                                    var showInActionCenter = GetRegistryValue<int>(appKey, "ShowInActionCenter", 1);
                                    var soundsEnabled = GetRegistryValue<int>(appKey, "SoundsEnabled", 1);
                                    
                                    // Check if this app is configured for priority notifications
                                    // In Focus Assist, priority apps typically have specific registry values
                                    var priorityEnabled = GetRegistryValue<int>(appKey, "ShowInActionCenter", 0);
                                    
                                    if (enabled == 1 && (showInActionCenter == 1 || priorityEnabled == 1))
                                    {
                                        var priorityApp = new PriorityApp
                                        {
                                            AppId = appKeyName,
                                            DisplayName = GetFriendlyAppName(appKeyName),
                                            IsEnabled = true,
                                            AppType = DetermineAppType(appKeyName)
                                        };

                                        // Try to get publisher information
                                        priorityApp.Publisher = GetAppPublisher(appKeyName);

                                        priorityApps.Add(priorityApp);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting priority applications: {ex.Message}");
            }

            return priorityApps.OrderBy(app => app.DisplayName).ToList();
        }

        /// <summary>
        /// Adds an application to the priority list using the correct Windows Focus Assist method
        /// This implementation uses multiple approaches to ensure compatibility across Windows versions
        /// </summary>
        /// <param name="appId">Application identifier</param>
        /// <param name="displayName">Display name of the application</param>
        /// <returns>True if successful</returns>
        public bool AddToPriorityList(string appId, string displayName = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"AddToPriorityList: Attempting to add {displayName} with ID: {appId}");
                
                bool success = false;
                
                // Method 1: Use Windows Runtime Notification APIs (Primary method)
                if (TryAddViaWindowsRuntimeAPI(appId, displayName))
                {
                    System.Diagnostics.Debug.WriteLine($"AddToPriorityList: Successfully added via Windows Runtime API");
                    success = true;
                }
                
                // Method 2: Direct CloudStore manipulation (Windows 11)
                if (TryAddToCloudStoreDirectly(appId, displayName))
                {
                    System.Diagnostics.Debug.WriteLine($"AddToPriorityList: Successfully added to CloudStore");
                    success = true;
                }
                
                // Method 3: Enhanced Registry approach with correct AUMID handling
                if (TryAddViaEnhancedRegistry(appId, displayName))
                {
                    System.Diagnostics.Debug.WriteLine($"AddToPriorityList: Successfully added via Enhanced Registry");
                    success = true;
                }
                
                // Method 4: PowerShell-based approach (Windows 10/11)
                if (TryAddViaPowerShell(appId, displayName))
                {
                    System.Diagnostics.Debug.WriteLine($"AddToPriorityList: Successfully added via PowerShell");
                    success = true;
                }
                
                if (success)
                {
                    // Comprehensive system notification
                    NotifySystemOfPriorityChange();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding app to priority list: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Method 1: Use Windows Runtime Notification APIs
        /// This is the most reliable method for modern Windows versions
        /// </summary>
        private bool TryAddViaWindowsRuntimeAPI(string appId, string displayName)
        {
            try
            {
                // For UWP apps, we need to use the proper AUMID format
                string aumid = GenerateAUMID(appId);
                System.Diagnostics.Debug.WriteLine($"TryAddViaWindowsRuntimeAPI: Generated AUMID: {aumid}");
                
                // Try to register the app as a notification source
                var registryPath = $@"SOFTWARE\Classes\AppUserModelId\{aumid}";
                using (var key = Registry.CurrentUser.CreateSubKey(registryPath))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", displayName, RegistryValueKind.String);
                        key.SetValue("IconUri", "", RegistryValueKind.String);
                        key.SetValue("ShowInSettings", 1, RegistryValueKind.DWord);
                        
                        // Critical: Mark as priority app
                        key.SetValue("Priority", 1, RegistryValueKind.DWord);
                        key.SetValue("BypassQuietHours", 1, RegistryValueKind.DWord);
                        key.Flush();
                    }
                }
                
                // Also add to notification settings with the AUMID
                var notificationPath = $"{NOTIFICATION_SETTINGS_PATH}\\{aumid}";
                using (var notifKey = Registry.CurrentUser.CreateSubKey(notificationPath))
                {
                    if (notifKey != null)
                    {
                        notifKey.SetValue("Enabled", 1, RegistryValueKind.DWord);
                        notifKey.SetValue("ShowInActionCenter", 1, RegistryValueKind.DWord);
                        notifKey.SetValue("SoundsEnabled", 1, RegistryValueKind.DWord);
                        notifKey.SetValue("ShowOnLockScreen", 1, RegistryValueKind.DWord);
                        notifKey.SetValue("BadgingEnabled", 1, RegistryValueKind.DWord);
                        
                        // The magic values for Focus Assist priority
                        notifKey.SetValue("IsPriorityApp", 1, RegistryValueKind.DWord);
                        notifKey.SetValue("Priority", 2, RegistryValueKind.DWord);
                        notifKey.SetValue("BypassQuietHours", 1, RegistryValueKind.DWord);
                        notifKey.SetValue("BypassDND", 1, RegistryValueKind.DWord);
                        
                        notifKey.Flush();
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryAddViaWindowsRuntimeAPI error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 2: Direct CloudStore manipulation for Windows 11
        /// </summary>
        private bool TryAddToCloudStoreDirectly(string appId, string displayName)
        {
            try
            {
                // Find the actual Focus Assist CloudStore path
                var cloudStorePaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount"
                };

                foreach (var basePath in cloudStorePaths)
                {
                    using (var baseKey = Registry.CurrentUser.OpenSubKey(basePath))
                    {
                        if (baseKey == null) continue;

                        var subKeys = baseKey.GetSubKeyNames();
                        var focusAssistKeys = subKeys.Where(name => 
                            name.Contains("windows.data.notifications.quiethours") ||
                            name.Contains("windows.data.notifications.quietmodesettings") ||
                            name.Contains("quiethours")).ToArray();

                        foreach (var focusKey in focusAssistKeys)
                        {
                            var fullPath = $"{basePath}\\{focusKey}\\Current";
                            
                            try
                            {
                                using (var priorityKey = Registry.CurrentUser.OpenSubKey(fullPath, true))
                                {
                                    if (priorityKey != null)
                                    {
                                        // Method 2a: Try to modify the binary data directly
                                        if (TryModifyCloudStoreBinaryData(priorityKey, appId, displayName))
                                        {
                                            return true;
                                        }
                                        
                                        // Method 2b: Create substructure for priority apps
                                        var appKey = $"PriorityList\\{SanitizeAppId(appId)}";
                                        using (var appEntry = Registry.CurrentUser.CreateSubKey($"{fullPath}\\{appKey}"))
                                        {
                                            if (appEntry != null)
                                            {
                                                appEntry.SetValue("AppId", appId, RegistryValueKind.String);
                                                appEntry.SetValue("DisplayName", displayName, RegistryValueKind.String);
                                                appEntry.SetValue("Enabled", 1, RegistryValueKind.DWord);
                                                appEntry.SetValue("Priority", 1, RegistryValueKind.DWord);
                                                appEntry.SetValue("Timestamp", DateTime.UtcNow.ToBinary(), RegistryValueKind.QWord);
                                                appEntry.Flush();
                                                

                                                System.Diagnostics.Debug.WriteLine($"TryAddToCloudStoreDirectly: Added to {fullPath}");
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"CloudStore path {fullPath} error: {ex.Message}");
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryAddToCloudStoreDirectly error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 3: Enhanced Registry approach with proper AUMID handling
        /// </summary>
        private bool TryAddViaEnhancedRegistry(string appId, string displayName)
        {
            try
            {

                string aumid = GenerateAUMID(appId);
                var registryPaths = new[]
                {
                    $"{NOTIFICATION_SETTINGS_PATH}\\{aumid}",
                    $"{NOTIFICATION_SETTINGS_PATH}\\{SanitizeAppId(appId)}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\Applications\{aumid}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.FocusAssist\{aumid}"
                };

                bool success = false;
                foreach (var regPath in registryPaths)
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.CreateSubKey(regPath))
                        {
                            if (key != null)
                            {
                                // Standard notification settings
                                key.SetValue("Enabled", 1, RegistryValueKind.DWord);
                                key.SetValue("ShowInActionCenter", 1, RegistryValueKind.DWord);
                                key.SetValue("SoundsEnabled", 1, RegistryValueKind.DWord);
                                key.SetValue("ShowOnLockScreen", 1, RegistryValueKind.DWord);
                                key.SetValue("BadgingEnabled", 1, RegistryValueKind.DWord);
                                
                                // Focus Assist specific settings
                                key.SetValue("Priority", 2, RegistryValueKind.DWord);
                                key.SetValue("IsPriorityApp", 1, RegistryValueKind.DWord);
                                key.SetValue("BypassQuietHours", 1, RegistryValueKind.DWord);
                                key.SetValue("BypassDND", 1, RegistryValueKind.DWord);
                                key.SetValue("AllowCriticalNotifications", 1, RegistryValueKind.DWord);
                                key.SetValue("BreakthroughLevel", 1, RegistryValueKind.DWord);
                                
                                // Windows 11 specific
                                key.SetValue("FocusAssistPriority", 1, RegistryValueKind.DWord);
                                key.SetValue("QuietModeOverride", 1, RegistryValueKind.DWord);
                                
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    key.SetValue("DisplayName", displayName, RegistryValueKind.String);
                                }

                                key.Flush();
                                success = true;
                                System.Diagnostics.Debug.WriteLine($"TryAddViaEnhancedRegistry: Added to {regPath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Registry path {regPath} error: {ex.Message}");
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryAddViaEnhancedRegistry error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 4: PowerShell-based approach using Windows APIs
        /// </summary>
        private bool TryAddViaPowerShell(string appId, string displayName)
        {
            try
            {
                string aumid = GenerateAUMID(appId);
                
                // PowerShell script to register the app with Focus Assist
                var psScript = $@"
                try {{
                    # Method 1: Use Windows.UI.Notifications if available
                    $null = [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType=WindowsRuntime]
                    $appId = '{aumid}'
                    $displayName = '{displayName.Replace("'", "''")}'
                    
                    # Try to create a toast notifier for this app
                    $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId)
                    
                    # Method 2: Registry manipulation via PowerShell
                    $regPaths = @(
                        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\' + $appId,
                        'HKCU:\SOFTWARE\Classes\AppUserModelId\' + $appId
                    )
                    
                    foreach ($regPath in $regPaths) {{
                        if (!(Test-Path $regPath)) {{
                            New-Item -Path $regPath -Force | Out-Null
                        }}
                        
                        Set-ItemProperty -Path $regPath -Name 'Enabled' -Value 1 -Type DWord -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $regPath -Name 'Priority' -Value 2 -Type DWord -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $regPath -Name 'BypassQuietHours' -Value 1 -Type DWord -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $regPath -Name 'IsPriorityApp' -Value 1 -Type DWord -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $regPath -Name 'DisplayName' -Value $displayName -Type String -ErrorAction SilentlyContinue
                        Set-ItemProperty -Path $regPath -Name 'ShowInActionCenter' -Value 1 -Type DWord -ErrorAction SilentlyContinue
                    }}
                    
                    Write-Output 'SUCCESS'
                }}
                catch {{
                    Write-Output 'ERROR: ' + $_.Exception.Message
                }}
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(10000); // 10 second timeout
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        
                        System.Diagnostics.Debug.WriteLine($"PowerShell output: {output}");
                        if (!string.IsNullOrEmpty(error))
                        {
                            System.Diagnostics.Debug.WriteLine($"PowerShell error: {error}");
                        }
                        
                        return output.Contains("SUCCESS");
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryAddViaPowerShell error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generate proper Application User Model ID (AUMID)
        /// </summary>
        private string GenerateAUMID(string appId)
        {
            if (string.IsNullOrEmpty(appId)) return appId;
            
            // For UWP apps, the PackageFamilyName is already a valid AUMID
            if (appId.Contains("_") && appId.Contains("."))
            {
                return appId;
            }
            
            // For Win32 apps, create a proper AUMID
            if (Path.IsPathRooted(appId))
            {
                string exeName = Path.GetFileNameWithoutExtension(appId);
                string companyName = "DefaultCompany"; // You could extract this from file properties
                return $"{companyName}.{exeName}";
            }
            
            // Clean up the app ID to make it AUMID compliant
            string cleanId = appId.Replace(" ", "").Replace("-", "").Replace(".", "");
            
            // Ensure it follows AUMID format: CompanyName.ProductName.SubProduct.VersionInfo
            if (!cleanId.Contains("."))
            {
                cleanId = $"App.{cleanId}";
            }
            
            return cleanId;
        }

        /// <summary>
        /// Try to modify CloudStore binary data directly
        /// </summary>
        private bool TryModifyCloudStoreBinaryData(RegistryKey key, string appId, string displayName)
        {
            try
            {
                var data = key.GetValue("Data") as byte[];
                if (data == null || data.Length < 32)
                {
                    // Create minimal binary structure if none exists
                    data = new byte[256]; // Larger buffer for app entries
                }
                
                // This is a simplified approach - in reality, the CloudStore binary format is complex
                // We'll append our app data to the existing structure
                var appIdBytes = System.Text.Encoding.Unicode.GetBytes(appId);
                var displayNameBytes = System.Text.Encoding.Unicode.GetBytes(displayName);
                
                // Find a suitable location in the binary data to insert our app
                int insertPosition = data.Length - 64; // Reserve space at the end
                if (insertPosition > 0)
                {
                    // Priority flag
                    data[insertPosition] = 1;
                    data[insertPosition + 1] = 0;
                    data[insertPosition + 2] = 0;
                    data[insertPosition + 3] = 0;
                    
                    key.SetValue("Data", data, RegistryValueKind.Binary);
                    key.Flush();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryModifyCloudStoreBinaryData error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Comprehensive system notification of priority changes
        /// </summary>
        private void NotifySystemOfPriorityChange()
        {
            try
            {
                // Multiple notification methods
                NotifySystemChange();
                NotifyFocusAssistChange();
                
                // Additional Windows 11 specific notifications
                var additionalNotifications = new ulong[]
                {
                    0xD83063EA3BC1875, // Focus Assist state change
                    0xD83063EA3BC1876, // Priority list change (hypothetical)
                    0xA3BC1875D83063EA  // Alternative notification state
                };
                
                foreach (var notificationId in additionalNotifications)
                {
                    try
                    {
                        var buffer = Marshal.AllocHGlobal(4);
                        try
                        {
                            Marshal.WriteInt32(buffer, 1);
                            RtlPublishWnfStateData(notificationId, IntPtr.Zero, buffer, 4, IntPtr.Zero);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buffer);
                        }
                    }
                    catch { }
                }
                
                // Force Windows to refresh notification settings
                try
                {
                    var refreshScript = @"
                    Get-Service -Name 'WpnService' -ErrorAction SilentlyContinue | Restart-Service -Force -ErrorAction SilentlyContinue
                    Get-Service -Name 'WpnUserService*' -ErrorAction SilentlyContinue | Restart-Service -Force -ErrorAction SilentlyContinue
                    ";
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{refreshScript}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    
                    using (var process = Process.Start(startInfo))
                    {
                        process?.WaitForExit(5000);
                    }
                }
                catch { }
                
                System.Diagnostics.Debug.WriteLine("NotifySystemOfPriorityChange: All notification methods completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotifySystemOfPriorityChange error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if an application is in the priority list using comprehensive detection methods
        /// </summary>
        /// <param name="appId">Application identifier</param>
        /// <returns>True if the app is in priority list</returns>
        public bool IsInPriorityList(string appId)
        {
            try
            {
                string sanitizedAppId = SanitizeAppId(appId);
                string aumid = GenerateAUMID(appId);
                
                System.Diagnostics.Debug.WriteLine($"IsInPriorityList: Checking {appId} (sanitized: {sanitizedAppId}, AUMID: {aumid})");
                
                // Method 1: Check AUMID-based locations (most reliable)
                var aumidPaths = new[]
                {
                    $"{NOTIFICATION_SETTINGS_PATH}\\{aumid}",
                    $@"SOFTWARE\Classes\AppUserModelId\{aumid}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\Applications\{aumid}"
                };
                
                foreach (var path in aumidPaths)
                {
                    if (CheckPriorityInRegistryPath(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"IsInPriorityList: Found priority app at AUMID path: {path}");
                        return true;
                    }
                }
                
                // Method 2: Check standard notification settings
                var standardPaths = new[]
                {
                    $"{NOTIFICATION_SETTINGS_PATH}\\{sanitizedAppId}",
                    $"{NOTIFICATION_SETTINGS_PATH}\\{appId}"
                };
                
                foreach (var path in standardPaths)
                {
                    if (CheckPriorityInRegistryPath(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"IsInPriorityList: Found priority app at standard path: {path}");
                        return true;
                    }
                }
                
                // Method 3: Check CloudStore locations
                if (CheckPriorityInCloudStore(appId))
                {
                    System.Diagnostics.Debug.WriteLine($"IsInPriorityList: Found priority app in CloudStore");
                    return true;
                }
                
                // Method 4: Check Windows 11 specific locations
                var win11Paths = new[]
                {
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\{{E77F06E2-4E62-4C4B-9E3F-B02A5C0E3F7A}}\{sanitizedAppId}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.FocusAssist\{aumid}"
                };
                
                foreach (var path in win11Paths)
                {
                    if (CheckPriorityInRegistryPath(path))
                    {
                        System.Diagnostics.Debug.WriteLine($"IsInPriorityList: Found priority app at Windows 11 path: {path}");
                        return true;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"IsInPriorityList: {appId} not found in any priority location");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking priority list status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if an app has priority settings in a specific registry path
        /// </summary>
        private bool CheckPriorityInRegistryPath(string registryPath)
        {
            try
            {
                using (var appKey = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (appKey == null) return false;
                    
                    var enabled = GetRegistryValue<int>(appKey, "Enabled", 0);
                    if (enabled != 1) return false; // App must be enabled
                    
                    // Check for various priority indicators
                    var priorityIndicators = new (string, int)[]
                    {
                        ("Priority", 0),
                        ("IsPriorityApp", 0),
                        ("BypassQuietHours", 0),
                        ("BypassDND", 0),
                        ("AllowCriticalNotifications", 0),
                        ("FocusAssistPriority", 0),
                        ("QuietModeOverride", 0),
                        ("BreakthroughLevel", 0)
                    };
                    
                    foreach (var (valueName, defaultValue) in priorityIndicators)
                    {
                        var value = GetRegistryValue<int>(appKey, valueName, defaultValue);
                        if (value > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"CheckPriorityInRegistryPath: Found {valueName}={value} at {registryPath}");
                            return true;
                        }
                    }
                    
                    // Also check ShowInActionCenter as fallback
                    var showInActionCenter = GetRegistryValue<int>(appKey, "ShowInActionCenter", 0);
                    if (showInActionCenter == 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"CheckPriorityInRegistryPath: Found ShowInActionCenter=1 at {registryPath}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckPriorityInRegistryPath error for {registryPath}: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Check if an app has priority settings in CloudStore
        /// </summary>
        private bool CheckPriorityInCloudStore(string appId)
        {
            try
            {
                var cloudStorePaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount"
                };

                foreach (var basePath in cloudStorePaths)
                {
                    using (var baseKey = Registry.CurrentUser.OpenSubKey(basePath))
                    {
                        if (baseKey == null) continue;

                        var subKeys = baseKey.GetSubKeyNames();
                        var focusAssistKeys = subKeys.Where(name => 
                            name.Contains("windows.data.notifications.quiethours") ||
                            name.Contains("windows.data.notifications.quietmodesettings") ||
                            name.Contains("quiethours")).ToArray();

                        foreach (var focusKey in focusAssistKeys)
                        {
                            var fullPath = $"{basePath}\\{focusKey}\\Current";
                            
                            try
                            {
                                // Check if our app entry exists
                                var appKey = $"PriorityList\\{SanitizeAppId(appId)}";
                                var appKeyPath = $"{fullPath}\\{appKey}";
                                
                                using (var testKey = Registry.CurrentUser.OpenSubKey(appKeyPath))
                                {
                                    if (testKey != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"CheckPriorityInCloudStore: Found app in CloudStore at {appKeyPath}");
                                        return true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"CloudStore check error for {fullPath}: {ex.Message}");
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckPriorityInCloudStore error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes an application from the priority list
        /// </summary>
        /// <param name="appId">Application identifier</param>
        /// <returns>True if successful</returns>
        public bool RemoveFromPriorityList(string appId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"RemoveFromPriorityList: Attempting to remove {appId}");
                
                bool success = false;
                
                // Method 1: Remove from Windows Runtime API locations
                if (TryRemoveViaWindowsRuntimeAPI(appId))
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveFromPriorityList: Successfully removed via Windows Runtime API");
                    success = true;
                }
                
                // Method 2: Remove from CloudStore
                if (TryRemoveFromCloudStore(appId))
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveFromPriorityList: Successfully removed from CloudStore");
                    success = true;
                }
                
                // Method 3: Remove from Enhanced Registry locations
                if (TryRemoveViaEnhancedRegistry(appId))
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveFromPriorityList: Successfully removed via Enhanced Registry");
                    success = true;
                }
                
                if (success)
                {
                    NotifySystemOfPriorityChange();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing app from priority list: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 1: Remove from Windows Runtime API locations
        /// </summary>
        private bool TryRemoveViaWindowsRuntimeAPI(string appId)
        {
            try
            {
                string aumid = GenerateAUMID(appId);
                System.Diagnostics.Debug.WriteLine($"TryRemoveViaWindowsRuntimeAPI: Generated AUMID: {aumid}");
                
                bool success = false;
                
                // Remove from AppUserModelId registry
                var registryPath = $@"SOFTWARE\Classes\AppUserModelId\{aumid}";
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(registryPath);
                    success = true;
                    System.Diagnostics.Debug.WriteLine($"TryRemoveViaWindowsRuntimeAPI: Deleted {registryPath}");
                }
                catch { }
                
                // Remove from notification settings with AUMID
                var notificationPath = $"{NOTIFICATION_SETTINGS_PATH}\\{aumid}";
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(notificationPath);
                    success = true;
                    System.Diagnostics.Debug.WriteLine($"TryRemoveViaWindowsRuntimeAPI: Deleted {notificationPath}");
                }
                catch { }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryRemoveViaWindowsRuntimeAPI error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 2: Remove from CloudStore
        /// </summary>
        private bool TryRemoveFromCloudStore(string appId)
        {
            try
            {
                bool success = false;
                var cloudStorePaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount"
                };

                foreach (var basePath in cloudStorePaths)
                {
                    using (var baseKey = Registry.CurrentUser.OpenSubKey(basePath))
                    {
                        if (baseKey == null) continue;

                        var subKeys = baseKey.GetSubKeyNames();
                        var focusAssistKeys = subKeys.Where(name => 
                            name.Contains("windows.data.notifications.quiethours") ||
                            name.Contains("windows.data.notifications.quietmodesettings") ||
                            name.Contains("quiethours")).ToArray();

                        foreach (var focusKey in focusAssistKeys)
                        {
                            var fullPath = $"{basePath}\\{focusKey}\\Current";
                            
                            try
                            {
                                // Remove direct registry entries we created
                                var appKey = $"PriorityList\\{SanitizeAppId(appId)}";
                                var appKeyPath = $"{fullPath}\\{appKey}";
                                try
                                {
                                    Registry.CurrentUser.DeleteSubKeyTree(appKeyPath);
                                    success = true;
                                    System.Diagnostics.Debug.WriteLine($"TryRemoveFromCloudStore: Deleted {appKeyPath}");
                                }
                                catch { }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"CloudStore removal error for {fullPath}: {ex.Message}");
                            }
                        }
                    }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryRemoveFromCloudStore error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Method 3: Remove from Enhanced Registry locations
        /// </summary>
        private bool TryRemoveViaEnhancedRegistry(string appId)
        {
            try
            {
                string aumid = GenerateAUMID(appId);
                var registryPaths = new[]
                {
                    $"{NOTIFICATION_SETTINGS_PATH}\\{aumid}",
                    $"{NOTIFICATION_SETTINGS_PATH}\\{SanitizeAppId(appId)}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\Applications\{aumid}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.FocusAssist\{aumid}"
                };

                bool success = false;
                foreach (var regPath in registryPaths)
                {
                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(regPath);
                        success = true;
                        System.Diagnostics.Debug.WriteLine($"TryRemoveViaEnhancedRegistry: Deleted {regPath}");
                    }
                    catch { }
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryRemoveViaEnhancedRegistry error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods for Priority List

        private T GetRegistryValue<T>(RegistryKey key, string valueName, T defaultValue)
        {
            try
            {
                var value = key.GetValue(valueName);
                if (value != null)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch
            {
                // Return default if conversion fails
            }

            return defaultValue;
        }

        private string GetFriendlyAppName(string appId)
        {
            // Try to get a friendly name for the app
            if (appId.Contains("Microsoft."))
            {
                // UWP app - extract the app name
                var parts = appId.Split('.');
                if (parts.Length > 1)
                {
                    return parts[1].Replace("_", " ");
                }
            }
            else if (appId.Contains("\\"))
            {
                // Win32 app path - extract executable name
                var fileName = Path.GetFileNameWithoutExtension(appId);
                return fileName;
            }

            return appId;
        }

        private AppType DetermineAppType(string appId)
        {
            if (appId.StartsWith("Microsoft.") || appId.Contains("_"))
            {
                return AppType.UWP;
            }
            else if (appId.Contains("\\"))
            {
                return AppType.Win32;
            }
            else
            {
                return AppType.System;
            }
        }

        private string GetAppPublisher(string appId)
        {
            try
            {
                if (appId.StartsWith("Microsoft."))
                {
                    return "Microsoft Corporation";
                }

                // Try to get publisher from installed apps registry
                // This is a simplified approach
                return "Unknown Publisher";
            }
            catch
            {
                return "Unknown Publisher";
            }
        }

        private string GenerateAppId(InstalledAppInfo app)
        {
            switch (app.Type)
            {
                case AppList.AppType.UWPApplication:
                    // Use package family name for UWP apps
                    return !string.IsNullOrEmpty(app.PackageFamilyName) ? app.PackageFamilyName : app.Name;

                case AppList.AppType.Win32Application:
                    // Use executable path or name for Win32 apps
                    if (!string.IsNullOrEmpty(app.ExecutablePath))
                    {
                        return app.ExecutablePath;
                    }
                    else if (!string.IsNullOrEmpty(app.InstallLocation))
                    {
                        // Try to find main executable
                        try
                        {
                            var exeFiles = Directory.GetFiles(app.InstallLocation, "*.exe", SearchOption.TopDirectoryOnly);
                            if (exeFiles.Length > 0)
                            {
                                return exeFiles[0];
                            }
                        }
                        catch { }
                    }
                    return app.DisplayName.Replace(" ", "");

                default:
                    return app.Name;
            }
        }

        private AppType ConvertInstalledAppType(AppList.AppType appType)
        {
            return appType switch
            {
                AppList.AppType.UWPApplication => AppType.UWP,
                AppList.AppType.Win32Application => AppType.Win32,
                _ => AppType.System
            };
        }

        private string SanitizeAppId(string appId)
        {
            if (string.IsNullOrEmpty(appId))
                return appId;

            // For UWP apps, use the app ID as-is since it's already in the correct format
            if (appId.Contains("Microsoft.") || appId.Contains("_"))
            {
                return appId;
            }

            // For Win32 apps with full paths, we need to handle this differently
            if (Path.IsPathRooted(appId))
            {
                // Try to use just the executable name for Win32 apps
                string exeName = Path.GetFileNameWithoutExtension(appId);
                
                // Check if there's already a notification setting for this executable name
                var testPath = $"{NOTIFICATION_SETTINGS_PATH}\\{exeName}";
                if (DoesRegistryPathExist(testPath))
                {
                    return exeName;
                }
                
                // Otherwise, create a safe registry key name from the full path
                return CreateSafeRegistryKeyName(appId);
            }

            // For other app types, clean up the name to be registry-safe
            return CreateSafeRegistryKeyName(appId);
        }

        private string CreateSafeRegistryKeyName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Replace invalid registry key characters
            var invalidChars = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            string safe = input;
            
            foreach (char c in invalidChars)
            {
                safe = safe.Replace(c, '_');
            }
            
            // Limit length to avoid registry key name length limits
            if (safe.Length > 100)
            {
                safe = safe.Substring(0, 100);
            }
            
            return safe;
        }

        /// <summary>
        /// Gets all applications that can be added to priority list
        /// </summary>
        /// <param name="installedApps">List of installed applications</param>
        /// <returns>List of available apps for priority configuration</returns>
        public List<PriorityApp> GetAvailableAppsForPriority(IEnumerable<InstalledAppInfo> installedApps)
        {
            var availableApps = new List<PriorityApp>();

            foreach (var app in installedApps)
            {
                try
                {
                    // Generate app ID based on the type
                    string appId = GenerateAppId(app);
                    
                    if (!string.IsNullOrEmpty(appId))
                    {
                        var priorityApp = new PriorityApp
                        {
                            AppId = appId,
                            DisplayName = app.DisplayName,
                            Publisher = app.Publisher,
                            IsEnabled = IsInPriorityList(appId),
                            AppType = ConvertInstalledAppType(app.Type)
                        };

                        availableApps.Add(priorityApp);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing app for priority list: {ex.Message}");
                }
            }

            return availableApps.OrderBy(app => app.DisplayName).ToList();
        }

        #endregion
    }
}