using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinLogger;
using WinSQLiteDBAdapter.Contract;

namespace INotifyLibrary.DBHandler
{
    public sealed partial class NotifyDBHandler : DBHandlerBase, INotifyDBHandler
    {
        public NotifyDBHandler(IDBAdapter dbAdapter) : base(dbAdapter) { }

        public List<Type> GetDBModels()
        {
            List<Type> dbModels = new()
            {
                typeof(KToastNotification),
                typeof(KPackageProfile),
                typeof(KSpace),
                typeof(KSpaceMapper),
                typeof(KCustomPriorityApp) // Add custom priority model
            };
            return dbModels;
        }

        public async Task InitializeDBAsync(string dbFolderPath, string dbuserId, string dbRefId = null)
        {
            try
            {
                await InitializeDBAdapterAsync(dbFolderPath).ConfigureAwait(false);

                IDBConnection DBConnection = await DBAdapter.CreateOrGetDBConnectionAsync(dbuserId, dbRefId).ConfigureAwait(false);
                DBConnection.CreateTables(GetDBModels());
            }

            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
            }
        }
        
        public override async System.Threading.Tasks.Task InitializeDBAdapterAsync(string dbFolderPath, bool isReadOnlyConn = false)
        {
            if (!DBAdapter.IsInitialized)
            {
                await DBAdapter.InitializeAsync(dbFolderPath, "ToastData").ConfigureAwait(false);

                IDBConnection serviceDbConn = DBAdapter.GetDBConnection();
                serviceDbConn.CreateTables(GetServiceDBModels());
            }
        }

        #region Custom Priority Methods

