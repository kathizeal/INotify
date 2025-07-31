using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using INotifyLibrary.DBHandler.Contract;
using INotify.ViewModels;
using AppList;

namespace INotify.Services
{
    /// <summary>
    /// Service for managing custom priority assignments with database persistence
    /// </summary>
    public class CustomPriorityService
    {
        private readonly INotifyDBHandler _dbHandler;
        private readonly string _currentUserId;

        public CustomPriorityService(INotifyDBHandler dbHandler, string userId = "default")
        {
            _dbHandler = dbHandler ?? throw new ArgumentNullException(nameof(dbHandler));
            _currentUserId = userId;
        }

        #region Custom Priority Management

        /// <summary>
        /// Gets all custom priority apps from database
        /// </summary>
        public async Task<List<PriorityPackageViewModel>> GetCustomPriorityAppsAsync()
        {
            try
            {
                var customApps = await Task.Run(() => _dbHandler.GetCustomPriorityApps(_currentUserId));
                var viewModels = new List<PriorityPackageViewModel>();

                foreach (var app in customApps)
                {
                    var viewModel = new PriorityPackageViewModel
                    {
                        PackageId = app.PackageId,
                        DisplayName = app.DisplayName,
                        Publisher = app.Publisher,
                        Priority = app.Priority,
                        IsEnabled = app.IsEnabled,
                        NotificationCount = GetNotificationCountForApp(app.PackageId)
                    };
                    viewModels.Add(viewModel);
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting custom priority apps: {ex.Message}");
                return new List<PriorityPackageViewModel>();
            }
        }

        /// <summary>
        /// Gets apps by specific priority level
        /// </summary>
        public async Task<List<PriorityPackageViewModel>> GetAppsByPriorityAsync(Priority priority)
        {
            try
            {
                var customApps = await Task.Run(() => _dbHandler.GetAppsByPriority(priority, _currentUserId));
                var viewModels = new List<PriorityPackageViewModel>();

                foreach (var app in customApps)
                {
                    var viewModel = new PriorityPackageViewModel
                    {
                        PackageId = app.PackageId,
                        DisplayName = app.DisplayName,
                        Publisher = app.Publisher,
                        Priority = app.Priority,
                        IsEnabled = app.IsEnabled,
                        NotificationCount = GetNotificationCountForApp(app.PackageId)
                    };
                    viewModels.Add(viewModel);
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting apps by priority: {ex.Message}");
                return new List<PriorityPackageViewModel>();
            }
        }

        /// <summary>
        /// Adds or updates an app's custom priority
        /// </summary>
        public async Task<bool> SetAppPriorityAsync(string packageId, string displayName, string publisher, Priority priority)
        {
            try
            {
                return await Task.Run(() => _dbHandler.AddOrUpdateCustomPriorityApp(
                    packageId, displayName, publisher, priority, _currentUserId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting app priority: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes an app from custom priority
        /// </summary>
        public async Task<bool> RemoveAppPriorityAsync(string packageId)
        {
            try
            {
                return await Task.Run(() => _dbHandler.RemoveCustomPriorityApp(packageId, _currentUserId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing app priority: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets custom priority for a specific app
        /// </summary>
        public async Task<Priority?> GetAppPriorityAsync(string packageId)
        {
            try
            {
                return await Task.Run(() => _dbHandler.GetAppCustomPriority(packageId, _currentUserId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting app priority: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region All Applications with Priority Status

        /// <summary>
        /// Gets all installed applications with their current priority status
        /// </summary>
        public async Task<List<PriorityPackageViewModel>> GetAllApplicationsWithPriorityAsync()
        {
            try
            {
                // Get all installed apps
                var installedApps = await InstalledAppsService.GetAllInstalledAppsAsync();
                var customPriorityApps = await Task.Run(() => _dbHandler.GetCustomPriorityApps(_currentUserId));
                
                var priorityLookup = customPriorityApps.ToDictionary(
                    app => app.PackageId, 
                    app => app.Priority);

                var viewModels = new List<PriorityPackageViewModel>();

                foreach (var app in installedApps)
                {
                    if (string.IsNullOrWhiteSpace(app.DisplayName))
                        continue;

                    var packageId = GeneratePackageId(app);
                    var currentPriority = priorityLookup.GetValueOrDefault(packageId, Priority.None);

                    var viewModel = new PriorityPackageViewModel
                    {
                        PackageId = packageId,
                        DisplayName = app.DisplayName,
                        Publisher = app.Publisher ?? "Unknown",
                        Priority = currentPriority,
                        IsEnabled = true,
                        NotificationCount = GetNotificationCountForApp(packageId)
                    };

                    viewModels.Add(viewModel);
                }

                return viewModels.OrderBy(vm => vm.DisplayName).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all applications: {ex.Message}");
                return new List<PriorityPackageViewModel>();
            }
        }

        #endregion

        #region Space Management Integration

        /// <summary>
        /// Adds an app to a space with priority consideration
        /// </summary>
        public async Task<bool> AddAppToSpaceAsync(string packageId, string spaceId, string displayName, string publisher)
        {
            try
            {
                return await Task.Run(() => _dbHandler.AddPackageToSpaceEnhanced(
                    packageId, spaceId, displayName, publisher, _currentUserId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding app to space: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets space statistics
        /// </summary>
        public async Task<Dictionary<string, (int AppCount, int NotificationCount)>> GetSpaceStatisticsAsync()
        {
            try
            {
                return await Task.Run(() => _dbHandler.GetSpaceStatistics(_currentUserId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting space statistics: {ex.Message}");
                return new Dictionary<string, (int AppCount, int NotificationCount)>();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets notification count for a specific app
        /// </summary>
        private int GetNotificationCountForApp(string packageId)
        {
            try
            {
                var notifications = _dbHandler.GetKToastNotificationsByPackageId(packageId, _currentUserId);
                return notifications?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Generates a consistent package ID from app info
        /// </summary>
        private string GeneratePackageId(InstalledAppInfo app)
        {
            switch (app.Type)
            {
                case AppType.UWPApplication:
                    return !string.IsNullOrEmpty(app.PackageFamilyName) ? app.PackageFamilyName : app.Name;

                case AppType.Win32Application:
                    if (!string.IsNullOrEmpty(app.ExecutablePath))
                    {
                        return System.IO.Path.GetFileNameWithoutExtension(app.ExecutablePath);
                    }
                    return app.DisplayName.Replace(" ", "").Replace(".", "").Replace("-", "");

                default:
                    return app.Name;
            }
        }

        /// <summary>
        /// Initializes default spaces if they don't exist
        /// </summary>
        public async Task InitializeDefaultSpacesAsync()
        {
            try
            {
                var existingSpaces = await Task.Run(() => _dbHandler.GetAllSpaces(_currentUserId));
                
                if (!existingSpaces.Any())
                {
                    var defaultSpaces = new[]
                    {
                        new KSpace { SpaceId = "work", SpaceName = "Work & Productivity", SpaceDescription = "Work-related applications and tools" },
                        new KSpace { SpaceId = "personal", SpaceName = "Personal", SpaceDescription = "Personal applications and utilities" },
                        new KSpace { SpaceId = "entertainment", SpaceName = "Entertainment", SpaceDescription = "Games, media, and entertainment apps" }
                    };

                    await Task.Run(() => _dbHandler.UpdateSpaces(defaultSpaces, _currentUserId));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing default spaces: {ex.Message}");
            }
        }

        #endregion

        #region Priority Statistics

        /// <summary>
        /// Gets priority statistics for the dashboard
        /// </summary>
        public async Task<(int High, int Medium, int Low, int Total)> GetPriorityStatisticsAsync()
        {
            try
            {
                var allApps = await Task.Run(() => _dbHandler.GetCustomPriorityApps(_currentUserId));
                
                var high = allApps.Count(a => a.Priority == Priority.High);
                var medium = allApps.Count(a => a.Priority == Priority.Medium);
                var low = allApps.Count(a => a.Priority == Priority.Low);
                var total = allApps.Count;

                return (high, medium, low, total);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting priority statistics: {ex.Message}");
                return (0, 0, 0, 0);
            }
        }

        #endregion
    }
}