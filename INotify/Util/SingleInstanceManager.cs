using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinLogger;
using WinLogger.Contract;

namespace INotify.Util
{
    /// <summary>
    /// Manages single instance functionality for the application
    /// </summary>
    public sealed class SingleInstanceManager : IDisposable
    {
        #region Win32 API
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        #endregion

        #region Constants
        private const string MUTEX_NAME = "Global\\INotify_SingleInstance_Mutex_A7B8C9D0";
        private const string PIPE_NAME = "INotify_SingleInstance_Pipe";
        private const int PIPE_TIMEOUT = 5000;
        #endregion

        #region Private Members
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly Mutex _mutex;
        private readonly NamedPipeServerStream _pipeServer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;
        private bool _isFirstInstance;
        #endregion

        #region Events
        /// <summary>
        /// Event fired when another instance attempts to start
        /// </summary>
        public event EventHandler<string[]>? AnotherInstanceDetected;
        #endregion

        #region Constructor
        public SingleInstanceManager()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                // Try to create or open the mutex
                _mutex = new Mutex(true, MUTEX_NAME, out _isFirstInstance);
                
                if (_isFirstInstance)
                {
                    Logger.Info(LogManager.GetCallerInfo(), "First instance detected, setting up named pipe server");
                    
                    // This is the first instance, set up named pipe server
                    _pipeServer = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);
                    
                    // Start listening for connections from other instances
                    _ = Task.Run(ListenForOtherInstances, _cancellationTokenSource.Token);
                }
                else
                {
                    Logger.Info(LogManager.GetCallerInfo(), "Another instance is already running");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error initializing SingleInstanceManager: {ex.Message}");
                _isFirstInstance = true; // Default to allowing instance if we can't determine
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets whether this is the first instance of the application
        /// </summary>
        public bool IsFirstInstance => _isFirstInstance;
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to notify the existing instance and bring it to foreground
        /// </summary>
        /// <param name="args">Command line arguments to send to the existing instance</param>
        /// <returns>True if notification was successful, false otherwise</returns>
        public async Task<bool> NotifyExistingInstanceAsync(string[] args)
        {
            if (_isFirstInstance)
            {
                Logger.Warning(LogManager.GetCallerInfo(), "NotifyExistingInstance called on first instance");
                return false;
            }

            try
            {
                // Try to connect to the existing instance via named pipe
                using var pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out);
                
                var connectTask = pipeClient.ConnectAsync(PIPE_TIMEOUT);
                await connectTask;

                if (pipeClient.IsConnected)
                {
                    // Send the command line arguments
                    var message = string.Join("|", args);
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    
                    await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await pipeClient.FlushAsync();
                    
                    Logger.Info(LogManager.GetCallerInfo(), $"Successfully notified existing instance with args: {message}");
                    
                    // Try to bring the existing window to foreground
                    await BringExistingInstanceToForegroundAsync();
                    
                    return true;
                }
            }
            catch (TimeoutException)
            {
                Logger.Warning(LogManager.GetCallerInfo(), "Timeout connecting to existing instance");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error notifying existing instance: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Attempts to bring the existing instance window to foreground
        /// </summary>
        private async Task BringExistingInstanceToForegroundAsync()
        {
            try
            {
                // Find the main window of the existing process
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);
                
                foreach (var process in processes)
                {
                    if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        var handle = process.MainWindowHandle;
                        
                        // If the window is minimized, restore it
                        if (IsIconic(handle))
                        {
                            ShowWindow(handle, SW_RESTORE);
                        }
                        else
                        {
                            ShowWindow(handle, SW_SHOW);
                        }
                        
                        // Bring window to foreground
                        SetForegroundWindow(handle);
                        
                        Logger.Info(LogManager.GetCallerInfo(), "Brought existing instance window to foreground");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error bringing existing instance to foreground: {ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Listens for connections from other instances
        /// </summary>
        private async Task ListenForOtherInstances()
        {
            if (_pipeServer == null)
                return;

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Wait for a client to connect
                        await _pipeServer.WaitForConnectionAsync(_cancellationTokenSource.Token);
                        
                        Logger.Info(LogManager.GetCallerInfo(), "Another instance connected via named pipe");
                        
                        // Read the message from the client
                        var buffer = new byte[1024];
                        var bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                        
                        if (bytesRead > 0)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            var args = message.Split('|', StringSplitOptions.RemoveEmptyEntries);
                            
                            Logger.Info(LogManager.GetCallerInfo(), $"Received message from another instance: {message}");
                            
                            // Fire the event to notify the application
                            AnotherInstanceDetected?.Invoke(this, args);
                        }
                        
                        // Disconnect the client
                        _pipeServer.Disconnect();
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LogManager.GetCallerInfo(), $"Error in named pipe communication: {ex.Message}");
                        
                        // Wait a bit before retrying
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                Logger.Info(LogManager.GetCallerInfo(), "Named pipe listener cancelled");
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Fatal error in named pipe listener: {ex.Message}");
            }
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                Logger.Info(LogManager.GetCallerInfo(), "Disposing SingleInstanceManager");
                
                _cancellationTokenSource?.Cancel();
                _pipeServer?.Dispose();
                
                if (_mutex != null)
                {
                    if (_isFirstInstance)
                    {
                        _mutex.ReleaseMutex();
                    }
                    _mutex.Dispose();
                }
                
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), $"Error disposing SingleInstanceManager: {ex.Message}");
            }
            finally
            {
                _isDisposed = true;
            }
        }
        #endregion
    }
}