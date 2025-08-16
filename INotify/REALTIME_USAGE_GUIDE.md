# Real-Time Notification Usage Guide

## Overview

This guide demonstrates how to use the enhanced real-time notification capabilities in both `NotificationListControl` and `KToastListControl`. These controls automatically receive and display new notifications as they arrive, while maintaining proper filtering and avoiding duplicates.

## Control Types and Use Cases

### 1. NotificationListControl
**Purpose**: Displays notifications filtered by priority or space categories
**Used in**: Priority boards (High/Medium/Low) and Space boards (Space1/Space2/Space3)
**File**: `INotify/View/NotificationListControl.xaml.cs`

### 2. KToastListControl  
**Purpose**: Displays all notifications with advanced filtering capabilities
**Used in**: "All Notifications" view
**File**: `INotify/KToastView/View/KToastListControl.xaml.cs`

## Real-Time Features

### NotificationListControl Features
- ? **Priority-based filtering**: Only shows notifications from apps with matching priority
- ? **Space-based filtering**: Only shows notifications from apps in the specified space
- ? **Cached filtering**: Uses `NotificationFilterCacheService` for fast lookups
- ? **Multiple instances**: Each priority/space column operates independently
- ? **Duplicate prevention**: Prevents same notification appearing in multiple views
- ? **Auto-subscription**: Subscribes to real-time events when visible, unsubscribes when hidden

### KToastListControl Features
- ? **Search filtering**: Filters new notifications by search keywords
- ? **App filtering**: Shows only notifications from selected app
- ? **Date filtering**: Supports specific date or date range filtering
- ? **Dynamic filter updates**: Re-evaluates existing notifications when filters change
- ? **Filter persistence**: Maintains filter settings during real-time updates
- ? **Comprehensive UI**: Includes search box, filter panel, and sorting options

## Usage Examples

### Example 1: Priority-Based Real-Time Filtering

```xml
<!-- MainWindow.xaml - Priority Board -->
<Grid x:Name="PriorityBoardView">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- High Priority Column -->
    <view1:NotificationListControl
        x:Name="HighPriorityListControl"
        Grid.Column="0"
        CurrentTargetType="Priority"
        SelectionTargetId="High" />

    <!-- Medium Priority Column -->
    <view1:NotificationListControl
        x:Name="MediumPriorityListControl"
        Grid.Column="1"
        CurrentTargetType="Priority"
        SelectionTargetId="Medium" />

    <!-- Low Priority Column -->
    <view1:NotificationListControl
        x:Name="LowPriorityListControl"
        Grid.Column="2"
        CurrentTargetType="Priority"
        SelectionTargetId="Low" />
</Grid>
```

**How it works**:
1. When a new notification arrives, all three controls receive the event
2. Each control checks if the notification's app has the matching priority
3. Only the control with the matching priority displays the notification
4. Apps without custom priority assignments don't appear in any priority column

### Example 2: Space-Based Real-Time Filtering

```xml
<!-- MainWindow.xaml - Space Board -->
<Grid x:Name="SpaceBoardView">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- Space 1 Column -->
    <view1:NotificationListControl
        x:Name="Space1ListControl"
        Grid.Column="0"
        CurrentTargetType="Space"
        SelectionTargetId="Space1" />

    <!-- Space 2 Column -->
    <view1:NotificationListControl
        x:Name="Space2ListControl"
        Grid.Column="1"
        CurrentTargetType="Space"
        SelectionTargetId="Space2" />

    <!-- Space 3 Column -->
    <view1:NotificationListControl
        x:Name="Space3ListControl"
        Grid.Column="2"
        CurrentTargetType="Space"
        SelectionTargetId="Space3" />
</Grid>
```

**How it works**:
1. When a new notification arrives, all three controls receive the event
2. Each control checks if the notification's app is assigned to its space
3. Only the control for the matching space displays the notification
4. Apps not assigned to any space don't appear in space columns

### Example 3: All Notifications with Advanced Filtering

```xml
<!-- MainWindow.xaml - All Notifications View -->
<Grid x:Name="AllNotificationsView">
    <view:KToastListControl x:Name="KToastListViewControl" />
</Grid>
```

**How it works**:
1. Displays ALL notifications by default
2. Users can apply filters which affect both existing and new notifications
3. Real-time notifications are filtered based on current filter settings
4. Changing filters re-evaluates all displayed notifications

## Real-Time Filtering Scenarios

### Scenario 1: Search Filter Active
```
User types "outlook" in search box
? Only notifications containing "outlook" in title, message, or app name are shown
? New notifications are automatically filtered for "outlook"
? Existing notifications not matching "outlook" are hidden
```

