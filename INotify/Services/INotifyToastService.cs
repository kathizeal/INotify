using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.Services;
using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;

namespace INotify.Services
{
    /// <summary>
    /// Service for creating and managing toast notifications from INotify app
    /// Supports priority and space categorization with custom sounds (future)
    /// </summary>
    public class INotifyToastService : IDisposable
    {
        private static readonly Lazy<INotifyToastService> _instance = new(() => new INotifyToastService());
        public static INotifyToastService Instance => _instance.Value;

        private readonly NotificationFilterCacheService _cacheService;
        private readonly INotifyDBHandler _dbHandler;
        private readonly string _appDisplayName = "INotify";
        private readonly string _appId;
        private bool _disposed = false;

        // Toast notification template for categorized notifications
        private const string TOAST_TEMPLATE = @"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>{0}</text>
            <text>{1}</text>
            <text>{2}</text>
            <image placement='appLogoOverride' hint-crop='circle' src='{3}'/>
        </binding>
    </visual>
    <audio src='{4}' loop='false'/>
    <actions>
        <action content='View in INotify' arguments='action=view&amp;notificationId={5}' activationType='foreground'/>
        <action content='Dismiss' arguments='action=dismiss' activationType='background'/>
    </actions>
</toast>";

        private INotifyToastService()
        {
            try
            {
                _cacheService = NotificationFilterCacheService.Instance;
                _dbHandler = KToastDIServiceProvider.Instance.GetService<INotifyDBHandler>();
                _appId = Package.Current.Id.FamilyName;
                Debug.WriteLine("INotifyToastService initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing INotifyToastService: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a received notification and creates INotify toast if categorized
        /// </summary>
        public async Task ProcessNotificationAsync(KToastVObj originalNotification)
        {
            try
            {
                if (originalNotification?.ToastPackageProfile == null || originalNotification.NotificationData == null)
                    return;

                var packageFamilyName = originalNotification.ToastPackageProfile.PackageFamilyName;
                var appDisplayName = originalNotification.ToastPackageProfile.AppDisplayName;

                // Check if app has priority assignment
                var priorityInfo = await GetAppPriorityInfoAsync(packageFamilyName);
                
                // Check if app belongs to any spaces
                var spaceInfo = await GetAppSpaceInfoAsync(packageFamilyName);

                // Only create toast if app is categorized
                if (priorityInfo.HasPriority || spaceInfo.HasSpaces)
                {
                    await CreateCategorizedToastAsync(originalNotification, priorityInfo, spaceInfo);
                }
                else
                {
                    Debug.WriteLine($"App {appDisplayName} not categorized, skipping toast creation");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing notification for toast: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets priority information for an app
        /// </summary>
        private async Task<PriorityInfo> GetAppPriorityInfoAsync(string packageFamilyName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    foreach (Priority priority in Enum.GetValues<Priority>())
                    {
                        if (priority == Priority.None) continue;

                        if (_cacheService.IsPackageInPriorityCategory(packageFamilyName, priority.ToString()))
                        {
                            return new PriorityInfo
                            {
                                HasPriority = true,
                                Priority = priority,
                                PriorityText = GetPriorityDisplayText(priority),
                                PriorityIcon = GetPriorityIcon(priority)
                            };
                        }
                    }

                    return new PriorityInfo { HasPriority = false };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting priority info: {ex.Message}");
                    return new PriorityInfo { HasPriority = false };
                }
            });
        }

        /// <summary>
        /// Gets space information for an app
        /// </summary>
        private async Task<SpaceInfo> GetAppSpaceInfoAsync(string packageFamilyName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var spaces = new List<string>();
                    var spaceIds = new[] { "Space1", "Space2", "Space3" };

                    foreach (var spaceId in spaceIds)
                    {
                        if (_cacheService.IsPackageInSpace(packageFamilyName, spaceId))
                        {
                            spaces.Add(GetSpaceDisplayText(spaceId));
                        }
                    }

                    return new SpaceInfo
                    {
                        HasSpaces = spaces.Count > 0,
                        Spaces = spaces,
                        SpacesText = spaces.Count > 0 ? string.Join(", ", spaces) : "",
                        SpaceIcon = spaces.Count > 0 ? GetSpaceIcon() : ""
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting space info: {ex.Message}");
                    return new SpaceInfo { HasSpaces = false };
                }
            });
        }

        /// <summary>
        /// Creates and shows a categorized toast notification
        /// </summary>
        private async Task CreateCategorizedToastAsync(KToastVObj originalNotification, PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            try
            {
                // Build the enhanced title and content
                var enhancedTitle = BuildEnhancedTitle(originalNotification, priorityInfo, spaceInfo);
                var enhancedContent = BuildEnhancedContent(originalNotification, priorityInfo, spaceInfo);
                var categoryTags = BuildCategoryTags(priorityInfo, spaceInfo);

                // Get app icon path
                var iconPath = await GetAppIconPathAsync(originalNotification);

                // Get appropriate sound (future enhancement)
                var soundPath = GetNotificationSound(priorityInfo, spaceInfo);

                // Create toast XML
                var toastXml = CreateToastXml(
                    enhancedTitle,
                    enhancedContent,
                    categoryTags,
                    iconPath,
                    soundPath,
                    originalNotification.NotificationData.NotificationId
                );

                // Create and show toast
                var toast = new ToastNotification(toastXml);
                
                // Set expiration time (1 hour from now)
                toast.ExpirationTime = DateTimeOffset.Now.AddHours(1);
                
                // Add custom data for handling activation
                toast.Data = new NotificationData();
                toast.Data.Values["originalAppName"] = originalNotification.ToastPackageProfile.AppDisplayName;
                toast.Data.Values["originalPackage"] = originalNotification.ToastPackageProfile.PackageFamilyName;
                toast.Data.Values["priority"] = priorityInfo.HasPriority ? priorityInfo.Priority.ToString() : "";
                toast.Data.Values["spaces"] = spaceInfo.SpacesText;


                // Create toast notifier and show
                var notifier = ToastNotificationManager.CreateToastNotifier(_appId);
                notifier.Show(toast);

                Debug.WriteLine($"Created categorized toast for {originalNotification.ToastPackageProfile.AppDisplayName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating categorized toast: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds enhanced title with category information
        /// </summary>
        private string BuildEnhancedTitle(KToastVObj notification, PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            var parts = new List<string>();
            
            if (priorityInfo.HasPriority)
            {
                parts.Add($"{priorityInfo.PriorityIcon} {priorityInfo.PriorityText}");
            }
            
            if (spaceInfo.HasSpaces)
            {
                parts.Add($"{spaceInfo.SpaceIcon} {spaceInfo.SpacesText}");
            }

            var categorization = parts.Count > 0 ? $"[{string.Join(" | ", parts)}]" : "";
            var appName = notification.ToastPackageProfile.AppDisplayName;
            
            return $"{categorization} {appName}";
        }

        /// <summary>
        /// Builds enhanced content with original notification content
        /// </summary>
        private string BuildEnhancedContent(KToastVObj notification, PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            var originalTitle = notification.NotificationData.NotificationTitle ?? "";
            return $"{originalTitle}";
        }

        /// <summary>
        /// Builds category tags for display
        /// </summary>
        private string BuildCategoryTags(PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            var tags = new List<string>();
            
            if (priorityInfo.HasPriority)
            {
                tags.Add($"Priority: {priorityInfo.PriorityText}");
            }
            
            if (spaceInfo.HasSpaces)
            {
                tags.Add($"Spaces: {spaceInfo.SpacesText}");
            }

            return tags.Count > 0 ? string.Join(" • ", tags) : "Managed by INotify";
        }

        /// <summary>
        /// Gets the app icon path for the toast
        /// </summary>
        private async Task<string> GetAppIconPathAsync(KToastVObj notification)
        {
            try
            {
                // Use the cached icon path if available
                if (!string.IsNullOrEmpty(notification.ToastPackageProfile.LogoFilePath) && 
                    File.Exists(notification.ToastPackageProfile.LogoFilePath))
                {
                    return notification.ToastPackageProfile.LogoFilePath;
                }

                // Fall back to default INotify icon
                var packagePath = Package.Current.InstalledLocation.Path;
                var defaultIconPath = Path.Combine(packagePath, "Assets", "StoreLogo.png");
                return File.Exists(defaultIconPath) ? defaultIconPath : "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting app icon path: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Gets the appropriate notification sound based on categorization
        /// Future enhancement: different sounds for different categories
        /// </summary>
        private string GetNotificationSound(PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            // Future enhancement: return different sounds based on priority/space
            // For now, use default notification sound
            return "ms-winsoundevent:Notification.Default";
        }

        /// <summary>
        /// Creates the toast XML document
        /// </summary>
        private XmlDocument CreateToastXml(string title, string content, string categoryTags, string iconPath, string soundPath, string notificationId)
        {
            var toastXml = new XmlDocument();
            var xmlContent = string.Format(
                TOAST_TEMPLATE,
                System.Security.SecurityElement.Escape(title),
                System.Security.SecurityElement.Escape(content),
                System.Security.SecurityElement.Escape(categoryTags),
                System.Security.SecurityElement.Escape(iconPath),
                soundPath,
                notificationId
            );
            
            toastXml.LoadXml(xmlContent);
            return toastXml;
        }

        #region Helper Methods

        private string GetPriorityDisplayText(Priority priority) => priority switch
        {
            Priority.High => "High Priority",
            Priority.Medium => "Medium Priority", 
            Priority.Low => "Low Priority",
            _ => "Priority"
        };

        private string GetPriorityIcon(Priority priority) => priority switch
        {
            Priority.High => "🔴",
            Priority.Medium => "🟡",
            Priority.Low => "🟢",
            _ => "📌"
        };

        private string GetSpaceDisplayText(string spaceId) => spaceId switch
        {
            "Space1" => "Space 1",
            "Space2" => "Space 2", 
            "Space3" => "Space 3",
            _ => spaceId
        };

        private string GetSpaceIcon() => "🏷️";

        #endregion

        #region Data Classes

        private class PriorityInfo
        {
            public bool HasPriority { get; set; }
            public Priority Priority { get; set; }
            public string PriorityText { get; set; } = "";
            public string PriorityIcon { get; set; } = "";
        }

        private class SpaceInfo
        {
            public bool HasSpaces { get; set; }
            public List<string> Spaces { get; set; } = new();
            public string SpacesText { get; set; } = "";
            public string SpaceIcon { get; set; } = "";
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}