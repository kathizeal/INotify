# INotify Toast Setup Guide

## Quick Start: Getting INotify Toasts During DND

### Step 1: Configure Windows 11 Priority Notifications
1. Open **Windows Settings** (Win + I)
2. Go to **System** ? **Notifications**
3. Click **"Set priority notifications"**
4. Find **"INotify"** in the list
5. **Toggle it ON** to mark as priority app
6. ? INotify can now show notifications during DND mode

### Step 2: Categorize Your Important Apps
Choose either Priority OR Space categorization (or both):

#### Option A: Priority-Based (Recommended)
1. Open INotify app
2. Navigate to **Priority** view
3. Click **"+ Add Apps"** in desired priority column:
   - **High Priority**: ?? Critical apps (Teams, Outlook, Banking)
   - **Medium Priority**: ?? Important apps (WhatsApp, Telegram)
   - **Low Priority**: ?? Less urgent apps (News, Social media)
4. Select apps and confirm

#### Option B: Space-Based Organization
1. Open INotify app  
2. Navigate to **Space** view
3. Click **"+ Add Apps"** in desired space:
   - **Space 1**: ??? Work apps
   - **Space 2**: ??? Personal apps  
   - **Space 3**: ??? Entertainment apps
4. Select apps and confirm

### Step 3: Test the Setup
1. Enable **Do Not Disturb** mode in Windows
2. Send yourself a notification from a categorized app (e.g., WhatsApp message)
3. ? You should see an INotify toast with category tags like:
   ```
   [?? Medium Priority] WhatsApp
   New message from John
   Priority: Medium Priority
   ```

## What Happens Now?

### ? Categorized Apps (Get INotify Toasts)
- Apps you've assigned to Priority or Space
- Show enhanced notifications with category tags
- Appear even during DND mode (if INotify is marked priority)
- Example: `[?? High Priority] Microsoft Teams`

### ? Non-Categorized Apps (No INotify Toasts)  
- Apps not assigned to any category
- Still monitored and stored in INotify database
- Follow normal Windows notification behavior
- No additional INotify toast created

## Visual Examples

### High Priority App Notification
```
???????????????????????????????????????????
? [?? High Priority] Microsoft Teams      ?
? Meeting starting in 5 minutes           ?
? Priority: High Priority                 ?
? [View in INotify] [Dismiss]             ?
???????????????????????????????????????????
```

### Multiple Categories
```
???????????????????????????????????????????
? [?? Medium Priority | ??? Space 1] WhatsApp ?
? Hey, are we still meeting today?        ?
? Priority: Medium Priority • Spaces: Space 1 ?
? [View in INotify] [Dismiss]             ?
???????????????????????????????????????????
```

## Troubleshooting

### Problem: No INotify toasts appearing during DND
**Solutions:**
1. ? Verify INotify is marked as priority app in Windows Settings
2. ? Ensure apps are properly categorized in INotify
3. ? Check that DND is actually enabled
4. ? Restart INotify app after categorization changes

### Problem: Too many INotify toasts
**Solutions:**
1. ?? Remove apps from categories if they're not truly important
2. ?? Use more selective categorization (fewer apps in High Priority)
3. ?? Consider using Spaces instead of Priority for better organization

### Problem: Can't find INotify in Windows priority apps list
**Solutions:**
1. ?? Restart INotify app to register with Windows
2. ?? Send a test notification from INotify first
3. ?? Check if INotify appears in regular notification settings first

## Best Practices

### ?? Smart Categorization
- **High Priority**: Only truly urgent apps (5-10 apps max)
- **Medium Priority**: Important but not critical (10-15 apps)
- **Low Priority**: Everything else you want to track

### ??? Space Organization
- **Work/Personal separation**: Different spaces for work vs personal apps
- **Context-based**: Group apps by usage context (communication, productivity, entertainment)
- **Time-based**: Different spaces for different times of day

### ?? Balanced Approach
- Don't categorize every app - keep non-important apps uncategorized
- Use Priority for urgency, Spaces for organization
- Review and adjust categories based on actual usage patterns

## Advanced Tips

### ?? Combining Priority + Spaces
- Assign both priority AND space to the same app
- Example: WhatsApp ? Medium Priority + Personal Space
- Results in: `[?? Medium Priority | ??? Personal Space] WhatsApp`

### ?? Future Sound Customization
- Framework is ready for custom notification sounds per category
- Will allow different sounds for High/Medium/Low priority
- Coming in future updates

### ?? Toast Actions
- **"View in INotify"**: Opens INotify and brings to foreground
- **"Dismiss"**: Dismisses the toast (future: marks as read)
- More actions planned for future releases

---

**?? You're all set!** INotify will now create smart, categorized toast notifications for your important apps, ensuring you never miss critical communications even during Do Not Disturb periods.