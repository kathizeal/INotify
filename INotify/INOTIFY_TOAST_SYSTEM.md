# INotify Toast Notification System

## Overview

The INotify app now creates its own toast notifications when it receives notifications from categorized apps (those assigned to priority levels or spaces). This ensures that important notifications can still be seen even when Windows 11 is in Do Not Disturb (DND) mode, provided that the INotify app itself is marked as a priority app in Windows notification settings.

## Key Features

### ?? **Smart Categorization**
- Only creates INotify toasts for apps that are **categorized** (have priority or space assignments)
- Non-categorized apps are processed normally but don't trigger INotify toasts
- Prevents notification spam by filtering to only important, user-configured apps

### ?? **Enhanced Toast Content**
- **Title**: Shows categorization tags and original app name  
  - Example: `[?? High Priority | ??? Space 1] WhatsApp`
- **Content**: Displays the original notification title
- **Details**: Shows category information like "Priority: High Priority • Spaces: Space 1"
- **Icon**: Uses the original app's icon when available

### ?? **DND Mode Support**
- INotify toasts can appear even when Windows is in DND mode
- Requires marking INotify as a priority app in Windows 11 notification settings
- Bypasses system-level notification blocking for categorized apps

### ?? **Future Sound Support**
- Framework ready for custom notification sounds per category
- Currently uses default notification sound
- Extensible design for priority-based and space-based audio alerts

## How It Works

### Flow Diagram
```
1. External App (e.g., WhatsApp) ? Sends Notification
2. Windows System ? Captures via UserNotificationListener  
3. BackgroundNotificationService ? Processes notification
4. Check: Is app categorized? (Priority or Space assigned)
   ?? YES ? Create INotify toast with categorization info
   ?? NO ? Store in database only, no toast
5. INotify Toast ? Displayed in Windows notification center
6. User clicks toast ? Brings INotify app to foreground
```

### Example Scenarios

#### Scenario 1: WhatsApp (Medium Priority + Space1)
```
Original: "John: Hey, are we still meeting today?"
INotify Toast:
???????????????????????????????????????????
? [?? Medium Priority | ??? Space 1] WhatsApp ?
? Hey, are we still meeting today?        ?
? Priority: Medium Priority • Spaces: Space 1 ?
? [View in INotify] [Dismiss]             ?
???????????????????????????????????????????
```

#### Scenario 2: Microsoft Teams (High Priority Only)
```
Original: "Meeting starting in 5 minutes"
INotify Toast:
???????????????????????????????????????????
? [?? High Priority] Microsoft Teams      ?
? Meeting starting in 5 minutes           ?
? Priority: High Priority                 ?
? [View in INotify] [Dismiss]             ?
???????????????????????????????????????????
```

#### Scenario 3: Chrome (No Categorization)
```
Original: "Download completed"
Result: No INotify toast created (stored in database only)
```

## Implementation Details

### Core Components

#### 1. INotifyToastService
**File**: `INotify/Services/INotifyToastService.cs`

**Key Features**:
- Singleton service for creating categorized toast notifications
- Checks app categorization using `NotificationFilterCacheService`
- Builds enhanced toast content with category information
- Prevents infinite loops by filtering out INotify's own notifications

**Main Methods**:
- `ProcessNotificationAsync()`: Main entry point for processing notifications
- `GetAppPriorityInfoAsync()`: Retrieves priority assignment for an app
- `GetAppSpaceInfoAsync()`: Retrieves space assignments for an app
- `CreateCategorizedToastAsync()`: Creates and displays the toast

#### 2. BackgroundNotificationService (Enhanced)
**File**: `INotify/Services/BackgroundNotificationService.cs`

**New Features**:
- Integrated with `INotifyToastService` 
- Loop prevention by filtering INotify notifications
- Calls `ProcessForINotifyToast()` for each received notification

**Enhanced Process Flow**:
```csharp
private async Task ProcessNotification(UserNotification notification)
{
    // ... existing processing ...
    
    // Update ViewModel (stores in database)
    _viewModel?.UpdateKToastNotification(kToastViewData);
    
    // Raise event for UI updates
    NotificationEventInokerUtil.NotifyNotificationListened(new NotificationReceivedEventArgs(kToastViewData));
    
    // NEW: Process for INotify toast creation
    await ProcessForINotifyToast(kToastViewData);
}
```

#### 3. ToastActivationService
**File**: `INotify/Services/ToastActivationService.cs`

**Purpose**: Handles user interactions with INotify toast notifications

**Actions Supported**:
- **"View in INotify"**: Brings main window to foreground
- **"Dismiss"**: Dismisses the toast (future: mark as read)

### Toast XML Template

The service uses a customizable XML template for creating rich toast notifications:

