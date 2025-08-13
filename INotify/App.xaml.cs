using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using INotify.Manager;
using INotify.Util;
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

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// 
        /// Single instance support is initialized here to ensure only one instance of the application
        /// can run at a time.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            WinUI3AppInfo.Initialize("INotify");
            ZAppInfoProvider.Initialize(WinUI3AppInfo.Instance);
            
            // Initialize single instance management
            InitializeSingleInstance();
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
                // Continue anyway in case of error
            }
        }

        /// <summary>
        /// Handles when another instance is detected
        /// </summary>
        private void OnAnotherInstanceDetected(object? sender, string[] args)
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), $"Another instance detected with args: {string.Join(", ", args)}");
                
                // Bring main window to foreground on UI thread
                if (m_window != null)
                {
                    var dispatcher = DispatcherQueue.GetForCurrentThread();
                    if (dispatcher != null)
                    {
                        dispatcher.TryEnqueue(() =>
                        {
                            BringMainWindowToForeground();
                        });
                    }
                    else
                    {
                        // Fallback: try to activate directly
                        BringMainWindowToForeground();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error handling another instance detection: {ex.Message}");
            }
        }

        /// <summary>
        /// Brings the main window to foreground
        /// </summary>
        private void BringMainWindowToForeground()
        {
            try
            {
                if (m_window != null)
                {
                    // Activate the window
                    m_window.Activate();
                    
                    // If MainWindow has a BringToForeground method, call it
                    if (m_window is MainWindow mainWindow)
                    {
                        mainWindow.BringToForeground();
                    }
                    
                    Logger.Info(LogManager.GetCallerInfo(), "Main window brought to foreground.");
                }
                else
                {
                    Logger.Warning(LogManager.GetCallerInfo(), "Main window is null, cannot bring to foreground.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error bringing main window to foreground: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Only proceed if this is the first instance
                if (_singleInstanceManager?.IsFirstInstance != true)
                {
                    Logger.Info(LogManager.GetCallerInfo(), "OnLaunched called on non-first instance, exiting.");
                    return;
                }

                await InitializationManager.Instance.InitializeApp(args);
                m_window = new MainWindow();
                m_window.Activate();

                // Subscribe to window closed event for cleanup
                if (m_window is MainWindow mainWindow)
                {
                    mainWindow.Closed += (s, e) =>
                    {
                        CleanupSingleInstance();
                    };
                }

                Logger.Info(LogManager.GetCallerInfo(), "Application launched successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error in OnLaunched: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clean up single instance resources
        /// </summary>
        private void CleanupSingleInstance()
        {
            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Cleaning up single instance resources.");
                
                if (_singleInstanceManager != null)
                {
                    _singleInstanceManager.AnotherInstanceDetected -= OnAnotherInstanceDetected;
                    _singleInstanceManager.Dispose();
                    _singleInstanceManager = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error during single instance cleanup: {ex.Message}");
            }
        }
    }
}
