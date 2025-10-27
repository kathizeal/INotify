using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinLogger;
using WinLogger.Contract;

namespace INotify
{
    /// <summary>
    /// Custom Program class implementing single instance behavior for WinUI 3 app
    /// Based on Microsoft's official documentation for Windows App SDK
    /// </summary>
    public class Program
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private static IntPtr redirectEventHandle = IntPtr.Zero;

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "INotify starting with single instance check");
                
                // Initialize COM wrappers for WinRT
                WinRT.ComWrappersSupport.InitializeComWrappers();
                
                // Check if this instance should redirect to existing instance
                bool isRedirect = DecideRedirection();

                if (!isRedirect)
                {
                    Logger.Info(LogManager.GetCallerInfo(), "Starting new INotify instance");
                    
                    // Start the application normally
                    Application.Start((p) =>
                    {
                        var context = new DispatcherQueueSynchronizationContext(
                            DispatcherQueue.GetForCurrentThread());
                        SynchronizationContext.SetSynchronizationContext(context);
                        _ = new App();
                    });
                }
                else
                {
                    Logger.Info(LogManager.GetCallerInfo(), "Redirected to existing INotify instance, exiting");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error in Main: {ex.Message}");
                Debug.WriteLine($"? Error in Program.Main: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Determines whether to redirect to existing instance or allow new instance
        /// </summary>
        /// <returns>True if should redirect, false if should continue with new instance</returns>
        private static bool DecideRedirection()
        {
            try
            {
                bool isRedirect = false;
                
                // Get current activation arguments
                AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
                ExtendedActivationKind kind = args.Kind;
                
                Logger.Info(LogManager.GetCallerInfo(), $"Activation kind: {kind}");
                
                // Register or find existing instance with unique key
                AppInstance keyInstance = AppInstance.FindOrRegisterForKey("INotify_SingleInstance_Key");

                if (keyInstance.IsCurrent)
                {
                    // This is the main/first instance
                    Logger.Info(LogManager.GetCallerInfo(), "This is the main instance, setting up activation handler");
                    keyInstance.Activated += OnActivated;
                }
                else
                {
                    // Another instance exists, redirect to it
                    Logger.Info(LogManager.GetCallerInfo(), "Another instance exists, redirecting activation");
                    isRedirect = true;
                    RedirectActivationTo(args, keyInstance);
                }

                return isRedirect;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error in DecideRedirection: {ex.Message}");
                Debug.WriteLine($"? Error in DecideRedirection: {ex.Message}");
                return false; // Allow new instance on error
            }
        }

        /// <summary>
        /// Redirects activation to the existing instance and brings it to foreground
        /// </summary>
        /// <param name="args">Activation arguments to pass to existing instance</param>
        /// <param name="keyInstance">The existing app instance to redirect to</param>
        private static void RedirectActivationTo(AppActivationArguments args, AppInstance keyInstance)
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), $"Redirecting to process ID: {keyInstance.ProcessId}");
                
                // Create event handle for synchronization
                redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
                
                // Perform redirection on separate thread
                Task.Run(() =>
                {
                    try
                    {
                        keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                        SetEvent(redirectEventHandle);
                        Logger.Info(LogManager.GetCallerInfo(), "Redirection completed successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LogManager.GetCallerInfo(), $"Error during redirection: {ex.Message}");
                        SetEvent(redirectEventHandle); // Ensure event is set even on error
                    }
                });

                // Wait for redirection to complete
                const uint CWMO_DEFAULT = 0;
                const uint INFINITE = 0xFFFFFFFF;
                _ = CoWaitForMultipleObjects(
                   CWMO_DEFAULT, INFINITE, 1,
                   [redirectEventHandle], out uint handleIndex);

                // Bring the existing window to foreground
                try
                {
                    Process process = Process.GetProcessById((int)keyInstance.ProcessId);
                    if (process?.MainWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(process.MainWindowHandle);
                        Logger.Info(LogManager.GetCallerInfo(), "Brought existing window to foreground");
                    }
                    else
                    {
                        Logger.Warning(LogManager.GetCallerInfo(), "Could not get main window handle for existing process");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(LogManager.GetCallerInfo(), $"Could not bring window to foreground: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error in RedirectActivationTo: {ex.Message}");
                Debug.WriteLine($"? Error in RedirectActivationTo: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles activation events for the main instance
        /// Called when other instances attempt to activate this app
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="args">Activation arguments from the other instance</param>
        private static void OnActivated(object sender, AppActivationArguments args)
        {
            try
            {
                ExtendedActivationKind kind = args.Kind;
                Logger.Info(LogManager.GetCallerInfo(), $"Main instance activated with kind: {kind}");
                
                // Get the current app instance to bring window to foreground
                var currentApp = Microsoft.UI.Xaml.Application.Current as App;
                if (currentApp != null)
                {
                    // Bring the main window to foreground
                    currentApp.ShowMainWindow();
                    Logger.Info(LogManager.GetCallerInfo(), "Brought main window to foreground via activation");
                }
                else
                {
                    Logger.Warning(LogManager.GetCallerInfo(), "Could not get current App instance for activation");
                }

                // TODO: Handle specific activation scenarios if needed
                // For example, file activation, protocol activation, etc.
                Debug.WriteLine($"?? Main instance activated: {kind}");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error in OnActivated: {ex.Message}");
                Debug.WriteLine($"? Error in OnActivated: {ex.Message}");
            }
        }

        #region Win32 API Imports

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes, bool bManualReset,
            bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("ole32.dll")]
        private static extern uint CoWaitForMultipleObjects(
            uint dwFlags, uint dwMilliseconds, ulong nHandles,
            IntPtr[] pHandles, out uint dwIndex);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion
    }
}