```xml
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>[?? High Priority] WhatsApp</text>
            <text>Hey, are we still meeting today?</text>
            <text>Priority: High Priority • Spaces: Space 1</text>
            <image placement='appLogoOverride' hint-crop='circle' src='app-icon.png'/>
        </binding>
    </visual>
    <audio src='ms-winsoundevent:Notification.Default' loop='false'/>
    <actions>
        <action content='View in INotify' arguments='action=view&notificationId=123' activationType='foreground'/>
        <action content='Dismiss' arguments='action=dismiss' activationType='background'/>
    </actions>
</toast>
```

## Configuration & Setup

### Windows 11 DND Configuration

For INotify toasts to appear during DND mode:

1. **Open Windows Settings** ? **System** ? **Notifications**
2. **Click "Set priority notifications"**
3. **Find "INotify"** in the app list
4. **Toggle ON** to mark as priority app
5. **Configure notification settings** as desired

### App Categorization

Apps must be categorized in INotify to trigger toast creation:

**Priority Assignment**:
- Navigate to Priority board in INotify
- Click "+ Add Apps" in High/Medium/Low columns
- Select apps to assign priority levels

**Space Assignment**:  
- Navigate to Space board in INotify
- Click "+ Add Apps" in Space 1/2/3 columns
- Select apps to assign to spaces

### Database Dependencies

The system relies on these database tables:
- `KCustomPriorityApp`: Stores app priority assignments
- `KSpaceMapper`: Maps apps to spaces
- `KToastNotification`: Stores all received notifications
- `KPackageProfile`: Stores app profile information

## Visual Design

### Priority Icons
- ?? **High Priority** (Red circle)
- ?? **Medium Priority** (Yellow circle)  
- ?? **Low Priority** (Green circle)

### Space Icons
- ??? **All Spaces** (Label/tag icon)

### Toast Layout
```
???????????????????????????????????????????
? [Icons] Category Tags + App Name        ? ? Title with categorization
? Original notification content           ? ? Original message
? Category details text                   ? ? Detailed category info
? [Action Button 1] [Action Button 2]    ? ? User actions
???????????????????????????????????????????
```

## Testing & Debugging

### Debug Output

The system provides comprehensive debug logging:

```
// Service initialization
INotifyToastService initialized successfully

// Categorization checks
App WhatsApp not categorized, skipping toast creation
Created categorized toast for Microsoft Teams

// Loop prevention
Skipping INotify notification to prevent loop: INotify

// Toast creation
Enhanced toast content: [?? High Priority] Microsoft Teams
Toast activation received with arguments: action=view&notificationId=123
```

### Test Scenarios

1. **Basic Categorization Test**:
   - Assign WhatsApp to Medium Priority
   - Send WhatsApp notification ? Verify INotify toast appears

2. **DND Mode Test**:
   - Enable Windows DND mode
   - Mark INotify as priority app in Windows settings
   - Send notification from categorized app ? Verify toast appears

3. **Non-Categorized Test**:
   - Send notification from uncategorized app ? Verify no INotify toast

4. **Multiple Categories Test**:
   - Assign app to both Priority and Space
   - Verify toast shows both categorizations

5. **Loop Prevention Test**:
   - Trigger INotify notification ? Verify no infinite loop

## Future Enhancements

### ?? Custom Sounds
```csharp
// Future implementation
private string GetNotificationSound(PriorityInfo priorityInfo, SpaceInfo spaceInfo)
{
    return priorityInfo.Priority switch
    {
        Priority.High => "ms-appx:///Assets/Sounds/HighPriority.wav",
        Priority.Medium => "ms-appx:///Assets/Sounds/MediumPriority.wav", 
        Priority.Low => "ms-appx:///Assets/Sounds/LowPriority.wav",
        _ => "ms-winsoundevent:Notification.Default"
    };
}
```

### ?? Smart Routing
- Navigate directly to relevant priority/space view on toast activation
- Filter to specific app when viewing notifications
- Deep linking to exact notification

### ?? Visual Enhancements
- Custom toast templates per category
- Rich images and progressive disclosure
- Theme-aware color schemes

### ?? Analytics
- Track toast effectiveness and user engagement
- Category-based notification frequency analysis
- User interaction patterns

## Error Handling

The system includes comprehensive error handling:
- **Service initialization failures**: Logs errors but continues operation
- **Categorization lookup failures**: Defaults to no categorization
- **Toast creation failures**: Logs errors, doesn't crash background service
- **Database access issues**: Graceful degradation to basic functionality

## Performance Considerations

- **Caching**: Uses `NotificationFilterCacheService` for fast categorization lookups
- **Async Processing**: All toast creation is asynchronous to avoid blocking
- **Loop Prevention**: Efficient filtering to prevent processing INotify's own toasts
- **Memory Management**: Proper disposal of resources and event handlers

This toast notification system transforms INotify into a powerful notification management tool that can surface important notifications even during Do Not Disturb periods, while maintaining user control through smart categorization.