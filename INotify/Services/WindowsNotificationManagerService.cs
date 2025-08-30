using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace INotify.Services
{
    /// <summary>
    /// Service for managing Windows notifications programmatically
    /// Provides functionality to clear notifications from Windows Action Center
    /// </summary>
    public class WindowsNotificationManagerService : IDisposable
    {
        private static readonly Lazy<WindowsNotificationManagerService> _instance = new(() => new WindowsNotificationManagerService());
        public static WindowsNotificationManagerService Instance => _instance.Value;

        private UserNotificationListener? _listener;
        private bool _isInitialized = false;
        private bool _disposed = false;

        private WindowsNotificationManagerService()
        {
        }

        /// <summary>
        /// Initializes the service for notification management
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) return true;

            try
            {
                Debug.WriteLine("?? Initializing Windows Notification Manager Service...");

                // Check if UserNotificationListener is supported
                if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
                {
                    Debug.WriteLine("? UserNotificationListener not supported on this system");
                    return false;
                }

                _listener = UserNotificationListener.Current;
                var accessStatus = await _listener.RequestAccessAsync();

                switch (accessStatus)
                {
                    case UserNotificationListenerAccessStatus.Allowed:
                        Debug.WriteLine("? Windows notification management access granted");
                        _isInitialized = true;
                        return true;
                    case UserNotificationListenerAccessStatus.Denied:
                        Debug.WriteLine("? Windows notification management access denied");
                        return false;
                    case UserNotificationListenerAccessStatus.Unspecified:
                        Debug.WriteLine("?? Windows notification management access status unspecified");
                        return false;
                    default:
                        Debug.WriteLine($"?? Unknown access status: {accessStatus}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error initializing Windows Notification Manager Service: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clears all notifications for a specific package from Windows Action Center
        /// </summary>
        /// <param name="packageFamilyName">The package family name of the app</param>
        /// <returns>Number of notifications cleared</returns>
        public async Task<int> ClearNotificationsForPackageAsync(string packageFamilyName)
        {
            if (!_isInitialized)
            {
                var initialized = await InitializeAsync();
                if (!initialized)
                {
                    Debug.WriteLine("? Cannot clear notifications - service not initialized");
                    return 0;
                }
            }

            try
            {
                Debug.WriteLine($"?? Clearing Windows notifications for package: {packageFamilyName}");

                if (_listener == null)
                {
                    Debug.WriteLine("? UserNotificationListener is null");
                    return 0;
                }

                // Get all toast notifications
                var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                var targetNotifications = new List<UserNotification>();

                foreach (UserNotification notification in notifications)
                {
                    try
                    {
                        // Check if this notification belongs to the target package
                        var notificationPackage = notification.AppInfo?.PackageFamilyName ?? notification.AppInfo?.AppUserModelId ?? "";
                        
                        if (string.Equals(notificationPackage, packageFamilyName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetNotifications.Add(notification);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"?? Error checking notification package: {ex.Message}");
                    }
                }

                Debug.WriteLine($"?? Found {targetNotifications.Count} notifications to clear for {packageFamilyName}");

                // Remove each notification
                int clearedCount = 0;
                foreach (var notification in targetNotifications)
                {
                    try
                    {
                        await RemoveNotificationAsync(notification);
                        clearedCount++;
                        Debug.WriteLine($"? Cleared notification ID: {notification.Id}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"? Failed to clear notification ID {notification.Id}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"?? Successfully cleared {clearedCount}/{targetNotifications.Count} notifications for {packageFamilyName}");
                return clearedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error clearing notifications for package {packageFamilyName}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Clears notifications for multiple packages
        /// </summary>
        /// <param name="packageFamilyNames">List of package family names</param>
        /// <returns>Dictionary with package names and their cleared notification counts</returns>
        public async Task<Dictionary<string, int>> ClearNotificationsForPackagesAsync(IEnumerable<string> packageFamilyNames)
        {
            var results = new Dictionary<string, int>();

            foreach (var packageName in packageFamilyNames)
            {
                try
                {
                    var clearedCount = await ClearNotificationsForPackageAsync(packageName);
                    results[packageName] = clearedCount;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"? Error clearing notifications for {packageName}: {ex.Message}");
                    results[packageName] = 0;
                }
            }

            return results;
        }

        /// <summary>
        /// Gets count of notifications for a specific package
        /// </summary>
        /// <param name="packageFamilyName">The package family name</param>
        /// <returns>Number of notifications in Windows Action Center</returns>
        public async Task<int> GetNotificationCountForPackageAsync(string packageFamilyName)
        {
            if (!_isInitialized)
            {
                var initialized = await InitializeAsync();
                if (!initialized) return 0;
            }

            try
            {
                if (_listener == null) return 0;

                var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                int count = 0;

                foreach (UserNotification notification in notifications)
                {
                    try
                    {
                        var notificationPackage = notification.AppInfo?.PackageFamilyName ?? notification.AppInfo?.AppUserModelId ?? "";
                        if (string.Equals(notificationPackage, packageFamilyName, StringComparison.OrdinalIgnoreCase))
                        {
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"?? Error checking notification: {ex.Message}");
                    }
                }

                Debug.WriteLine($"?? Found {count} Windows notifications for {packageFamilyName}");
                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error getting notification count for {packageFamilyName}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets all notifications currently in Windows Action Center grouped by package
        /// </summary>
        /// <returns>Dictionary with package names and their notification counts</returns>
        public async Task<Dictionary<string, int>> GetAllNotificationCountsAsync()
        {
            if (!_isInitialized)
            {
                var initialized = await InitializeAsync();
                if (!initialized) return new Dictionary<string, int>();
            }

            try
            {
                Debug.WriteLine("?? Getting all Windows notification counts...");

                if (_listener == null) return new Dictionary<string, int>();

                var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                var packageCounts = new Dictionary<string, int>();

                foreach (UserNotification notification in notifications)
                {
                    try
                    {
                        var packageName = notification.AppInfo?.PackageFamilyName ?? notification.AppInfo?.AppUserModelId ?? "Unknown";
                        var appDisplayName = notification.AppInfo?.DisplayInfo?.DisplayName ?? "Unknown App";

                        // Use display name for better readability, fall back to package name
                        var key = !string.IsNullOrEmpty(appDisplayName) && appDisplayName != "Unknown App" ? appDisplayName : packageName;

                        if (packageCounts.ContainsKey(key))
                        {
                            packageCounts[key]++;
                        }
                        else
                        {
                            packageCounts[key] = 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"?? Error processing notification: {ex.Message}");
                    }
                }

                Debug.WriteLine($"?? Found notifications from {packageCounts.Count} different apps");
                foreach (var kvp in packageCounts.OrderByDescending(x => x.Value))
                {
                    Debug.WriteLine($"  ?? {kvp.Key}: {kvp.Value} notifications");
                }

                return packageCounts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error getting all notification counts: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Clears all notifications from Windows Action Center (use with caution)
        /// </summary>
        /// <returns>Number of notifications cleared</returns>
        public async Task<int> ClearAllNotificationsAsync()
        {
            if (!_isInitialized)
            {
                var initialized = await InitializeAsync();
                if (!initialized) return 0;
            }

            try
            {
                Debug.WriteLine("?? Clearing ALL Windows notifications...");

                if (_listener == null) return 0;

                var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                int clearedCount = 0;

                foreach (UserNotification notification in notifications)
                {
                    try
                    {
                        await RemoveNotificationAsync(notification);
                        clearedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"? Failed to clear notification: {ex.Message}");
                    }
                }

                Debug.WriteLine($"?? Cleared {clearedCount} notifications from Windows Action Center");
                return clearedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error clearing all notifications: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Removes a specific notification
        /// </summary>
        private async Task RemoveNotificationAsync(UserNotification notification)
        {
            try
            {
                if (_listener == null) return;

                // Use RemoveNotification method if available
                await Task.Run(() =>
                {
                    try
                    {
                        _listener.RemoveNotification(notification.Id);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"?? RemoveNotification failed for ID {notification.Id}: {ex.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Failed to remove notification ID {notification.Id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests the notification clearing functionality with a demo
        /// </summary>
        public async Task<bool> TestNotificationClearingAsync()
        {
            try
            {
                Debug.WriteLine("?? === Testing Windows Notification Clearing ===");

                // Get current notification counts
                var currentCounts = await GetAllNotificationCountsAsync();
                Debug.WriteLine($"?? Current notifications in Action Center: {currentCounts.Values.Sum()}");

                if (currentCounts.Count == 0)
                {
                    Debug.WriteLine("?? No notifications found to test clearing");
                    return true;
                }

                // Test clearing notifications for the first app with notifications
                var firstApp = currentCounts.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstApp.Key))
                {
                    Debug.WriteLine($"?? Testing clear for: {firstApp.Key} ({firstApp.Value} notifications)");
                    var cleared = await ClearNotificationsForPackageAsync(firstApp.Key);
                    Debug.WriteLine($"? Test completed - cleared {cleared} notifications");
                    return cleared > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error during notification clearing test: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // UserNotificationListener doesn't implement IDisposable
                _listener = null;
                _isInitialized = false;
                Debug.WriteLine("?? Windows Notification Manager Service disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error disposing Windows Notification Manager Service: {ex.Message}");
            }

            _disposed = true;
        }
    }
}