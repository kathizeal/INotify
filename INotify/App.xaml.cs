using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using INotify.Manager;
using INotify.Services;
using INotify.Util;
using INotify.KToastViewModel.ViewModelContract;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using WinCommon.Util;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI3Component.Util;
using WinLogger;
using WinLogger.Contract;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using System.Threading.Tasks;
using INotify.KToastDI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private SingleInstanceManager? _singleInstanceManager;
        private Window? m_window;
        private TrayManager? _trayManager;
        private BackgroundNotificationService? _backgroundService;
        private bool _isWindowVisible = true;

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// 
        /// Single instance support is initialized here to ensure only one instance of the application
        /// can run at a time.
        /// </summary>
        public App()
        {
            InitializeDependencyInjection();
            this.InitializeComponent();
            WinUI3AppInfo.Initialize("INotify");
            ZAppInfoProvider.Initialize(WinUI3AppInfo.Instance);
            
            // Initialize DI services first
            
            // Initialize single instance management
            InitializeSingleInstance();
        }

        /// <summary>
        /// Initializes dependency injection services
        /// </summary>
        private void InitializeDependencyInjection()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Initializing dependency injection services");
                
                // Initialize the main DI container
                InitializationManager.Instance.InitializeDI();

                                // Also ensure KToastDIServiceProvider is initialized
                                var testService = INotify.KToastDI.KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();
                Logger.Info(LogManager.GetCallerInfo(), $"KToastDIServiceProvider test: {(testService != null ? "Success" : "Failed")}");
                
                Logger.Info(LogManager.GetCallerInfo(), "Dependency injection services initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error initializing dependency injection: {ex.Message}");
                // Don't throw here as we want the app to continue even if DI fails
            }
        }

        /// <summary>
        /// Initializes single instance functionality
        /// </summary>
        private async void InitializeSingleInstance()
        {
            try
            {
                _singleInstanceManager = new SingleInstanceManager();
                
                if (!_singleInstanceManager.IsFirstInstance)
                {
                    Logger.Info(LogManager.GetCallerInfo(), "Another instance is already running. Notifying existing instance and exiting.");
                    
                    // Get command line arguments
                    var args = Environment.GetCommandLineArgs();
                    
                    // Notify the existing instance
                    bool notified = await _singleInstanceManager.NotifyExistingInstanceAsync(args);
                    
                    if (notified)
                    {
                        Logger.Info(LogManager.GetCallerInfo(), "Successfully notified existing instance. Exiting current instance.");
                    }
                    else
                    {
                        Logger.Warning(LogManager.GetCallerInfo(), "Failed to notify existing instance, but another instance is detected. Exiting anyway.");
                    }
                    
                    // Exit the current instance
                    this.Exit();
                    return;
                }
                
                // This is the first instance, set up event handling
                _singleInstanceManager.AnotherInstanceDetected += OnAnotherInstanceDetected;
                
                Logger.Info(LogManager.GetCallerInfo(), "First instance initialized successfully with single instance support.");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error initializing single instance: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for when another instance is detected
        /// </summary>
        private void OnAnotherInstanceDetected(object? sender, string[] args)
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), $"Another instance detected with args: {string.Join(" ", args)}");
                
                // Bring the main window to foreground
                ShowMainWindow();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error handling another instance detection: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Application launched");

                // Initialize library services for the current user
                InitializeLibraryServices();

                // Initialize background services first
                InitializeBackgroundServices();

                // Create and show main window
                m_window = new MainWindow();
                m_window.Closed += OnMainWindowClosed;
                
                // Set the main window reference for toast activation
                ToastActivationService.SetMainWindow(m_window);
                
                m_window.Activate();

                Logger.Info(LogManager.GetCallerInfo(), "Main window created and activated");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error during application launch: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the main window and brings it to foreground
        /// </summary>
        private void ShowMainWindow()
        {
            try
            {
                if (m_window == null)
                {
                    m_window = new MainWindow();
                    m_window.Closed += OnMainWindowClosed;
                    ToastActivationService.SetMainWindow(m_window);
                }

                m_window.Activate();
                if (m_window is MainWindow mainWindow)
                {
                    mainWindow.BringToForeground();
                }

                _isWindowVisible = true;
                Logger.Info(LogManager.GetCallerInfo(), "Main window shown and activated");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error showing main window: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the main window to system tray
        /// </summary>
        private void HideMainWindow()
        {
            try
            {
                // For WinUI 3, we need to use SetWindowPos to hide the window
                if (m_window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
                    ShowWindow(hwnd, 0); // SW_HIDE
                    _isWindowVisible = false;
                    
                    _trayManager?.ShowBalloonTip("INotify", "Application minimized to tray. Notifications will continue to be monitored.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error hiding main window: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for tray "Show" menu item
        /// </summary>
        private void OnShowMainWindowRequested(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        /// <summary>
        /// Event handler for tray "Exit" menu item
        /// </summary>
        private void OnExitApplicationRequested(object? sender, EventArgs e)
        {
            ExitApplication();
        }

        /// <summary>
        /// Completely exits the application
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Exiting application");

                // Clean up background services
                _backgroundService?.Dispose();
                _trayManager?.Dispose();
                _singleInstanceManager?.Dispose();

                // Close the main window properly
                if (m_window != null)
                {
                    m_window.Closed -= OnMainWindowClosed; // Prevent hiding logic
                    m_window.Close();
                }

                // Exit the application
                Exit();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error during application exit: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts for showing/hiding the application
        /// This can be called from the main window to toggle visibility
        /// </summary>
        public void ToggleWindowVisibility()
        {
            if (_isWindowVisible)
            {
                HideMainWindow();
            }
            else
            {
                ShowMainWindow();
            }
        }

        /// <summary>
        /// Initializes library services for the current user
        /// </summary>
        private async void InitializeLibraryServices()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Initializing library services");
                
                // Initialize library services (database, etc.)
                await InitializationManager.Instance.InitializeLibraryServicesForCurrentUser();
                
                Logger.Info(LogManager.GetCallerInfo(), "Library services initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error initializing library services: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes background services including tray and notification monitoring
        /// </summary>
        private async void InitializeBackgroundServices()
        {
            try
            {
                // Run tray diagnostics first (only in debug mode)
#if DEBUG
                Debug.WriteLine("Running tray diagnostics...");
                await TrayDiagnostics.RunTrayDiagnosticsAsync();
                TrayDiagnostics.CheckNotificationAreaSettings();
#endif

                // Initialize tray manager first
                _trayManager = new TrayManager();
                _trayManager.ShowMainWindowRequested += OnShowMainWindowRequested;
                _trayManager.ExitApplicationRequested += OnExitApplicationRequested;
                
                try
                {
                    _trayManager.Initialize();
                    Logger.Info(LogManager.GetCallerInfo(), "Tray manager initialized successfully");
                    
                    // Test the tray icon by showing a welcome notification
                    _trayManager.ShowBalloonTip("INotify Started", "INotify is now running in the system tray. Click the icon to show the window.");
                }
                catch (Exception trayEx)
                {
                    Logger.Warning(LogManager.GetCallerInfo(), $"Tray manager initialization had issues but continuing: {trayEx.Message}");
                    
                    // Run quick tray test to help diagnose the issue
#if DEBUG
                    Debug.WriteLine("Running quick tray test due to initialization issues...");
                    await TrayDiagnostics.QuickTrayTestAsync();
#endif
                    // Continue without tray functionality if it fails
                }

                // Initialize background notification service
                _backgroundService = new BackgroundNotificationService();
                bool started = await _backgroundService.StartAsync();
                
                if (started)
                {
                    Logger.Info(LogManager.GetCallerInfo(), "Background notification service started successfully");
                }
                else
                {
                    Logger.Warning(LogManager.GetCallerInfo(), "Failed to start background notification service");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error initializing background services: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles main window close event - minimizes to tray instead of exiting
        /// </summary>
        private void OnMainWindowClosed(object sender, WindowEventArgs e)
        {
            try
            {
                // Prevent the window from actually closing, just hide it
                e.Handled = true;
                HideMainWindow();
                
                Logger.Info(LogManager.GetCallerInfo(), "Main window hidden to tray");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error handling window close: {ex.Message}");
            }
        }

        // P/Invoke for hiding window
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
