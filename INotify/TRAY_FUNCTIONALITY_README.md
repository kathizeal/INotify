# INotify - System Tray Background Functionality

## Overview

INotify now supports running in the background as a system tray application. When you close the main window, the application will continue to monitor notifications in the background and display a tray icon in the system notification area.

## Features

### Background Notification Monitoring
- The application continues to listen for system notifications even when the main window is hidden
- All notifications are processed and stored in the database
- Background service remains active until the application is completely exited

### System Tray Integration
- System tray icon appears when the application is running
- Notification balloons inform users when the app is minimized to tray
- Tray icon provides access to basic functionality (can be expanded in future)

### Keyboard Shortcuts
- **Ctrl + H**: Hide the main window to system tray
- **Escape**: Hide the main window to system tray
- Works from anywhere in the application

### Single Instance Support
- Only one instance of INotify can run at a time
- Attempting to start a second instance will bring the existing instance to foreground

## How It Works

### Application Architecture
1. **App.xaml.cs**: Manages application lifecycle, tray functionality, and background services
2. **BackgroundNotificationService**: Handles notification listening independently of the UI
3. **TrayManager**: Manages system tray icon and user interactions
4. **MainWindow**: Provides the main UI and can be hidden/shown as needed

### Background Processing
When the main window is closed:
1. The window is hidden (not actually closed)
2. Background notification service continues running
3. System tray icon remains visible
4. Notifications continue to be captured and processed
5. All captured notifications are stored in the database for later viewing

### Restoring the Window
The main window can be restored by:
1. Left-clicking the system tray icon (future enhancement)
2. Starting another instance of the application (single instance will bring existing to foreground)
3. Using the tray context menu (future enhancement)

### Complete Exit
To completely exit the application:
- Currently requires using Task Manager or implementing an Exit option in tray context menu

## Implementation Details

### Dependencies
- **H.NotifyIcon.WinUI**: Provides system tray functionality for WinUI 3 applications
- **Windows.UI.Notifications.Management**: For accessing system notifications

### Key Components

#### BackgroundNotificationService
```csharp
// Manages notification listening independently of UI
public class BackgroundNotificationService : IDisposable
{
    // Continues monitoring notifications even when UI is hidden
    // Processes notifications and updates the database
    // Raises events for UI updates when window is visible
}
```

#### TrayManager
```csharp
// Manages system tray functionality
public class TrayManager : IDisposable
{
    // Creates and manages tray icon
    // Shows notification balloons
    // Handles user interactions with tray icon (basic implementation)
}
```

#### App Lifecycle Management
```csharp
// In App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // Initialize background services first
    // Create main window
    // Services continue running independently
}
```

#### Keyboard Shortcuts
```csharp
// In MainWindow.xaml.cs
public void HandleKeyboardShortcut(object sender, KeyRoutedEventArgs e)
{
    // Handles Ctrl+H and Escape to hide window
    // Called from XAML PreviewKeyDown event
}
```

### XAML Integration
```xaml
<!-- In MainWindow.xaml -->
<Grid PreviewKeyDown="HandleKeyboardShortcut">
    <!-- Main application content -->
</Grid>
```

## User Experience

### Normal Operation
1. Application starts with main window visible
2. User can interact with the full UI normally
3. Background service starts monitoring notifications
4. Tray icon is visible in system notification area

### Background Mode
1. User closes main window (or presses Ctrl+H/Escape)
2. Window hides to system tray
3. Tray balloon notification confirms the action
4. Application continues monitoring notifications in background
5. Tray icon remains visible for quick access

### Restoration
1. User clicks tray icon (when enhanced)
2. Main window is restored and brought to foreground
3. All notifications collected during background operation are visible
4. Full UI functionality is immediately available

## Technical Benefits

1. **Continuous Monitoring**: Notifications are never missed, even when UI is not visible
2. **Resource Efficient**: Background operation uses minimal system resources
3. **User Friendly**: Easy to hide/restore with simple interactions
4. **Data Integrity**: All notifications are captured and stored regardless of UI state
5. **Quick Access**: System tray provides immediate access to restore the application

## Current Limitations & Future Enhancements

### Current Limitations
1. **Basic Tray Interaction**: Only shows notification balloons, no context menu yet
2. **No Exit from Tray**: Must use Task Manager to completely exit
3. **No Tray Click Restore**: Left-click doesn't restore window yet

### Future Enhancements
1. **Tray Context Menu**: Right-click menu with Show/Exit options
2. **Notification Previews**: Show recent notifications directly from tray
3. **Quick Settings**: Access DND controls from tray
4. **Startup Integration**: Auto-start with Windows in tray mode
5. **Global Hot Keys**: System-wide hotkeys to show/hide the application
6. **Tray Click Actions**: Configure what happens on tray icon clicks

## Configuration

### Adding NuGet Package
The implementation uses the H.NotifyIcon.WinUI package:
```xml
<PackageReference Include="H.NotifyIcon.WinUI" Version="2.1.4" />
```

### Service Registration
Background services are automatically initialized in App.xaml.cs during application startup.

## Troubleshooting

### Tray Icon Not Appearing
- Ensure H.NotifyIcon.WinUI package is properly installed
- Check that the icon resource path is correct
- Verify Windows notification area settings

### Notifications Not Captured in Background
- Confirm notification access permissions are granted
- Check Windows notification settings
- Verify UserNotificationListener access status

### Window Not Responding to Keyboard Shortcuts
- Ensure the main Grid has focus
- Check that PreviewKeyDown event is properly wired in XAML
- Verify keyboard shortcut implementation in code-behind

## Testing the Implementation

1. **Start Application**: Verify main window opens and tray icon appears
2. **Hide to Tray**: Press Ctrl+H or Escape, confirm window hides and balloon shows
3. **Background Monitoring**: Send test notifications, verify they're captured
4. **Single Instance**: Try starting another instance, confirm it brings existing to foreground
5. **Data Persistence**: Restore window and verify notifications collected during background mode are visible