### Scenario 2: App Filter Active
```
User selects "Microsoft Teams" from app dropdown
? Only notifications from Microsoft Teams are shown
? New notifications from other apps are ignored
? Existing notifications from other apps are hidden
```

### Scenario 3: Date Filter Active
```
User sets date range: Jan 1, 2024 - Jan 31, 2024
? Only notifications from January 2024 are shown
? New notifications outside this date range are ignored
? Existing notifications outside date range are hidden
```

### Scenario 4: Multiple Filters Active
```
User has active: Search="meeting", App="Microsoft Teams", Date="Today"
? Only shows Teams notifications from today containing "meeting"
? New notifications must match ALL criteria to be displayed
? Very specific filtering for focused viewing
```

## Performance Considerations

### Caching (NotificationListControl)
```csharp
// Priority and space mappings are cached for 5 minutes
// Cache automatically refreshes when needed
// Provides fast O(1) lookup for filtering decisions
```

### Efficient Filtering (KToastListControl)
```csharp
// Early returns for non-matching notifications
// Case-insensitive string matching using ToLowerInvariant()
// Date comparisons only performed when date filters are active
```

### Memory Management
```csharp
// Automatic subscription/unsubscription based on visibility
// Event handlers properly cleaned up on control unload
// No memory leaks from long-running event subscriptions
```

## Debugging Real-Time Notifications

### Debug Output Examples

```
// NotificationListControl
NotificationListControl subscribed to real-time notifications for Priority:High
Added real-time notification to notification view: Meeting reminder
Duplicate notification detected, skipping: msg-12345

// KToastListControl  
KToastListControl subscribed to real-time notifications for All Notifications view
Real-time notification filtered out: System Update Available
Added real-time notification to KToastListControl: New email received
Filtered out 3 existing notifications based on new filter criteria
```

### Troubleshooting Tips

1. **Notifications not appearing in priority columns**:
   - Check if the app has a priority assignment in the database
   - Verify cache is working by checking debug output
   - Ensure the priority level matches exactly ("High", "Medium", "Low")

2. **Notifications not appearing in space columns**:
   - Check if the app is assigned to the space in KSpaceMapper table
   - Verify space ID matches exactly ("Space1", "Space2", "Space3")
   - Check database connections and query results

3. **Filters not working in All Notifications**:
   - Check if filter criteria are being applied correctly
   - Verify string matching logic for search keywords
   - Ensure date comparisons account for time zones

4. **Duplicate notifications**:
   - Check NotificationId uniqueness in the database
   - Verify duplicate prevention logic is working
   - Look for timing issues in rapid notification arrival

## Testing Real-Time Functionality

### Test Cases

1. **Basic Real-Time Display**:
   - Send test notification ? Verify appears in correct view(s)
   - Send notification from priority app ? Verify appears in priority column
   - Send notification from space app ? Verify appears in space column

2. **Filter Testing**:
   - Apply search filter ? Send notification ? Verify filtering works
   - Change app filter ? Verify existing notifications are re-evaluated
   - Set date range ? Send old/new notifications ? Verify date filtering

3. **Performance Testing**:
   - Send rapid notifications ? Verify no UI blocking
   - Apply complex filters ? Verify performance remains good
   - Test with large notification history ? Verify memory usage

4. **Edge Cases**:
   - Send notification while control is hidden ? Verify no processing
   - Change filters rapidly ? Verify no race conditions
   - Send malformed notifications ? Verify error handling

## Configuration Requirements

### Database Tables
- `KCustomPriorityApp`: Stores app priority assignments
- `KSpaceMapper`: Maps apps to spaces  
- `KToastNotification`: Stores notification data
- `KPackageProfile`: Stores app profile information

### Services Required
- `BackgroundNotificationService`: Listens for system notifications
- `NotificationFilterCacheService`: Provides cached filtering for priorities/spaces
- `KToastDIServiceProvider`: Dependency injection container

### Event System
- `NotificationEventInokerUtil.NotificationReceived`: Global notification event
- All controls subscribe to this event for real-time updates

## Best Practices

1. **Filter Design**: Keep filters simple and intuitive for users
2. **Performance**: Use caching for frequently accessed filter data
3. **User Experience**: Provide visual feedback when filters are active
4. **Error Handling**: Gracefully handle filter failures without crashing
5. **Testing**: Test all filter combinations with real-time notifications
6. **Documentation**: Keep filter behavior clearly documented for users

This real-time notification system provides a robust, performant solution for managing notifications across multiple views with different filtering requirements.