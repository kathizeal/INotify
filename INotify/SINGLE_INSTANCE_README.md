# Single Instance Implementation

## Overview

The INotify application now supports single instance functionality, meaning only one instance of the application can run at a time. When a user tries to launch a second instance, the existing instance will be brought to the foreground instead.

## Implementation Details

### Components

1. **SingleInstanceManager** (`INotify\Util\SingleInstanceManager.cs`)
   - Manages single instance functionality using a named mutex
   - Uses named pipes for inter-process communication
   - Handles bringing existing window to foreground

2. **App.xaml.cs** - Updated to integrate single instance support
   - Initializes `SingleInstanceManager` in constructor
   - Checks if another instance is running during startup
   - Handles bringing window to foreground when another instance is detected

3. **MainWindow.xaml.cs** - Added `BringToForeground()` method

### How It Works

1. **Mutex-based Detection**: Uses a globally named mutex (`Global\\INotify_SingleInstance_Mutex_A7B8C9D0`) to detect if another instance is running.

2. **Inter-Process Communication**: Uses named pipes (`INotify_SingleInstance_Pipe`) to communicate between instances.

3. **Window Activation**: When a new instance is detected, the existing instance is brought to the foreground using Win32 APIs.

### Key Features

- **Cross-session support**: Uses global mutex to work across user sessions
- **Command line argument passing**: New instances can pass their command line arguments to the existing instance
- **Robust error handling**: Continues to work even if some components fail
- **Proper cleanup**: Resources are properly disposed when the application closes
- **Logging**: Full logging support for debugging and monitoring

### Security Considerations

- Uses global named objects which are visible across sessions
- Named pipe communication is local to the machine
- Mutex name includes a GUID-like suffix to prevent conflicts

### Usage

No code changes are required by other parts of the application. The single instance functionality is automatically initialized when the application starts.

### Troubleshooting

If single instance functionality is not working:

1. Check the application logs for any errors during `SingleInstanceManager` initialization
2. Verify that the application has permissions to create global named objects
3. Ensure no antivirus software is blocking named pipe communication
4. Check if the mutex is being held by a crashed process (restart may be needed)

### Error Recovery

The implementation includes fallback mechanisms:
- If mutex creation fails, the application will still start (defaulting to allowing multiple instances)
- If named pipe communication fails, the application will still attempt to bring the window to foreground using process enumeration
- Timeout mechanisms prevent hanging on pipe operations