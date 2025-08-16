# Real-Time Notification Support Implementation

## Overview

Both `NotificationListControl` and `KToastListControl` have been enhanced to support real-time notification updates. This allows multiple instances of the controls to automatically receive and display new notifications as they arrive, while maintaining proper filtering and preventing duplicate notifications.

## Key Features

### 1. Real-Time Notification Listening
- Each control instance subscribes to the global `NotificationEventInokerUtil.NotificationReceived` event
- Automatically processes new notifications as they arrive from the `BackgroundNotificationService`
- Manages subscription lifecycle based on control visibility and loading state

### 2. Intelligent Filtering
- **NotificationListControl**: Uses cached priority and space mappings for efficient real-time filtering
- **KToastListControl**: Respects active filter settings (search keywords, app filters, date filters)
- Only processes notifications relevant to the current view configuration

### 3. Duplicate Prevention
- Checks for existing notifications before adding new ones
- Prevents duplicate entries in both notification list view and package view
- Uses notification ID for accurate duplicate detection

### 4. Performance Optimization
- Implements `NotificationFilterCacheService` for fast priority/space lookups (NotificationListControl)
- Efficient insertion algorithms for maintaining chronological order
- Filter-aware real-time processing (KToastListControl)

## Architecture

### Core Components

#### 1. NotificationListControl (Enhanced)
**File**: `INotify/View/NotificationListControl.xaml.cs`

**Purpose**: Handles priority-based and space-based notification views

**Key Features**:
- Priority and space filtering using cached mappings
- Multiple instances for different priority levels and spaces
- Automatic subscription/unsubscription based on control lifecycle

#### 2. KToastListControl (Enhanced)
**File**: `INotify/KToastView/View/KToastListControl.xaml.cs`

**Purpose**: Handles the "All Notifications" view with advanced filtering capabilities

**Key Features**:
- **Filter-Aware Real-Time Updates**: New notifications are filtered based on current filter settings
- **Search Integration**: Real-time notifications respect search keywords
- **App Filtering**: Only shows notifications from selected apps when app filter is active
- **Date Filtering**: Respects specific date or date range filters
- **Dynamic Filter Updates**: When filters change, existing real-time notifications are re-evaluated

**Key Methods**:
- `ShouldNotificationBeDisplayed()`: Determines if notification passes current filters
- `FilterExistingNotifications()`: Re-evaluates existing notifications when filters change
- `HandleRealTimeNotificationOnUIThread()`: Processes filtered notifications on UI thread

#### 3. NotificationFilterCacheService
**File**: `INotify/Services/NotificationFilterCacheService.cs`

**Purpose**: Provides fast, cached lookups for notification filtering (used by NotificationListControl)

## Implementation Details

### Event Flow

1. **New Notification Arrives**: `BackgroundNotificationService` processes system notification
2. **Event Raised**: `NotificationEventInokerUtil.NotifyNotificationListened()` is called
3. **Event Received**: All subscribed control instances receive the event
4. **Filtering Applied**: Each control applies its specific filtering logic
5. **UI Update**: Relevant notifications are added to the appropriate view on the UI thread
6. **Duplicate Check**: Existing notifications are checked to prevent duplicates

### Filtering Logic

#### NotificationListControl (Priority/Space Based)
```csharp
// Check if package has the required priority level or belongs to space
bool isRelevant = CurrentTargetType switch
{
    SelectionTargetType.Priority => _cacheService.IsPackageInPriorityCategory(packageFamilyName, priorityLevel),
    SelectionTargetType.Space => _cacheService.IsPackageInSpace(packageFamilyName, spaceId),
    _ => false
};
```

#### KToastListControl (Filter Based)
```csharp
// Check search keyword filter
if (!string.IsNullOrEmpty(_VM.SearchKeyword))
{
    var searchTerm = _VM.SearchKeyword.ToLowerInvariant();
    var matches = title.Contains(searchTerm) || message.Contains(searchTerm) || appName.Contains(searchTerm);
    if (!matches) return false;
}

// Check app filter
if (!string.IsNullOrEmpty(_VM.SelectedAppFilter))
{
    if (notification.ToastPackageProfile.PackageFamilyName != _VM.SelectedAppFilter)
        return false;
}

// Check date filters (specific date or date range)
if (_VM.SelectedDate.HasValue && notificationDate.Date != _VM.SelectedDate.Value.Date)
    return false;
```

