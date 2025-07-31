using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace WindowsDnd
{
    /// <summary>
    /// Windows Do Not Disturb (Focus Assist/Quiet Hours) Manager
    /// Provides functionality to get and set Windows DND modes
    /// </summary>
    public class WindowsDndManager : IDisposable
    {
        private IQuietHoursSettings _quietHoursSettings;
        private bool _disposed = false;

        // Profile constants
        private const string PROFILE_UNRESTRICTED = "Microsoft.QuietHoursProfile.Unrestricted";
        private const string PROFILE_PRIORITY_ONLY = "Microsoft.QuietHoursProfile.PriorityOnly";
        private const string PROFILE_ALARMS_ONLY = "Microsoft.QuietHoursProfile.AlarmsOnly";

        /// <summary>
        /// Available DND modes
        /// </summary>
        public enum DndMode
        {
            Off,
            PriorityOnly,
            AlarmsOnly
        }

        /// <summary>
        /// Initializes the Windows DND Manager
        /// </summary>
        /// <exception cref="COMException">Thrown when COM initialization fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when unable to create QuietHoursSettings instance</exception>
        public WindowsDndManager()
        {
            try
            {
                // Create instance of QuietHoursSettings COM object
                var type = Type.GetTypeFromCLSID(new Guid("f53321fa-34f8-4b7f-b9a3-361877cb94cf"));
                _quietHoursSettings = (IQuietHoursSettings)Activator.CreateInstance(type);
            }
            catch (COMException ex)
            {
                throw new COMException($"Failed to initialize COM interface: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to create QuietHoursSettings instance: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the current DND mode
        /// </summary>
        /// <returns>Current DND mode</returns>
        /// <exception cref="COMException">Thrown when unable to retrieve current profile</exception>
        /// <exception cref="InvalidOperationException">Thrown when profile is unrecognized</exception>
        public DndMode GetCurrentMode()
        {
            ThrowIfDisposed();

            try
            {
                string profileId = _quietHoursSettings.UserSelectedProfile;
                
                return profileId switch
                {
                    PROFILE_UNRESTRICTED => DndMode.Off,
                    PROFILE_PRIORITY_ONLY => DndMode.PriorityOnly,
                    PROFILE_ALARMS_ONLY => DndMode.AlarmsOnly,
                    _ => throw new InvalidOperationException($"Unrecognized profile: {profileId}")
                };
            }
            catch (COMException ex)
            {
                throw new COMException($"Unable to retrieve current DND mode: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the DND mode
        /// </summary>
        /// <param name="mode">DND mode to set</param>
        /// <exception cref="COMException">Thrown when unable to set the profile</exception>
        public void SetMode(DndMode mode)
        {
            ThrowIfDisposed();

            string profileToSet = mode switch
            {
                DndMode.Off => PROFILE_UNRESTRICTED,
                DndMode.PriorityOnly => PROFILE_PRIORITY_ONLY,
                DndMode.AlarmsOnly => PROFILE_ALARMS_ONLY,
                _ => throw new ArgumentException($"Invalid DND mode: {mode}")
            };

            try
            {
                _quietHoursSettings.UserSelectedProfile = profileToSet;
            }
            catch (COMException ex)
            {
                throw new COMException($"Unable to set DND mode to {mode}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Turns off DND mode
        /// </summary>
        public void TurnOff() => SetMode(DndMode.Off);

        /// <summary>
        /// Sets DND mode to Priority Only
        /// </summary>
        public void SetPriorityOnly() => SetMode(DndMode.PriorityOnly);

        /// <summary>
        /// Sets DND mode to Alarms Only
        /// </summary>
        public void SetAlarmsOnly() => SetMode(DndMode.AlarmsOnly);

        /// <summary>
        /// Gets a user-friendly string representation of the current mode
        /// </summary>
        /// <returns>String representation of current mode</returns>
        public string GetCurrentModeString()
        {
            var mode = GetCurrentMode();
            return mode switch
            {
                DndMode.Off => "off",
                DndMode.PriorityOnly => "priority-only",
                DndMode.AlarmsOnly => "alarms-only",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Checks if DND is currently enabled (any mode except Off)
        /// </summary>
        /// <returns>True if DND is enabled, false otherwise</returns>
        public bool IsEnabled()
        {
            return GetCurrentMode() != DndMode.Off;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsDndManager));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_quietHoursSettings != null)
                    {
                        Marshal.ReleaseComObject(_quietHoursSettings);
                        _quietHoursSettings = null;
                    }
                }
                _disposed = true;
            }
        }

        ~WindowsDndManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// COM Interface for IQuietHoursSettings
    /// </summary>
    [ComImport]
    [Guid("6bff4732-81ec-4ffb-ae67-b6c1bc29631f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IQuietHoursSettings
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string UserSelectedProfile
        {
            [return: MarshalAs(UnmanagedType.LPWStr)]
            get;
            [param: In, MarshalAs(UnmanagedType.LPWStr)]
            set;
        }

        // Additional methods from the IDL can be added here as needed
        // For now, we only need UserSelectedProfile for basic DND functionality
    }

    /// <summary>
    /// Example usage class demonstrating how to use WindowsDndManager
    /// </summary>
    public static class WindowsDndExample
    {
        /// <summary>
        /// Example method showing basic usage
        /// </summary>
        public static void ExampleUsage()
        {
            try
            {
                using var dndManager = new WindowsDndManager();

                // Get current mode
                var currentMode = dndManager.GetCurrentMode();
                Console.WriteLine($"Current DND mode: {currentMode}");

                // Check if DND is enabled
                bool isEnabled = dndManager.IsEnabled();
                Console.WriteLine($"DND is enabled: {isEnabled}");

                // Set different modes
                dndManager.SetPriorityOnly();
                Console.WriteLine("Set to Priority Only mode");

                dndManager.SetAlarmsOnly();
                Console.WriteLine("Set to Alarms Only mode");

                dndManager.TurnOff();
                Console.WriteLine("Turned off DND");

                // Get string representation
                string modeString = dndManager.GetCurrentModeString();
                Console.WriteLine($"Current mode as string: {modeString}");
            }
            catch (COMException ex)
            {
                Console.WriteLine($"COM Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}