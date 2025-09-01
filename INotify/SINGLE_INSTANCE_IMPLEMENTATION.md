# INotify Single Instance Implementation

## Overview

INotify now implements proper single instance behavior using Microsoft's official Windows App SDK pattern for WinUI 3 applications. This ensures only one instance of the application can run at a time, following Microsoft's best practices.

## Implementation Details

### Architecture

The single instance implementation follows Microsoft's official documentation and uses:

1. **Custom Program.cs** - Entry point with single instance logic
2. **Microsoft.Windows.AppLifecycle APIs** - Official Windows App SDK APIs
3. **AppInstance.FindOrRegisterForKey()** - Proper instance registration
4. **Activation redirection** - Redirects new instances to existing one

### Key Components

#### 1. Program.cs (Entry Point)
```csharp
[STAThread]
static int Main(string[] args)
{
    // Initialize COM wrappers for WinRT
    WinRT.ComWrappersSupport.InitializeComWrappers();
    
    // Check if this instance should redirect to existing instance
    bool isRedirect = DecideRedirection();

    if (!isRedirect)
    {
        // Start the application normally
        Application.Start((p) => { /* ... */ });
    }
    
    return 0;
}
```

#### 2. Instance Detection
```csharp
AppInstance keyInstance = AppInstance.FindOrRegisterForKey("INotify_SingleInstance_Key");

if (keyInstance.IsCurrent)
{
    // This is the main/first instance
    keyInstance.Activated += OnActivated;
}
else
{
    // Another instance exists, redirect to it
    RedirectActivationTo(args, keyInstance);
}
```

#### 3. Window Activation
```csharp
private static void OnActivated(object sender, AppActivationArguments args)
{
    // Get the current app instance to bring window to foreground
    var currentApp = Microsoft.UI.Xaml.Application.Current as App;
    currentApp?.ShowMainWindow();
}
```

### Project Configuration

#### INotify.csproj
```xml
<PropertyGroup>
    <!-- Disable auto-generated Program code for custom single instance implementation -->
    <DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>
</PropertyGroup>
```

#### app.manifest
```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
        <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
        <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
    </windowsSettings>
</application>

<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
        <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
            <requestedExecutionLevel level="asInvoker" uiAccess="false" />
        </requestedPrivileges>
    </security>
</trustInfo>
```

## User Experience

### Expected Behavior

1. **First Launch**: Application starts normally
2. **Subsequent Launches**: 
   - New instance detects existing instance
   - Activation is redirected to existing instance
   - Existing window comes to foreground
   - New instance exits gracefully

### Supported Scenarios

- ? **Desktop shortcut double-click**
- ? **Start menu launch**
- ? **File association activation**
- ? **Protocol activation**
- ? **Toast notification activation**
- ? **Command line launch**

## Testing Guide

### Manual Testing

1. **Build and Deploy**
   ```bash
   # Build the application
   dotnet build --configuration Release
   
   # Deploy to test single instance (debugging prevents multiple instances)
   Right-click project ? Deploy
   ```

2. **Test Single Instance Behavior**
   ```
   1. Launch INotify from Start menu
   2. Launch INotify again from desktop shortcut
   3. Verify: Second launch activates existing window, doesn't create new instance
   ```

3. **Test Toast Activation**
   ```
   1. Launch INotify
   2. Generate test toast notification
   3. Click "View in INotify" button
   4. Verify: Existing window comes to foreground
   ```

4. **Process Validation**
   ```
   1. Open Task Manager
   2. Launch INotify multiple times
   3. Verify: Only one INotify.exe process exists
   ```

### Automated Testing

```csharp
// Test helper method
public static void ValidateSingleInstance()
{
    var currentProcess = Process.GetCurrentProcess();
    var allProcesses = Process.GetProcessesByName(currentProcess.ProcessName);
    
    Debug.WriteLine($"Found {allProcesses.Length} INotify process(es)");
    
    foreach (var process in allProcesses)
    {
        var isCurrent = process.Id == currentProcess.Id ? " (Current)" : "";
        Debug.WriteLine($"PID: {process.Id}, Started: {process.StartTime:HH:mm:ss}{isCurrent}");
    }
    
    if (allProcesses.Length == 1)
    {
        Debug.WriteLine("? Single instance validation PASSED");
    }
    else
    {
        Debug.WriteLine($"?? Multiple instances detected: {allProcesses.Length}");
    }
}
```

## Integration with Existing Features

### Toast Notifications
- **Maintained**: Existing toast notification functionality
- **Enhanced**: Toast activation properly brings window to foreground
- **Compatible**: Works with OTP copy and other toast features

### Tray Management
- **Maintained**: System tray functionality works normally
- **Enhanced**: Show/hide from tray respects single instance
- **Compatible**: Exit from tray properly closes the single instance

### Background Services
- **Maintained**: All background services (notification monitoring, etc.)
- **Enhanced**: Services start only in the main instance
- **Compatible**: No conflicts with single instance management

## Troubleshooting

### Common Issues

1. **Multiple Instances Still Appearing**
   - **Cause**: Running from debugger (Visual Studio)
   - **Solution**: Deploy and test outside debugger

2. **Window Not Coming to Foreground**
   - **Cause**: Windows focus policies
   - **Solution**: Check Windows settings for foreground window policies

3. **Activation Not Working**
   - **Cause**: Process permissions or Windows App SDK version
   - **Solution**: Verify Windows App SDK 1.6+ and run as appropriate user

### Debug Information

The implementation includes comprehensive logging:

```csharp
Logger.Info("INotify starting with single instance check");
Logger.Info("This is the main instance, setting up activation handler");
Logger.Info("Redirected to existing INotify instance, exiting");
Logger.Info("Main instance activated with kind: {kind}");
```

Check application logs for detailed single instance behavior information.

## Compliance with Microsoft Standards

### Windows App SDK Guidelines
- ? Uses official `Microsoft.Windows.AppLifecycle` APIs
- ? Follows documented patterns from Microsoft Learn
- ? Compatible with Windows 10/11 requirements
- ? Supports all activation types (file, protocol, toast)

### Best Practices
- ? Proper COM initialization
- ? Synchronization context handling
- ? Resource cleanup and disposal
- ? Error handling and logging
- ? Process lifecycle management

## Migration from Legacy Implementation

### What Changed
- **Removed**: Custom mutex-based SingleInstanceManager
- **Removed**: Named pipe communication
- **Added**: Microsoft's official AppInstance APIs
- **Enhanced**: Better activation handling
- **Improved**: More reliable foreground activation

### Backward Compatibility
- **API**: No changes to public App methods
- **Functionality**: All existing features preserved
- **Performance**: Improved startup and activation times
- **Reliability**: More robust single instance detection

## Summary

The INotify application now implements single instance behavior using Microsoft's official Windows App SDK pattern, ensuring:

- **Reliability**: Official APIs provide robust single instance detection
- **Compatibility**: Works with all Windows 10/11 versions and activation scenarios
- **Performance**: Optimized startup and activation processes
- **Maintainability**: Standard implementation following Microsoft guidelines
- **Future-proof**: Uses current Windows App SDK APIs

This implementation ensures INotify maintains single instance behavior while following modern Windows development best practices.