### Filter Integration Features

#### Dynamic Filter Updates (KToastListControl)
When users change filter settings:
1. New real-time notifications are automatically filtered based on new criteria
2. Existing notifications in the list are re-evaluated against new filters
3. Notifications that no longer match are removed from the display
4. Count displays are updated accordingly

#### Real-Time Search
- As users type in the search box, new incoming notifications are filtered in real-time
- Search applies to notification title, message content, and app name
- Case-insensitive matching for better user experience

#### App-Specific Filtering
- When an app is selected in the filter dropdown, only notifications from that app appear in real-time
- Supports "All Apps" option to show notifications from all sources

#### Date-Based Filtering
- **Specific Date**: Only shows notifications from the selected date
- **Date Range**: Shows notifications within the specified date range
- **Mutual Exclusion**: Specific date and date range filters are mutually exclusive

### Duplicate Prevention

Both controls implement comprehensive duplicate checking:

```csharp
var existingNotification = _VM.KToastNotifications.FirstOrDefault(n => 
    n.NotificationData.NotificationId == notification.NotificationData.NotificationId);

if (existingNotification != null)
{
    // Skip duplicate
    return;
}
```

### Chronological Insertion

Notifications are inserted in chronological order (most recent first):

```csharp
var insertIndex = 0;
for (int i = 0; i < _VM.KToastNotifications.Count; i++)
{
    if (_VM.KToastNotifications[i].NotificationData.CreatedTime < notification.NotificationData.CreatedTime)
    {
        insertIndex = i;
        break;
    }
    insertIndex = i + 1;
}

_VM.KToastNotifications.Insert(insertIndex, notification);
```

## Usage Examples

### Priority-Based Filtering (NotificationListControl)
```xml
<!-- High Priority Column -->
<view1:NotificationListControl
    x:Name="HighPriorityListControl"
    CurrentTargetType="Priority"
    SelectionTargetId="High" />
```

### All Notifications with Filtering (KToastListControl)
```xml
<!-- All Notifications View -->
<view:KToastListControl x:Name="KToastListViewControl" />
```

The `KToastListControl` automatically handles:
- Search keyword filtering
- App-based filtering via dropdown
- Date-based filtering (specific date or range)
- Real-time updates respecting all active filters

## Performance Considerations

### KToastListControl Optimizations
- **Filter Caching**: Filter criteria are cached to avoid repeated string comparisons
- **Efficient String Matching**: Uses `ToLowerInvariant()` and `Contains()` for fast search
- **Selective Updates**: Only processes notifications that pass initial filter checks
- **Batch Operations**: Filter updates process existing notifications in batches

### Memory Management
- Proper subscription/unsubscription lifecycle management
- Event handler cleanup on control unload
- Efficient filter evaluation with early returns

## Error Handling

Both controls include comprehensive error handling:
- Filter evaluation failures default to allowing notifications
- UI thread exceptions are caught and logged
- Subscription failures are handled gracefully
- Database operation failures don't crash the UI

## Testing Scenarios

When testing the enhanced real-time functionality:

1. **Filter Persistence**: Verify that filters persist during real-time updates
2. **Search Integration**: Test that new notifications respect search keywords
3. **App Filtering**: Ensure app-specific filters work with real-time notifications
4. **Date Filtering**: Test both specific date and date range filtering
5. **Filter Changes**: Verify that changing filters re-evaluates existing notifications
6. **Performance**: Monitor performance with rapid notification arrival and complex filters
7. **Duplicate Prevention**: Ensure no duplicate notifications appear
8. **Chronological Order**: Verify notifications maintain proper time-based ordering

## Future Enhancements

1. **Advanced Search**: Support for regex or advanced search operators
2. **Filter Presets**: Allow users to save and load filter configurations
3. **Smart Filtering**: Machine learning-based notification prioritization
4. **Bulk Operations**: Batch filtering and processing for better performance
5. **Filter Analytics**: Track which filters are most commonly used
6. **Real-Time Filter Hints**: Show filter suggestions based on current notifications

## Dependencies

- `INotify.Util.NotificationEventInokerUtil`: Global notification event system
- `INotify.Services.BackgroundNotificationService`: System notification listener
- `INotify.Services.NotificationFilterCacheService`: Priority/space filtering cache
- `INotifyLibrary.Model.Entity.*`: Data models
- `Microsoft.UI.Xaml.*`: WinUI 3 framework components