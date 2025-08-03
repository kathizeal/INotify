using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

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

        /// <summary>
        /// Gets all installed applications (Win32 + UWP)
        /// </summary>
        public async Task<ObservableCollection<InstalledAppInfo>> GetAllInstalledAppsAsync()
        {
            var apps = new ObservableCollection<InstalledAppInfo>();

            // Get Win32 applications
            var win32Apps = await GetWin32AppsAsync();
            foreach (var app in win32Apps)
            {
                apps.Add(app);
            }

            // Get UWP applications
            var uwpApps = await GetUWPAppsAsync();
            foreach (var app in uwpApps)
            {
                apps.Add(app);
            }

            return apps;
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
                            // Skip system packages and frameworks
                            if (package.IsFramework ||
                                package.SignatureKind == PackageSignatureKind.System ||
                                string.IsNullOrEmpty(package.DisplayName))
                                continue;

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
        /// Gets applications with icons loaded
        /// </summary>
        public async Task<ObservableCollection<InstalledAppInfo>> GetInstalledAppsWithIconsAsync()
        {
            var apps = await GetAllInstalledAppsAsync();

            await Task.Run(() =>
            {
                foreach (var app in apps)
                {
                    LoadAppIcon(app);
                }
            });

            return apps;
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
                if (app.Type == AppType.UWPApplication)
                {
                    LoadUWPIcon(app);
                }
                else if (!string.IsNullOrEmpty(app.ExecutablePath) && File.Exists(app.ExecutablePath))
                {
                    LoadWin32Icon(app);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icon for {app.DisplayName}: {ex.Message}");
            }
        }

        private void LoadWin32Icon(InstalledAppInfo app)
        {
            try
            {
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, app.ExecutablePath, 0);
                if (hIcon != IntPtr.Zero)
                {
                    using (var icon = Icon.FromHandle(hIcon))
                    {
                        app.Icon = ConvertIconToBitmapImage(icon);
                    }
                    DestroyIcon(hIcon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting Win32 icon: {ex.Message}");
            }
        }

        private void LoadUWPIcon(InstalledAppInfo app)
        {
            try
            {
                if (!string.IsNullOrEmpty(app.InstallLocation))
                {
                    var manifestPath = Path.Combine(app.InstallLocation, "AppxManifest.xml");
                    if (File.Exists(manifestPath))
                    {
                        // Look for common icon files
                        var iconFiles = new[] { "Assets\\StoreLogo.png", "Assets\\Square44x44Logo.png", "Assets\\Square150x150Logo.png" };
                        foreach (var iconFile in iconFiles)
                        {
                            var iconPath = Path.Combine(app.InstallLocation, iconFile);
                            if (File.Exists(iconPath))
                            {
                                app.Icon = LoadBitmapImageFromFile(iconPath);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading UWP icon: {ex.Message}");
            }
        }

        private BitmapImage ConvertIconToBitmapImage(Icon icon)
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
            catch
            {
                return null;
            }
        }

        private BitmapImage LoadBitmapImageFromFile(string filePath)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                using (var stream = File.OpenRead(filePath))
                {
                    bitmapImage.SetSource(stream.AsRandomAccessStream());
                }
                return bitmapImage;
            }
            catch
            {
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
    }
}
