using INotify.KToastDI;
using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace INotify.Services
{
    /// <summary>
    /// Cache service for notification filtering to improve real-time notification processing performance
    /// </summary>
    public class NotificationFilterCacheService : IDisposable
    {
        private static readonly Lazy<NotificationFilterCacheService> _instance = new(() => new NotificationFilterCacheService());
        public static NotificationFilterCacheService Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, Priority> _packagePriorityCache = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _spacePackageCache = new();
        private readonly object _cacheUpdateLock = new object();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheValidityDuration = TimeSpan.FromMinutes(5); // Cache for 5 minutes

        private INotifyDBHandler _dbHandler;

        private NotificationFilterCacheService()
        {
            try
            {
                // Initialize DB handler through DI
                _dbHandler = INotifyLibraryDIServiceProvider.Instance.GetService<INotifyDBHandler>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing NotificationFilterCacheService: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a package belongs to a specific priority category
        /// </summary>
        public bool IsPackageInPriorityCategory(string packageFamilyName, string priorityLevel)
        {
            try
            {
                if (string.IsNullOrEmpty(packageFamilyName) || string.IsNullOrEmpty(priorityLevel))
                    return false;

                // Parse priority level
                if (!Enum.TryParse<Priority>(priorityLevel, true, out var targetPriority))
                    return false;

                // Ensure cache is up to date
                EnsurePriorityCacheUpdated();

                // Check cache
                if (_packagePriorityCache.TryGetValue(packageFamilyName, out var packagePriority))
                {
                    return packagePriority == targetPriority;
                }

                // If not in cache, package doesn't have a custom priority, so it doesn't belong to any specific category
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking package priority: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a package belongs to a specific space
        /// </summary>
        public bool IsPackageInSpace(string packageFamilyName, string spaceId)
        {
            try
            {
                if (string.IsNullOrEmpty(packageFamilyName) || string.IsNullOrEmpty(spaceId))
                    return false;

                // Ensure cache is up to date
                EnsureSpaceCacheUpdated();

                // Check cache
                if (_spacePackageCache.TryGetValue(spaceId, out var packagesInSpace))
                {
                    return packagesInSpace.Contains(packageFamilyName);
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking package space membership: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Invalidates the cache to force a refresh on next access
        /// </summary>
        public void InvalidateCache()
        {
            lock (_cacheUpdateLock)
            {
                _packagePriorityCache.Clear();
                _spacePackageCache.Clear();
                _lastCacheUpdate = DateTime.MinValue;
                Debug.WriteLine("NotificationFilterCacheService cache invalidated");
            }
        }

        /// <summary>
        /// Ensures the priority cache is up to date
        /// </summary>
        private void EnsurePriorityCacheUpdated()
        {
            lock (_cacheUpdateLock)
            {
                if (IsCacheValid() || _dbHandler == null)
                    return;

                try
                {
                    // Load custom priority apps from database
                    var priorityApps = _dbHandler.GetCustomPriorityApps(INotifyConstant.CurrentUser);
                    
                    // Clear existing cache
                    _packagePriorityCache.Clear();

                    // Populate cache
                    foreach (var app in priorityApps)
                    {
                        if (!string.IsNullOrEmpty(app.PackageName))
                        {
                            _packagePriorityCache.TryAdd(app.PackageName, app.Priority);
                        }
                    }

                    Debug.WriteLine($"Updated priority cache with {priorityApps.Count} entries");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating priority cache: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Ensures the space cache is up to date
        /// </summary>
        private void EnsureSpaceCacheUpdated()
        {
            lock (_cacheUpdateLock)
            {
                if (IsCacheValid() || _dbHandler == null)
                    return;

                try
                {
                    // Load all spaces from database
                    var spaces = _dbHandler.GetAllSpaceMappers(INotifyConstant.CurrentUser);
                    
                    // Clear existing cache
                    _spacePackageCache.Clear();

                    // Populate cache for each space
                    foreach (var space in spaces)
                    {
                        var packagesInSpace = _dbHandler.GetPackagesBySpaceId(space.SpaceId, INotifyConstant.CurrentUser);
                        var packageFamilyNames = new HashSet<string>(
                            packagesInSpace.Where(p => !string.IsNullOrEmpty(p.PackageFamilyName))
                                          .Select(p => p.PackageFamilyName)
                        );

                        _spacePackageCache.TryAdd(space.SpaceId, packageFamilyNames);
                    }

                    _lastCacheUpdate = DateTime.Now;
                    Debug.WriteLine($"Updated space cache with {spaces.Count} spaces");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating space cache: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if the cache is still valid based on the last update time
        /// </summary>
        private bool IsCacheValid()
        {
            return DateTime.Now - _lastCacheUpdate < _cacheValidityDuration;
        }

        /// <summary>
        /// Adds or updates a package priority in the cache
        /// </summary>
        public void UpdatePackagePriorityInCache(string packageName, Priority priority)
        {
            if (!string.IsNullOrEmpty(packageName))
            {
                _packagePriorityCache.AddOrUpdate(packageName, priority, (key, oldValue) => priority);
                Debug.WriteLine($"Updated priority cache for package {packageName}: {priority}");
            }
        }

        /// <summary>
        /// Adds a package to a space in the cache
        /// </summary>
        public void AddPackageToSpaceInCache(string packageFamilyName, string spaceId)
        {
            if (!string.IsNullOrEmpty(packageFamilyName) && !string.IsNullOrEmpty(spaceId))
            {
                _spacePackageCache.AddOrUpdate(spaceId, 
                    new HashSet<string> { packageFamilyName },
                    (key, existingSet) => 
                    {
                        existingSet.Add(packageFamilyName);
                        return existingSet;
                    });

                Debug.WriteLine($"Added package {packageFamilyName} to space {spaceId} in cache");
            }
        }

        /// <summary>
        /// Removes a package from a space in the cache
        /// </summary>
        public void RemovePackageFromSpaceInCache(string packageFamilyName, string spaceId)
        {
            if (!string.IsNullOrEmpty(packageFamilyName) && !string.IsNullOrEmpty(spaceId))
            {
                if (_spacePackageCache.TryGetValue(spaceId, out var packagesInSpace))
                {
                    packagesInSpace.Remove(packageFamilyName);
                    Debug.WriteLine($"Removed package {packageFamilyName} from space {spaceId} in cache");
                }
            }
        }

        public void Dispose()
        {
            _packagePriorityCache.Clear();
            _spacePackageCache.Clear();
        }
    }
}