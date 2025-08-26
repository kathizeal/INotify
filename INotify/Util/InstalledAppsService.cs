using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage.Streams;
using System.Xml.Linq;
using Windows.Graphics.Imaging;

namespace AppList
{
    public class InstalledAppInfo : System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string PublisherId { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public BitmapImage? Icon { get; set; }
        public DateTime? InstallDate { get; set; }
        public long Size { get; set; }
        public string UninstallString { get; set; } = string.Empty;
        public AppType Type { get; set; }
        public string PackageFamilyName { get; set; } = string.Empty;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        // Initialize the DND service instance
        //public static void SetDndService(DndService dndService)
        //{
        //    _dndServiceInstance = dndService;
        //}

        //public string PriorityStatusText
        //{
        //    get
        //    {
        //        if (_dndServiceInstance == null) return "Unknown";

        //        try
        //        {
        //            string appId = GenerateAppId();
        //            if (string.IsNullOrEmpty(appId)) return "Unknown";

        //            bool isInPriority = _dndServiceInstance.IsInPriorityList(appId);
        //            return isInPriority ? "Priority" : "Standard";
        //        }
        //        catch
        //        {
        //            return "Unknown";
        //        }
        //    }
        //}

        //public SolidColorBrush PriorityStatusColor
        //{
        //    get
        //    {
        //        if (_dndServiceInstance == null)
        //            return new SolidColorBrush(Microsoft.UI.Colors.Gray);

        //        try
        //        {
        //            string appId = GenerateAppId();
        //            if (string.IsNullOrEmpty(appId))
        //                return new SolidColorBrush(Microsoft.UI.Colors.Gray);

        //            bool isInPriority = _dndServiceInstance.IsInPriorityList(appId);
        //            return new SolidColorBrush(isInPriority ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Gray);
        //        }
        //        catch
        //        {
        //            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        //        }
        //    }
        //}

        private string GenerateAppId()
        {
            switch (Type)
            {
                case AppType.UWPApplication:
                    return !string.IsNullOrEmpty(PackageFamilyName) ? PackageFamilyName : Name;

                case AppType.Win32Application:
                    if (!string.IsNullOrEmpty(ExecutablePath))
                    {
                        return Path.GetFileNameWithoutExtension(ExecutablePath);
                    }
                    if (!string.IsNullOrEmpty(InstallLocation) && Directory.Exists(InstallLocation))
                    {
                        try
                        {
                            var exeFiles = Directory.GetFiles(InstallLocation, "*.exe", SearchOption.TopDirectoryOnly);
                            if (exeFiles.Length > 0)
                            {
                                return Path.GetFileNameWithoutExtension(exeFiles[0]);
                            }
                        }
                        catch { }
                    }
                    return DisplayName.Replace(" ", "").Replace(".", "").Replace("-", "");

                default:
                    return Name;
            }
        }

    }

    public enum AppType
    {
        Win32Application,
        UWPApplication,
        MSIXApplication
    }


    public class InstalledAppsService
    {
        // Win32 API imports for icon extraction
        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out ushort lpiIcon);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private static readonly Dictionary<string, BitmapImage> _iconCache = new();

        /// <summary>
        /// Gets all installed applications (Win32 + UWP) excluding the current app
        /// </summary>
        public async Task<ObservableCollection<InstalledAppInfo>> GetAllInstalledAppsAsync()
        {
            var apps = new ObservableCollection<InstalledAppInfo>();

            // Current app identifiers to exclude
            const string currentAppName = "INotify";
            const string currentAppPackageName = "53bb359b-204d-49dc-a511-4e29dc79d34b";
            const string currentAppPublisher = "Kathi";

            // Get Win32 applications
            var win32Apps = await GetWin32AppsAsync();
            foreach (var app in win32Apps)
            {
                // Skip the current app based on display name and publisher
                if (IsCurrentApp(app, currentAppName, currentAppPackageName, currentAppPublisher))
                    continue;

                apps.Add(app);
            }

            // Get UWP applications
            var uwpApps = await GetUWPAppsAsync();
            foreach (var app in uwpApps)
            {
                // Skip the current app based on package name or display name
                if (IsCurrentApp(app, currentAppName, currentAppPackageName, currentAppPublisher))
                    continue;

                apps.Add(app);
            }

            return apps;
        }

