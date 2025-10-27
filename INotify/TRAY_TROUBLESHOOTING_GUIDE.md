# System Tray Icon Troubleshooting Guide

## Quick Diagnostics

If you're not seeing the INotify icon in your system tray, follow these steps:

### 1. **Check Windows Notification Area Settings**

**Step 1: Show Hidden Icons**
- Look for the "Show hidden icons" arrow (^) in your system tray
- Click it to expand the hidden icons area
- Your INotify icon might be in the hidden area

**Step 2: Notification Area Settings**
1. Right-click on the taskbar
2. Select "Taskbar settings"
3. Scroll down to "Notification area"
4. Click "Turn system icons on or off"
5. Ensure "Notification area" is enabled

**Step 3: Select Which Icons Appear**
1. In Taskbar settings, click "Select which icons appear on the taskbar"
2. Look for "INotify" in the list
3. If found, toggle it to "On"
4. If not found, the app may not be properly registering

### 2. **Check Application Status**

**Verify App is Running:**
1. Open Task Manager (Ctrl+Shift+Esc)
2. Go to "Processes" tab
3. Look for "INotify.exe" or "INotify"
4. If not found, the app isn't running

**Check Debug Output:**
1. If running from Visual Studio, check the Debug Output window
2. Look for these messages:
   - "Tray icon initialized successfully"
   - "Tray context menu setup completed"
   - "INotify Started" balloon notification

### 3. **Manual Testing**

**Test Balloon Notifications:**
1. The app should show a welcome balloon when it starts
2. If you see the balloon but no icon, the tray is working but icon has issues

**Try System Tray Area:**
1. Look in the bottom-right corner of your screen
2. Check both the main tray area and the hidden area (click ^ arrow)
3. Try hovering over potential icon areas to see tooltips

### 4. **Windows-Specific Issues**

**Windows 11 Considerations:**
- Notification area behavior changed in Windows 11
- Icons may be automatically hidden by default
- Check "Widgets" area if enabled

**Windows 10 Considerations:**
- Ensure Windows 10 version 1903 or later
- Some older versions have notification area bugs

### 5. **Application-Specific Checks**

**H.NotifyIcon Library Issues:**
1. Verify the NuGet package is properly installed
2. Check for any security software blocking tray access
3. Try running as Administrator (temporarily for testing)

**Icon Loading Issues:**
The app tries multiple icon sources in order:
1. Application executable icon
2. Programmatically created notification bell icon
3. Windows system application icon

If all fail, no icon will be visible.

## Advanced Diagnostics

### Enable Detailed Logging

Add this to your app for debugging:

```csharp
// In TrayManager.Initialize()
Debug.WriteLine($"TaskbarIcon created: {_trayIcon != null}");
Debug.WriteLine($"Process path: {Environment.ProcessPath}");
Debug.WriteLine($"Current user: {Environment.UserName}");
```

### Manual Icon Test

Try this simplified tray test:

```csharp
// Minimal tray test
var testIcon = new TaskbarIcon
{
    Icon = SystemIcons.Information,
    ToolTipText = "Test Icon",
    Visible = true
};
testIcon.ForceCreate(false);
```

### Registry Check

Check if tray icons are being blocked:
1. Open Registry Editor (regedit)
2. Navigate to: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`
3. Look for `NoTrayItemsDisplay` - if present and set to 1, it blocks tray icons

## Common Solutions

### Solution 1: Restart Explorer.exe
1. Open Task Manager
2. Find "Windows Explorer" in Processes
3. Right-click and select "Restart"
4. This resets the notification area

### Solution 2: Clear Notification Area Cache
1. Close INotify completely
2. Open Command Prompt as Administrator
3. Run: `ie4uinit.exe -ClearIconCache`
4. Restart INotify

### Solution 3: Check Group Policy
1. Open Group Policy Editor (gpedit.msc)
2. Navigate to: User Configuration > Administrative Templates > Start Menu and Taskbar
3. Ensure "Hide the notification area" is not enabled

### Solution 4: Recreate Tray Icon
If the icon appears briefly then disappears:
1. The icon may be created too early in app lifecycle
2. Try initializing tray after the main window is shown
3. Add a delay before tray initialization

## Expected Behavior

When working correctly, you should see:
1. **Application Start**: Welcome balloon notification appears
2. **Tray Icon**: Small icon visible in system tray (main area or hidden area)
3. **Tooltip**: Hover shows "INotify - Notification Manager"
4. **Left Click**: Restores/shows main window
5. **Right Click**: Shows context menu with "Show INotify" and "Exit"

## Quick Fix Implementation

If the issue persists, try this emergency fallback:

```csharp
// In TrayManager.Initialize(), add this as last resort:
try 
{
    _trayIcon.Visible = true;
    _trayIcon.Icon = SystemIcons.Application;
    
    // Force Windows to refresh the tray
    _trayIcon.Visible = false;
    System.Threading.Thread.Sleep(100);
    _trayIcon.Visible = true;
}
catch (Exception ex)
{
    Debug.WriteLine($"Emergency tray fix failed: {ex.Message}");
}
```

## Contact Information

If none of these solutions work:
1. Check Windows Event Viewer for any related errors
2. Verify antivirus software isn't blocking tray access
3. Try running the app on a different user account
4. Consider using an alternative tray library if H.NotifyIcon continues to have issues

## Success Indicators

You'll know it's working when:
- ? You see the welcome balloon notification
- ? Icon appears in system tray (may be in hidden area)
- ? Tooltip shows on hover
- ? Click handlers work (left click shows window)
- ? Right-click menu appears and functions