        /// <summary>
        /// Gets all apps with custom priorities for a user
        /// </summary>
        public IList<KCustomPriorityApp> GetCustomPriorityApps(string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                return dbConnection.Table<KCustomPriorityApp>()
                    .Where(x => x.UserId == userId)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return new List<KCustomPriorityApp>();
            }
        }

        /// <summary>
        /// Gets apps by specific priority level
        /// </summary>
        public IList<KCustomPriorityApp> GetAppsByPriority(Priority priority, string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                return dbConnection.Table<KCustomPriorityApp>()
                    .Where(x => x.UserId == INotifyConstant.CurrentUser && x.Priority == priority)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return new List<KCustomPriorityApp>();
            }
        }

        /// <summary>
        /// Adds or updates an app's custom priority
        /// </summary>
        public bool AddOrUpdateCustomPriorityApp(string packageName, string displayName, string publisher, Priority priority, string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                
                // Check if app already exists
                var existingApp = dbConnection.Table<KCustomPriorityApp>()
                    .FirstOrDefault(x => x.PackageName == packageName && x.UserId == INotifyConstant.CurrentUser);

                if (existingApp != null)
                {
                    // Update existing
                    existingApp.Priority = priority;
                    existingApp.DisplayName = displayName;
                    existingApp.Publisher = publisher;
                    existingApp.UpdatedTime = DateTimeOffset.Now;
                    
                    dbConnection.UpdateAll(new[] { existingApp });
                }
                else
                {
                    // Create new
                    var newApp = new KCustomPriorityApp
                    {
                        Id = Guid.NewGuid().ToString(),
                        PackageName = packageName,
                        DisplayName = displayName,
                        Publisher = publisher,
                        Priority = priority,
                        UserId = userId,
                        CreatedTime = DateTimeOffset.Now,
                        UpdatedTime = DateTimeOffset.Now,
                        IsEnabled = true
                    };
                    
                    dbConnection.InsertAll(new[] { newApp });
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Removes an app from custom priority
        /// </summary>
        public bool RemoveCustomPriorityApp(string packageId, string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                
                var result = dbConnection.Execute(
                    "DELETE FROM KCustomPriorityApp WHERE PackageId = ? AND UserId = ?",
                    packageId, userId);

                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets custom priority for a specific app
        /// </summary>
        public Priority? GetAppCustomPriority(string packageId, string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                
                var app = dbConnection.Table<KCustomPriorityApp>()
                    .FirstOrDefault(x => x.PackageName == packageId && x.UserId == userId);

                return app?.Priority;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return null;
            }
        }

        #endregion

        #region Enhanced Space Methods

        /// <summary>
        /// Gets packages in a specific space with their details
        /// </summary>
        public IList<KPackageProfile> GetPackagesBySpaceIdEnhanced(string spaceId, string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                
                // Get package IDs from space mapper
                var packageFamilyNames = dbConnection.Table<KSpaceMapper>()
                    .Where(x => x.SpaceId == spaceId)
                    .Select(x => x.PackageFamilyName)
                    .ToList();

                if (!packageFamilyNames.Any())
                    return new List<KPackageProfile>();

                // Get package profiles
                var packages = new List<KPackageProfile>();
                foreach (var packageFamilyName in packageFamilyNames)
                {
                    var package = dbConnection.Table<KPackageProfile>()
                        .FirstOrDefault(x => x.PackageFamilyName == packageFamilyName);
                    if (package != null)
                    {
                        packages.Add(package);
                    }
                }

                return packages;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return new List<KPackageProfile>();
            }
        }

        /// <summary>
        /// Adds a package to space with full package profile creation
        /// </summary>
        public bool AddPackageToSpaceEnhanced(string packageFamilyName, string spaceId, string displayName, string publisher, string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);

                dbConnection.RunInTransaction(() =>
                {
                    // Ensure package profile exists
                    var existingProfile = dbConnection.Table<KPackageProfile>()
                        .FirstOrDefault(x => x.PackageFamilyName == packageFamilyName);

                    if (existingProfile == null)
                    {
                        var newProfile = new KPackageProfile
                        {
                            PackageFamilyName = packageFamilyName,
                            AppDisplayName = displayName,
                            AppDescription = $"Application: {displayName}",
                            LogoFilePath = "",
                        };
                        dbConnection.InsertAll(new[] { newProfile });
                    }

                    // Check if mapping already exists
                    var existingMapper = dbConnection.Table<KSpaceMapper>()
                        .FirstOrDefault(x => x.PackageFamilyName == packageFamilyName && x.SpaceId == spaceId);

                    if (existingMapper == null)
                    {
                        var mapper = new KSpaceMapper
                        {
                            PackageFamilyName = packageFamilyName,
                            SpaceId = spaceId
                        };
                        dbConnection.InsertAll(new[] { mapper });
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets space statistics including app and notification counts
        /// </summary>
        public Dictionary<string, (int AppCount, int NotificationCount)> GetSpaceStatistics(string userId)
        {
            try
            {
                IDBConnection dbConnection = GetDBConnection(INotifyConstant.CurrentUser);
                var stats = new Dictionary<string, (int AppCount, int NotificationCount)>();

                var spaces = dbConnection.Table<KSpace>().ToList();
                
                foreach (var space in spaces)
                {
                    var appCount = dbConnection.Table<KSpaceMapper>()
                        .Count(x => x.SpaceId == space.SpaceId);
                    
                    // Get notification count for packages in this space
                    var packageIds = dbConnection.Table<KSpaceMapper>()
                        .Where(x => x.SpaceId == space.SpaceId)
                        .Select(x => x.PackageFamilyName)
                        .ToList();

                    var notificationCount = 0;
                    foreach (var packageId in packageIds)
                    {
                        notificationCount += dbConnection.Table<KToastNotification>()
                            .Count(x => x.PackageFamilyName == packageId);
                    }

                    stats[space.SpaceId] = (appCount, notificationCount);
                }

                return stats;
            }
            catch (Exception ex)
            {
                Logger.Error(LogManager.GetCallerInfo(), ex.Message);
                return new Dictionary<string, (int AppCount, int NotificationCount)>();
            }
        }

        #endregion
    }
}
