using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.Services;
using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
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
    /// Supports priority and space categorization with modern AppNotificationBuilder API
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

        // Legacy toast notification template for fallback scenarios
        private const string LEGACY_TOAST_TEMPLATE = @"
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
                _dbHandler = INotifyLibraryDIServiceProvider.Instance.GetService<INotifyDBHandler>();
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
        /// Creates and shows a categorized toast notification using modern AppNotificationBuilder
        /// </summary>
        private async Task CreateCategorizedToastAsync(KToastVObj originalNotification, PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            try
            {
                // Build the enhanced title and content
                var enhancedTitle = BuildEnhancedTitle(originalNotification, priorityInfo, spaceInfo);
                var enhancedContent = BuildEnhancedContent(originalNotification, priorityInfo, spaceInfo);
                //var categoryTags = BuildCategoryTags(priorityInfo, spaceInfo);

                // Get appropriate sound
                var notificationSound = GetNotificationSound(priorityInfo, spaceInfo, originalNotification.ToastPackageProfile.PackageFamilyName);

                Debug.WriteLine($"🔍 Creating modern toast for {originalNotification.ToastPackageProfile.AppDisplayName}");
                Debug.WriteLine($"🔍 Title: {enhancedTitle}");
                Debug.WriteLine($"🔍 Content: {enhancedContent}");
                Debug.WriteLine($"🔊 Sound: {notificationSound} ({NotificationSoundHelper.GetSoundTypeDescription(notificationSound)})");
                //Debug.WriteLine($"🔍 Tags: {categoryTags}");

                // Create notification using modern AppNotificationBuilder
                var builder = new AppNotificationBuilder()
                    .AddText(enhancedTitle)
                    .AddText(enhancedContent);
                    //.AddText(categoryTags);

                // Apply sound based on type
                ApplyNotificationSound(builder, notificationSound);

                // Add action buttons
                builder.AddButton(new AppNotificationButton("View in INotify")
                    .AddArgument("action", "view")
                    .AddArgument("notificationId", originalNotification.NotificationData.NotificationId)
                    .AddArgument("originalAppName", originalNotification.ToastPackageProfile.AppDisplayName)
                    .AddArgument("originalPackage", originalNotification.ToastPackageProfile.PackageFamilyName));

                builder.AddButton(new AppNotificationButton("Dismiss")
                    .AddArgument("action", "dismiss")
                    .AddArgument("notificationId", originalNotification.NotificationData.NotificationId));

                // Add custom data for activation handling
                if (priorityInfo.HasPriority)
                {
                    builder.AddArgument("priority", priorityInfo.Priority.ToString());
                }
                if (spaceInfo.HasSpaces)
                {
                    builder.AddArgument("spaces", spaceInfo.SpacesText);
                }

                // Build the notification
                var notification = builder.BuildNotification();
                
                // Set expiration time (1 hour from now)
                notification.Expiration = DateTimeOffset.Now.AddHours(1);

                // Show the notification using modern API
                AppNotificationManager.Default.Show(notification);

                Debug.WriteLine($"✅ Created modern categorized toast for {originalNotification.ToastPackageProfile.AppDisplayName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creating categorized toast: {ex.Message}");
                Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                // Fallback to legacy method if modern API fails
                await CreateLegacyCategorizedToastAsync(originalNotification, priorityInfo, spaceInfo);
            }
        }

        /// <summary>
        /// Fallback method using legacy XML approach for compatibility
        /// </summary>
        private async Task CreateLegacyCategorizedToastAsync(KToastVObj originalNotification, PriorityInfo priorityInfo, SpaceInfo spaceInfo)
        {
            try
            {
                Debug.WriteLine("🔄 Falling back to legacy toast creation method...");
                
                // Build the enhanced title and content
                var enhancedTitle = BuildEnhancedTitle(originalNotification, priorityInfo, spaceInfo);
                var enhancedContent = BuildEnhancedContent(originalNotification, priorityInfo, spaceInfo);
                var categoryTags = BuildCategoryTags(priorityInfo, spaceInfo);

                // Get app icon path
                var iconPath = await GetAppIconPathAsync(originalNotification);

                // Get appropriate sound
                var notificationSound = GetNotificationSound(priorityInfo, spaceInfo, originalNotification.ToastPackageProfile.PackageFamilyName);
                var soundPath = GetLegacySoundPath(notificationSound);

                // Create toast XML using legacy approach
                var toastXml = CreateToastXml(
                    enhancedTitle,
                    enhancedContent,
                    categoryTags,
                    iconPath,
                    soundPath,
                    originalNotification.NotificationData.NotificationId
                );

                // Create and show toast using legacy API
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

                Debug.WriteLine($"✅ Created legacy categorized toast for {originalNotification.ToastPackageProfile.AppDisplayName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Legacy toast creation also failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a simple test toast using modern AppNotificationBuilder to verify the service is working
        /// </summary>
        public async Task CreateTestToastAsync()
        {
            try
            {
                Debug.WriteLine("🔍 Creating modern test toast...");

                // Create notification using modern AppNotificationBuilder
                var notification = new AppNotificationBuilder()
                    .AddText("🔔 INotify Test Notification")
                    .AddText("Welcome to modern toast notifications!")
                    .AddText("This test verifies that the AppNotificationBuilder API is working correctly.")
                    .BuildNotification();

                // Show the notification
                AppNotificationManager.Default.Show(notification);

                Debug.WriteLine("✅ Modern test toast created successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creating modern test toast: {ex.Message}");
                Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                
                // Fallback to legacy test toast
                await CreateLegacyTestToastAsync();
            }
        }

        /// <summary>
        /// Creates a legacy test toast for compatibility
        /// </summary>
        private async Task CreateLegacyTestToastAsync()
        {
            try
            {
                Debug.WriteLine("🔄 Creating legacy test toast as fallback...");
                
                var toastXml = new XmlDocument();
                var xmlContent = @"
<toast>
    <visual>
        <binding template='ToastGeneric'>
            <text>🔔 INotify Test Notification</text>
            <text>Legacy API - This is a fallback test toast</text>
            <text>Basic functionality is working</text>
        </binding>
    </visual>
</toast>";
                
                toastXml.LoadXml(xmlContent);
                
                var toast = new ToastNotification(toastXml);
                var notifier = ToastNotificationManager.CreateToastNotifier(_appId);
                notifier.Show(toast);
                
                Debug.WriteLine("✅ Legacy test toast created successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Legacy test toast also failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Comprehensive test method to verify modern toast functionality with sound system
        /// </summary>
        //public async Task TestModernToastSystemAsync()
        //{
        //    try
        //    {
        //        Debug.WriteLine("🧪 === Testing Modern Toast System with Sound Support ===");
                
        //        // Test 1: Basic modern toast
        //        Debug.WriteLine("🧪 Test 1: Basic modern toast...");
        //        await CreateTestToastAsync();
        //        await Task.Delay(2000); // Wait between tests
                
        //        // Test 2: Simulated High Priority toast
        //        Debug.WriteLine("🧪 Test 2: High Priority simulation...");
        //        await CreateSimulatedCategorizedToast("High Priority", "WhatsApp", "John: Hey, are you coming to the meeting?");
        //        await Task.Delay(2000);
                
        //        // Test 3: Test custom sound
        //        Debug.WriteLine("🧪 Test 3: Custom sound test...");
        //        await CreateSoundTestToast("Custom Sound Test", NotificationSounds.Bell);
        //        await Task.Delay(2000);
                
        //        // Test 4: Test system sound
        //        Debug.WriteLine("🧪 Test 4: System sound test...");
        //        await CreateSoundTestToast("System Sound Test", NotificationSounds.SystemSMS);
        //        await Task.Delay(2000);
                
        //        // Test 5: Complex categorization
        //        Debug.WriteLine("🧪 Test 5: Complex categorization simulation...");
        //        await CreateSimulatedCategorizedToast("Low Priority + Space 2, Space 3", "Slack", "New message in #general channel");
                
        //        Debug.WriteLine("🧪 === Modern Toast System Tests Complete ===");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"❌ Error during toast system testing: {ex.Message}");
        //    }
        //}

        /// <summary>
        /// Creates a test toast with specific sound for testing sound system
        /// </summary>
        private async Task CreateSoundTestToast(string testName, NotificationSounds sound)
        {
            try
            {
                var builder = new AppNotificationBuilder()
                    .AddText($"🔔 {testName}")
                    .AddText($"Testing sound: {NotificationSoundHelper.GetSoundDisplayText(sound)}")
                    .AddText($"Sound type: {NotificationSoundHelper.GetSoundTypeDescription(sound)}");

                // Apply the specific sound
                ApplyNotificationSound(builder, sound);

                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);

                Debug.WriteLine($"✅ Created sound test toast: {testName} with {sound}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creating sound test toast: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests the enhanced sound system with direct MediaPlayer playback
        /// </summary>
        public async Task TestEnhancedSoundSystemAsync()
        {
            try
            {
                Debug.WriteLine("🧪 === Testing Enhanced Sound System ===");
                
                // Test 1: Custom sound via MediaPlayer
                Debug.WriteLine("🧪 Test 1: Custom sound (Bell)...");
                await SoundTestService.Instance.TestSoundAsync(NotificationSounds.Bell);
                await Task.Delay(2000);
                
                // Test 2: System sound via MediaPlayer (no toast)
                Debug.WriteLine("🧪 Test 2: System sound via MediaPlayer (SystemSMS)...");
                await SoundTestService.Instance.TestSoundAsync(NotificationSounds.SystemSMS);
                await Task.Delay(2000);
                
                // Test 3: System sound via toast notification
                Debug.WriteLine("🧪 Test 3: System sound via toast (SystemAlarm)...");
                await CreateSoundTestToast("System Sound via Toast", NotificationSounds.SystemAlarm);
                await Task.Delay(2000);
                
                // Test 4: Compare both approaches
                Debug.WriteLine("🧪 Test 4: Compare both approaches (SystemCall)...");
                Debug.WriteLine("  → Testing via MediaPlayer...");
                await SoundTestService.Instance.TestSoundAsync(NotificationSounds.SystemCall);
                await Task.Delay(1000);
                Debug.WriteLine("  → Testing via Toast...");
                await CreateSoundTestToast("Compare Test", NotificationSounds.SystemCall);
                
                Debug.WriteLine("🧪 === Enhanced Sound System Tests Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error during enhanced sound system testing: {ex.Message}");
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
            var content = notification.NotificationData.NotificationMessage ?? "";
            return $"{content}";
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
        /// Gets legacy sound path for XML-based toast notifications
        /// </summary>
        private string GetLegacySoundPath(NotificationSounds sound)
        {
            try
            {
                if (sound == NotificationSounds.None)
                {
                    return "ms-winsoundevent:Notification.Default";
                }

                if (NotificationSoundHelper.IsCustomSound(sound))
                {
                    return NotificationSoundHelper.GetCustomSoundPath(sound);
                }

                if (NotificationSoundHelper.IsSystemSound(sound))
                {
                    // For system sounds in legacy mode, map to equivalent system sound URIs
                    var systemSound = NotificationSoundHelper.GetSystemSoundEvent(sound);
                    return $"ms-winsoundevent:Notification.{systemSound}";
                }

                return "ms-winsoundevent:Notification.Default";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting legacy sound path for {sound}: {ex.Message}");
                return "ms-winsoundevent:Notification.Default";
            }
        }

        /// <summary>
        /// Applies notification sound to AppNotificationBuilder based on sound type
        /// Uses SetAudioUri for custom sounds and SetAudioEvent for system sounds
        /// </summary>
        private void ApplyNotificationSound(AppNotificationBuilder builder, NotificationSounds sound)
        {
            try
            {
                if (sound == NotificationSounds.None)
                {
                    // Use default system sound
                    builder.SetAudioEvent(AppNotificationSoundEvent.Default, AppNotificationAudioLooping.None);
                    Debug.WriteLine($"🔊 Applied default system sound");
                    return;
                }

                if (NotificationSoundHelper.IsCustomSound(sound))
                {
                    // Custom sound - use SetAudioUri with ms-appx URI
                    var soundPath = NotificationSoundHelper.GetCustomSoundPath(sound);
                    try
                    {
                        builder.SetAudioUri(new Uri(soundPath));
                        Debug.WriteLine($"🔊 Applied custom sound: {sound} -> {soundPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Failed to set custom sound {sound}: {ex.Message}, falling back to default");
                        builder.SetAudioEvent(AppNotificationSoundEvent.Default, AppNotificationAudioLooping.None);
                    }
                }
                else if (NotificationSoundHelper.IsSystemSound(sound))
                {
                    // System sound - use SetAudioEvent
                    var systemSound = NotificationSoundHelper.GetSystemSoundEvent(sound);
                    builder.SetAudioEvent(systemSound, AppNotificationAudioLooping.None);
                    Debug.WriteLine($"🔊 Applied system sound: {sound} -> {systemSound}");
                }
                else
                {
                    // Fallback to default
                    builder.SetAudioEvent(AppNotificationSoundEvent.Default, AppNotificationAudioLooping.None);
                    Debug.WriteLine($"🔊 Unknown sound type {sound}, using default");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error applying notification sound {sound}: {ex.Message}");
                // Fallback to default sound
                try
                {
                    builder.SetAudioEvent(AppNotificationSoundEvent.Default, AppNotificationAudioLooping.None);
                }
                catch
                {
                    // If even default fails, continue without sound
                    Debug.WriteLine($"⚠️ Failed to apply even default sound");
                }
            }
        }

        /// <summary>
        /// Gets the appropriate notification sound based on categorization and sound mappings
        /// Now supports both custom sounds (SetAudioUri) and system sounds (SetAudioEvent)
        /// </summary>
        private NotificationSounds GetNotificationSound(PriorityInfo priorityInfo, SpaceInfo spaceInfo, string packageFamilyName)
        {
            try
            {
                // Get custom sound mapping for this package
                var customSound = _dbHandler?.GetPackageSound(packageFamilyName, INotifyConstant.CurrentUser) ?? NotificationSounds.None;
                
                Debug.WriteLine($"🔊 Sound mapping for {packageFamilyName}: {customSound} ({NotificationSoundHelper.GetSoundTypeDescription(customSound)})");
                
                return customSound;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting notification sound: {ex.Message}");
                return NotificationSounds.None;
            }
        }

        /// <summary>
        /// Creates the toast XML document for legacy fallback
        /// </summary>
        private XmlDocument CreateToastXml(string title, string content, string categoryTags, string iconPath, string soundPath, string notificationId)
        {
            var toastXml = new XmlDocument();
            var xmlContent = string.Format(
                LEGACY_TOAST_TEMPLATE,
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