        /// <summary>
        /// Determines if the given app is the current INotify app
        /// </summary>
        private bool IsCurrentApp(InstalledAppInfo app, string currentAppName, string currentAppPackageName, string currentAppPublisher)
        {
            // Check by package name (for UWP apps)
            if (!string.IsNullOrEmpty(app.Name) && 
                app.Name.Equals(currentAppPackageName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check by display name
            if (!string.IsNullOrEmpty(app.DisplayName) && 
                app.DisplayName.Equals(currentAppName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check by display name and publisher combination
            if (!string.IsNullOrEmpty(app.DisplayName) && 
                !string.IsNullOrEmpty(app.Publisher) &&
                app.DisplayName.Equals(currentAppName, StringComparison.OrdinalIgnoreCase) &&
                app.Publisher.Equals(currentAppPublisher, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if executable path contains the current app name
            if (!string.IsNullOrEmpty(app.ExecutablePath) && 
                app.ExecutablePath.Contains(currentAppName, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// Gets Win32 applications from registry
        /// </summary>
        public async Task<List<InstalledAppInfo>> GetWin32AppsAsync()
        {
            return await Task.Run(() =>
            {
                var apps = new List<InstalledAppInfo>();
                var registryKeys = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var keyPath in registryKeys)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                        {
                            if (key != null)
                            {
                                foreach (var subKeyName in key.GetSubKeyNames())
                                {
                                    using (var subKey = key.OpenSubKey(subKeyName))
                                    {
                                        if (subKey != null)
                                        {
                                            var app = CreateWin32AppInfo(subKey, subKeyName);
                                            if (app != null && !string.IsNullOrEmpty(app.DisplayName))
                                            {
                                                apps.Add(app);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading registry: {ex.Message}");
                    }
                }

                return apps.OrderBy(a => a.DisplayName).ToList();
            });
        }

        /// <summary>
        /// Gets UWP applications using PackageManager
        /// </summary>
        /// <summary>
        /// Gets UWP applications using PackageManager
        /// </summary>
        public async Task<List<InstalledAppInfo>> GetUWPAppsAsync()
        {
            return await Task.Run(() =>
            {
                var apps = new List<InstalledAppInfo>();

                try
                {
                    var packageManager = new PackageManager();
                    var packages = packageManager.FindPackagesForUser("");

                    foreach (var package in packages)
                    {
                        try
                        {
                            // Skip only frameworks and packages without display names
                            // Allow system packages that are user-facing apps like Clock, Calculator, etc.
                            if (package.IsFramework ||
                                string.IsNullOrEmpty(package.DisplayName))
                                continue;

                            // Additional filtering for truly internal system packages
                            // Keep packages that users typically interact with
                            if (package.SignatureKind == PackageSignatureKind.System)
                            {
                                // Use your existing allowlist for system apps
                                // Allow specific system apps that users commonly use
                                var allowedSystemApps = new[]
                                {
    // Core Windows Apps
    "Microsoft.WindowsAlarms", // Clock app
    "Microsoft.WindowsCalculator", // Calculator
    "Microsoft.WindowsCamera", // Camera
    "Microsoft.WindowsMaps", // Maps
    "Microsoft.WindowsNotepad", // Notepad
    "Microsoft.Paint", // Paint
    "Microsoft.WindowsTerminal", // Terminal
    "Microsoft.WindowsSoundRecorder", // Sound Recorder
    "Microsoft.ScreenSketch", // Snipping Tool
    "Microsoft.MicrosoftStickyNotes", // Sticky Notes
    "Microsoft.WindowsFeedbackHub", // Feedback Hub
    "Microsoft.GetHelp", // Get Help
    "Microsoft.Getstarted", // Tips
    "Microsoft.Microsoft3DViewer", // 3D Viewer
    "Microsoft.MSPaint", // Paint 3D
    "Microsoft.Office.OneNote", // OneNote
    "Microsoft.People", // People
    "Microsoft.Windows.Photos", // Photos
    "Microsoft.WindowsStore", // Microsoft Store
    
    // Xbox and Gaming
    "Microsoft.Xbox.TCUI", // Xbox
    "Microsoft.XboxApp", // Xbox Console Companion
    "Microsoft.XboxGameOverlay", // Xbox Game Bar
    "Microsoft.XboxGamingOverlay", // Xbox Gaming Overlay
    "Microsoft.XboxIdentityProvider", // Xbox Identity Provider
    "Microsoft.XboxSpeechToTextOverlay", // Xbox Speech to Text
    "Microsoft.GamingApp", // Xbox (New)
    "Microsoft.XboxGameCallableUI", // Xbox Game UI
    
    // Communication & Social
    "Microsoft.YourPhone", // Phone Link
    "Microsoft.People", // People
    "Microsoft.Messaging", // Messaging
    "Microsoft.CommsPhone", // Phone
    "Microsoft.SkypeApp", // Skype
    
    // Media & Entertainment
    "Microsoft.ZuneMusic", // Groove Music
    "Microsoft.ZuneVideo", // Movies & TV
    "Microsoft.WindowsMediaPlayer", // Windows Media Player
    "SpotifyAB.SpotifyMusic", // Spotify
    "Netflix.Netflix", // Netflix
    "5319275A.WhatsAppDesktop", // WhatsApp
    "TelegramMessengerLLP.TelegramDesktop", // Telegram
    "Discord.Discord", // Discord
    "ZoomVideoCommunications.ZoomRooms", // Zoom
    "Microsoft.Teams", // Microsoft Teams
    
    // Productivity & Office
    "Microsoft.Office.Desktop", // Office Desktop Apps
    "Microsoft.OutlookForWindows", // New Outlook
    "Microsoft.MicrosoftOfficeHub", // Office Hub
    "Microsoft.Office.Word", // Word
    "Microsoft.Office.Excel", // Excel
    "Microsoft.Office.PowerPoint", // PowerPoint
    "Microsoft.Todos", // Microsoft To Do
    "Microsoft.PowerAutomateDesktop", // Power Automate
    "Microsoft.PowerBI", // Power BI
    "9NBLGGH4NNS1", // Adobe Photoshop Elements
    "AdobeSystemsIncorporated.AdobePhotoshopLightroom", // Adobe Lightroom
    
    // Web Browsers
    "Microsoft.MicrosoftEdge", // Microsoft Edge
    "Mozilla.Firefox", // Firefox
    "Google.Chrome", // Google Chrome
    "Opera.Opera", // Opera
    
    // Development Tools
    "Microsoft.VisualStudioCode", // Visual Studio Code
    "GitHubDesktop.GitHubDesktop", // GitHub Desktop
    "Microsoft.WindowsSubsystemForLinux", // WSL
    "Microsoft.PowerShell", // PowerShell
    
    // News & Information
    "Microsoft.BingWeather", // Weather
    "Microsoft.BingNews", // News
    "Microsoft.BingFinance", // Money
    "Microsoft.BingSports", // Sports
    "Microsoft.BingTravel", // Travel
    "Microsoft.BingHealthAndFitness", // Health & Fitness
    "Microsoft.BingFoodAndDrink", // Food & Drink
    "MSTeams", // Microsoft Teams (alternative)
    
    // Utilities & Tools
    "Microsoft.WindowsReadingList", // Reading List
    "Microsoft.MixedReality.Portal", // Mixed Reality Portal
    "Microsoft.Windows.Cortana", // Cortana
    "Microsoft.WindowsBackup", // Windows Backup
    "Microsoft.RemoteDesktop", // Remote Desktop
    "Microsoft.Windows.SecHealthUI", // Windows Security
    "WinZip.WinZip", // WinZip
    "RARLab.WinRAR", // WinRAR
    "9NBLGGH4Z1JC", // VLC Media Player
    "VideoLAN.VLC", // VLC (alternative)
    
    // Popular Store Apps
    "Amazon.com.Amazon", // Amazon
    "TheNewYorkTimes.NYTimes", // NY Times
    "Facebook.Facebook", // Facebook
    "Instagram.Instagram", // Instagram
    "Twitter.Twitter", // Twitter
    "LinkedInCorporation.LinkedIn", // LinkedIn
    "Uber.Uber", // Uber
    "Pinterest.Pinterest", // Pinterest
    "TikTok.TikTok", // TikTok
    "Flipboard.Flipboard", // Flipboard
    
    // Gaming Platforms
    "ValveSoftware.Steam", // Steam
    "EpicGames.EpicGamesLauncher", // Epic Games
    "Ubisoft.UbisoftConnect", // Ubisoft Connect
    "ElectronicArts.EADesktop", // EA Desktop
    "Blizzard.BattleNet", // Battle.net
    
    // Creative & Design
    "Adobe.CC.XD", // Adobe XD
    "Adobe.Illustrator", // Adobe Illustrator
    "Canva.Canva", // Canva
    "GIMP.GIMP", // GIMP
    "Inkscape.Inkscape", // Inkscape
    
    // File Management
    "Microsoft.OneDrive", // OneDrive
    "Dropbox.Dropbox", // Dropbox
    "Google.GoogleDrive", // Google Drive
    "Box.Box", // Box
    
    // Education & Learning
    "KhanAcademy.KhanAcademy", // Khan Academy
    "Duolingo.Duolingo", // Duolingo
    "Coursera.Coursera", // Coursera
    "Microsoft.Whiteboard", // Microsoft Whiteboard
    
    // Finance & Banking
    "PayPal.PayPal", // PayPal
    "Microsoft.BingFinance", // Microsoft Money
    "Mint.Mint", // Mint
    
    // Health & Fitness
    "MyFitnessPal.MyFitnessPal", // MyFitnessPal
    "Fitbit.Fitbit", // Fitbit
    "Nike.NikeTrainingClub", // Nike Training Club
    
    // Travel & Navigation
    "Uber.Uber", // Uber
    "Airbnb.Airbnb", // Airbnb
    "Booking.Booking", // Booking.com
    "TripAdvisor.TripAdvisor", // TripAdvisor
    
    // Shopping
    "Amazon.com.Amazon", // Amazon
    "eBay.eBay", // eBay
    "Walmart.Walmart", // Walmart
    "Target.Target", // Target
    
    // System & Windows Features
    "Microsoft.Windows.CloudExperienceHost", // Cloud Experience (if user-facing)
    "Microsoft.AccountsControl", // Accounts Control (if user-facing)
};

                                if (!allowedSystemApps.Contains(package.Id.Name))
                                    continue;
                            }
                            else
                            {
                                // For non-system packages (Store apps), exclude only problematic ones
                                var excludedApps = new[]
                                {
        // Add any specific Store apps you want to exclude
        "Microsoft.Advertising.Xaml", // Internal advertising framework
        "Microsoft.NET.Native.Framework", // .NET Native framework
        "Microsoft.NET.Native.Runtime", // .NET Native runtime
    };

                                if (excludedApps.Contains(package.Id.Name))
                                    continue;
                            }

                            var app = new InstalledAppInfo
                            {
                                Name = package.Id.Name,
                                DisplayName = package.DisplayName,
                                Version = package.Id.Version.ToString(),
                                PublisherId = package.Id.PublisherId?.ToString(),
                                Publisher = package.PublisherDisplayName,
                                InstallLocation = package.InstalledLocation?.Path ?? "",
                                Type = AppType.UWPApplication,
                                PackageFamilyName = package.Id.FamilyName
                            };

                            // Try to get install date
                            try
                            {
                                app.InstallDate = package.InstalledDate.DateTime;
                            }
                            catch { }

                            apps.Add(app);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing package {package.Id.Name}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting UWP apps: {ex.Message}");
                }

                return apps.OrderBy(a => a.DisplayName).ToList();
            });
        }
        /// <summary>
        /// Gets applications with icons loaded, excluding the current app
        /// </summary>
        public async Task<ObservableCollection<InstalledAppInfo>> GetInstalledAppsWithIconsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting GetInstalledAppsWithIconsAsync...");
                
                var apps = await GetAllInstalledAppsAsync(); // This now already excludes the current app
                System.Diagnostics.Debug.WriteLine($"Retrieved {apps.Count} total apps (excluding current app)");

                // Load icons with better error handling and progress tracking
                int iconLoadTasks = 0;
                int successfulIconLoads = 0;
                
                var iconTasks = apps.Select(async (app, index) =>
                {
                    iconLoadTasks++;
                    try
                    {
                        await Task.Run(() => 
                        {
                            LoadAppIcon(app);
                            if (app.Icon != null)
                            {
                                Interlocked.Increment(ref successfulIconLoads);
                                System.Diagnostics.Debug.WriteLine($"[{index + 1}/{apps.Count}] Icon loaded for: {app.DisplayName}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[{index + 1}/{apps.Count}] No icon for: {app.DisplayName}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading icon for {app.DisplayName}: {ex.Message}");
                    }
                });

                await Task.WhenAll(iconTasks);
                
                System.Diagnostics.Debug.WriteLine($"Icon loading completed: {successfulIconLoads}/{iconLoadTasks} apps have icons");
                System.Diagnostics.Debug.WriteLine($"Cache contains {_iconCache.Count} cached icons");

                return apps;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetInstalledAppsWithIconsAsync: {ex.Message}");
                return new ObservableCollection<InstalledAppInfo>();
            }
        }

        private InstalledAppInfo CreateWin32AppInfo(RegistryKey key, string subKeyName)
        {
            try
            {
                var displayName = key.GetValue("DisplayName")?.ToString();
                var systemComponent = key.GetValue("SystemComponent");
                var parentKeyName = key.GetValue("ParentKeyName");
                var windowsInstaller = key.GetValue("WindowsInstaller");

                // Skip system components and updates
                if (string.IsNullOrEmpty(displayName) ||
                    (systemComponent != null && systemComponent.ToString() == "1") ||
                    !string.IsNullOrEmpty(parentKeyName?.ToString()) ||
                    displayName.Contains("Update for") ||
                    displayName.Contains("Hotfix for"))
                {
                    return null;
                }

                var app = new InstalledAppInfo
                {
                    Name = key.Name.Split('\\').Last(),
                    DisplayName = displayName,
                    Version = key.GetValue("DisplayVersion")?.ToString() ?? "",
                    Publisher = key.GetValue("Publisher")?.ToString() ?? "",
                    InstallLocation = key.GetValue("InstallLocation")?.ToString() ?? "",
                    UninstallString = key.GetValue("UninstallString")?.ToString() ?? "",
                    Type = AppType.Win32Application
                };

                // Get executable path
                var installLocation = app.InstallLocation;
                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    var exeFiles = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exeFiles.Length > 0)
                    {
                        app.ExecutablePath = exeFiles[0];
                    }
                }

                // Try to get install date
                var installDateValue = key.GetValue("InstallDate");
                if (installDateValue != null && DateTime.TryParseExact(installDateValue.ToString(), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var installDate))
                {
                    app.InstallDate = installDate;
                }

                // Try to get size
                var sizeValue = key.GetValue("EstimatedSize");
                if (sizeValue != null && long.TryParse(sizeValue.ToString(), out var size))
                {
                    app.Size = size * 1024; // Convert from KB to bytes

                }
                app.PackageFamilyName = app.DisplayName + "_" + app.Publisher;
                app.PackageFamilyName = app.PackageFamilyName.Trim().Replace(" ", "_");

                return app;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating app info: {ex.Message}");
                return null;
            }
        }

        private void LoadAppIcon(InstalledAppInfo app)
        {
            try
            {
                // Check cache first
                var cacheKey = $"{app.Type}_{app.PackageFamilyName}_{app.ExecutablePath}";
                if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
                {
                    app.Icon = cachedIcon;
                    System.Diagnostics.Debug.WriteLine($"Using cached icon for: {app.DisplayName}");
                    return;
                }

                BitmapImage? icon = null;
                
                if (app.Type == AppType.UWPApplication)
                {
                    System.Diagnostics.Debug.WriteLine($"Loading UWP icon for: {app.DisplayName}");
                    icon = LoadUWPIcon(app);
                    if (icon == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load UWP icon for: {app.DisplayName}");
                    }
                }
                else if (!string.IsNullOrEmpty(app.ExecutablePath) && File.Exists(app.ExecutablePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Loading Win32 icon for: {app.DisplayName} from {app.ExecutablePath}");
                    icon = LoadWin32Icon(app);
                    if (icon == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load Win32 icon for: {app.DisplayName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No executable path for Win32 app: {app.DisplayName}");
                }

                if (icon != null)
                {
                    _iconCache[cacheKey] = icon;
                    app.Icon = icon;
                    System.Diagnostics.Debug.WriteLine($"Successfully loaded and cached icon for: {app.DisplayName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No icon could be loaded for: {app.DisplayName}");
                    // Try to load a default icon
                    try
                    {
                        var defaultIcon = LoadDefaultIcon();
                        if (defaultIcon != null)
                        {
                            app.Icon = defaultIcon;
                            System.Diagnostics.Debug.WriteLine($"Using default icon for: {app.DisplayName}");
                        }
                    }
                    catch (Exception defaultEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load default icon: {defaultEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icon for {app.DisplayName}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private BitmapImage? LoadWin32Icon(InstalledAppInfo app)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to extract Win32 icon from: {app.ExecutablePath}");
                
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, app.ExecutablePath, 0);
                if (hIcon != IntPtr.Zero)
                {
                    using (var icon = Icon.FromHandle(hIcon))
                    {
                        var result = ConvertIconToBitmapImage(icon);
                        DestroyIcon(hIcon);
                        
                        if (result != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Successfully extracted Win32 icon for: {app.DisplayName}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to convert Win32 icon to BitmapImage for: {app.DisplayName}");
                        }
                        
                        return result;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ExtractIcon returned null handle for: {app.ExecutablePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting Win32 icon for {app.DisplayName}: {ex.Message}");
            }
            return null;
        }

        private BitmapImage? LoadUWPIcon(InstalledAppInfo app)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to load UWP icon for: {app.DisplayName}");
                System.Diagnostics.Debug.WriteLine($"Install location: {app.InstallLocation}");
                
                if (!string.IsNullOrEmpty(app.InstallLocation))
                {
                    // Try multiple approaches in order of preference
                    var icon = 
                        LoadIconFromManifest(app) ??
                        LoadIconFromAssets(app) ??
                        LoadIconFromPackageManager(app);

                    if (icon != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded UWP icon for: {app.DisplayName}");
                        return icon;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"All UWP icon loading methods failed for: {app.DisplayName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No install location for UWP app: {app.DisplayName}");
                }
                
                // Don't return default icon here - let the caller handle it
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading UWP icon for {app.DisplayName}: {ex.Message}");
                return null;
            }
        }

        private BitmapImage? ConvertIconToBitmapImage(Icon icon)
        {
            try
            {
                using (var bitmap = icon.ToBitmap())
                {
                    var bitmapImage = new BitmapImage();
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        stream.Position = 0;
                        bitmapImage.SetSource(stream.AsRandomAccessStream());
                    }
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting icon to BitmapImage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads icon by parsing the AppxManifest.xml file
        /// </summary>
        private BitmapImage? LoadIconFromManifest(InstalledAppInfo app)
        {
            try
            {
                var manifestPath = Path.Combine(app.InstallLocation, "AppxManifest.xml");
                if (!File.Exists(manifestPath)) return null;

                var doc = System.Xml.Linq.XDocument.Load(manifestPath);
                var ns = doc.Root?.GetDefaultNamespace();
                if (ns == null) return null;

                // Look for Application/VisualElements/@Square44x44Logo or Square150x150Logo
                var visualElements = doc.Root.Descendants(ns + "Application")
                    .Elements(ns + "VisualElements")
                    .FirstOrDefault();

                if (visualElements != null)
                {
                    // Try different logo attributes in order of preference
                    var logoAttributes = new[]
                    {
                        "Square44x44Logo",
                        "Square150x150Logo", 
                        "Square71x71Logo",
                        "Logo"
                    };

                    foreach (var attr in logoAttributes)
                    {
                        var logoPath = visualElements.Attribute(attr)?.Value;
                        if (!string.IsNullOrEmpty(logoPath))
                        {
                            var fullIconPath = Path.Combine(app.InstallLocation, logoPath);
                            if (File.Exists(fullIconPath))
                            {
                                return LoadBitmapImageFromFile(fullIconPath);
                            }

                            // Try with different extensions
                            var extensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
                            var baseIconPath = Path.ChangeExtension(fullIconPath, null);
                            foreach (var ext in extensions)
                            {
                                var iconWithExt = baseIconPath + ext;
                                if (File.Exists(iconWithExt))
                                {
                                    return LoadBitmapImageFromFile(iconWithExt);
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing manifest for {app.DisplayName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads icon from common Assets folder patterns
        /// </summary>
        private BitmapImage? LoadIconFromAssets(InstalledAppInfo app)
        {
            try
            {
                var assetsPath = Path.Combine(app.InstallLocation, "Assets");
                System.Diagnostics.Debug.WriteLine($"Checking assets path: {assetsPath}");
                
                if (!Directory.Exists(assetsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Assets folder does not exist for: {app.DisplayName}");
                    return null;
                }

                // Common icon file patterns
                var iconPatterns = new[]
                {
                    "Square44x44Logo*.png",
                    "Square150x150Logo*.png",
                    "Square71x71Logo*.png",
                    "StoreLogo*.png",
                    "Logo*.png",
                    "AppIcon*.png",
                    "*Logo*.png",
                    "*Icon*.png"
                };

                foreach (var pattern in iconPatterns)
                {
                    var files = Directory.GetFiles(assetsPath, pattern, SearchOption.AllDirectories);
                    System.Diagnostics.Debug.WriteLine($"Pattern '{pattern}' found {files.Length} files for {app.DisplayName}");
                    
                    if (files.Length > 0)
                    {
                        // Prefer files with higher resolution indicators
                        var bestFile = files
                            .OrderByDescending(f => GetIconPriority(Path.GetFileName(f)))
                            .FirstOrDefault();

                        if (bestFile != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Selected best icon file: {bestFile} for {app.DisplayName}");
                            var result = LoadBitmapImageFromFile(bestFile);
                            if (result != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Successfully loaded icon from assets for: {app.DisplayName}");
                                return result;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"No suitable icon files found in assets for: {app.DisplayName}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading from assets for {app.DisplayName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets priority score for icon files (higher = better)
        /// </summary>
        private int GetIconPriority(string fileName)
        {
            var lower = fileName.ToLowerInvariant();
            
            if (lower.Contains("square150x150")) return 100;
            if (lower.Contains("square71x71")) return 90;
            if (lower.Contains("square44x44")) return 80;
            if (lower.Contains("storelogo")) return 70;
            if (lower.Contains("logo")) return 60;
            if (lower.Contains("icon")) return 50;
            
            return 0;
        }

        /// <summary>
        /// Alternative approach using PackageManager for system apps
        /// </summary>
        private BitmapImage? LoadIconFromPackageManager(InstalledAppInfo app)
        {
            try
            {
                // This approach works better for some system apps
                var packageManager = new PackageManager();
                var packages = packageManager.FindPackagesForUser("", app.PackageFamilyName);
                var package = packages.FirstOrDefault();

                if (package?.Logo != null)
                {
                    var logoUri = package.Logo;
                    if (logoUri.IsFile)
                    {
                        return LoadBitmapImageFromFile(logoUri.LocalPath);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading via PackageManager for {app.DisplayName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a default icon for apps without icons
        /// </summary>
        private BitmapImage? LoadDefaultIcon()
        {
            try
            {
                // Create a simple default icon programmatically
                var writeableBitmap = new WriteableBitmap(44, 44);
                
                // You could also load a default icon from resources
                // return LoadBitmapImageFromResource("ms-appx:///Assets/DefaultAppIcon.png");
                
                return ConvertWriteableBitmapToBitmapImage(writeableBitmap);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Enhanced file loading with better error handling and format support
        /// </summary>
        private BitmapImage? LoadBitmapImageFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var bitmapImage = new BitmapImage();
                
                using (var fileStream = File.OpenRead(filePath))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        bitmapImage.SetSource(memoryStream.AsRandomAccessStream());
                    }
                }
                
                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading bitmap from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts WriteableBitmap to BitmapImage
        /// </summary>
        private BitmapImage? ConvertWriteableBitmapToBitmapImage(WriteableBitmap writeableBitmap)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                using (var stream = new InMemoryRandomAccessStream())
                {
                    // Use proper WriteableBitmap encoding
                    var encoder = BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream).GetAwaiter().GetResult();
                    
                    // Get pixel data from WriteableBitmap using proper buffer access
                    var pixelBuffer = writeableBitmap.PixelBuffer;
                    var pixelData = new byte[pixelBuffer.Length];
                    using (var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(pixelBuffer))
                    {
                        dataReader.ReadBytes(pixelData);
                    }
                    
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)writeableBitmap.PixelWidth,
                        (uint)writeableBitmap.PixelHeight,
                        96, 96,
                        pixelData);
                    
                    encoder.FlushAsync().GetAwaiter().GetResult();
                    
                    stream.Seek(0);
                    bitmapImage.SetSource(stream);
                }
                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting WriteableBitmap: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Searches installed apps by name
        /// </summary>
        public async Task<ObservableCollection<InstalledAppInfo>> SearchInstalledAppsAsync(string searchTerm)
        {
            var allApps = await GetAllInstalledAppsAsync();
            var filteredApps = allApps.Where(app =>
                app.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                app.Publisher.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            return new ObservableCollection<InstalledAppInfo>(filteredApps);
        }

        /// <summary>
        /// Diagnostic method to test icon loading for a specific app
        /// </summary>
        public async Task<bool> TestIconLoadingAsync(string appDisplayName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Testing icon loading for: {appDisplayName} ===");
                
                var allApps = await GetAllInstalledAppsAsync();
                var testApp = allApps.FirstOrDefault(app => 
                    app.DisplayName.Contains(appDisplayName, StringComparison.OrdinalIgnoreCase));
                
                if (testApp == null)
                {
                    System.Diagnostics.Debug.WriteLine($"App '{appDisplayName}' not found");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine($"Found app: {testApp.DisplayName}");
                System.Diagnostics.Debug.WriteLine($"Type: {testApp.Type}");
                System.Diagnostics.Debug.WriteLine($"Package Family: {testApp.PackageFamilyName}");
                System.Diagnostics.Debug.WriteLine($"Install Location: {testApp.InstallLocation}");
                System.Diagnostics.Debug.WriteLine($"Executable Path: {testApp.ExecutablePath}");
                
                // Test icon loading
                LoadAppIcon(testApp);
                
                bool hasIcon = testApp.Icon != null;
                System.Diagnostics.Debug.WriteLine($"Icon loaded: {hasIcon}");
                
                if (hasIcon)
                {
                    System.Diagnostics.Debug.WriteLine($"Icon dimensions: {testApp.Icon.PixelWidth}x{testApp.Icon.PixelHeight}");
                }
                
                System.Diagnostics.Debug.WriteLine($"=== Test completed for: {appDisplayName} ===");
                
                return hasIcon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing icon loading: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets summary of icon loading statistics
        /// </summary>
        public async Task<string> GetIconLoadingStatsAsync()
        {
            try
            {
                var apps = await GetInstalledAppsWithIconsAsync();
                
                int totalApps = apps.Count;
                int appsWithIcons = apps.Count(app => app.Icon != null);
                int uwpApps = apps.Count(app => app.Type == AppType.UWPApplication);
                int win32Apps = apps.Count(app => app.Type == AppType.Win32Application);
                int uwpWithIcons = apps.Count(app => app.Type == AppType.UWPApplication && app.Icon != null);
                int win32WithIcons = apps.Count(app => app.Type == AppType.Win32Application && app.Icon != null);
                
                var stats = $"Icon Loading Statistics:\n" +
                           $"Total Apps: {totalApps}\n" +
                           $"Apps with Icons: {appsWithIcons} ({(appsWithIcons * 100.0 / totalApps):F1}%)\n" +
                           $"UWP Apps: {uwpApps} (Icons: {uwpWithIcons}/{uwpApps})\n" +
                           $"Win32 Apps: {win32Apps} (Icons: {win32WithIcons}/{win32Apps})\n" +
                           $"Cache Size: {_iconCache.Count}";
                
                System.Diagnostics.Debug.WriteLine(stats);
                return stats;
            }
            catch (Exception ex)
            {
                var error = $"Error getting stats: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(error);
                return error;
            }
        }
    }
}
