using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System.Collections.ObjectModel;
using System.Globalization;
using WinSQLiteDBAdapter.Contract;

namespace INotifyLibrary.DBHandler.Contract
{
    public interface INotifyDBHandler : IDBHandler
    {
        Task InitializeDBAsync(string dbFolderPath, string dbuserId, string dbRefId = null);
        List<Type> GetDBModels();
        List<Type> GetServiceDBModels();
        IList<KToastNotification> GetToastNotificationByUserId(string userId);
        IList<KToastNotification> GetKToastNotificationsByPackageId(string packageId, string userId);


        #region PackageProfile

        KPackageProfile GetPackageProfile(string packageId, string userId);
        IList<KPackageProfile> GetKPackageProfiles(string userId);


        #endregion
        void UpdateOrReplaceKToastNotification(ObservableCollection<KToastNotification> toastNotifications, string userId);
        void UpdateOrReplaceKToastNotification(KToastBObj toastData, string userId);

        void UpdateKPackageProfileFromAddition(KPackageProfile packageProfile, string userId);

        IList<KPackageProfile> GetPackagesBySpaceId(string spaceId, string userId);

        IList<KSpace> GetAllSpaces(string userId);
        void UpdateSpaces(IEnumerable<KSpace> kSpaces, string userId);

        bool AddPackageToSpace(KSpaceMapper mapper, string userId);

        bool RemovePackageFromSpace(string spaceId, string packageId, string userId);

        #region Custom Priority Methods

        /// <summary>
        /// Gets all apps with custom priorities for a user
        /// </summary>
        IList<KCustomPriorityApp> GetCustomPriorityApps(string userId);

        /// <summary>
        /// Gets apps by specific priority level
        /// </summary>
        IList<KCustomPriorityApp> GetAppsByPriority(Priority priority, string userId);

        /// <summary>
        /// Adds or updates an app's custom priority
        /// </summary>
        bool AddOrUpdateCustomPriorityApp(string packageId, string displayName, string publisher, Priority priority, string userId);

        /// <summary>
        /// Removes an app from custom priority
        /// </summary>
        bool RemoveCustomPriorityApp(string packageId, string userId);

        /// <summary>
        /// Gets custom priority for a specific app
        /// </summary>
        Priority? GetAppCustomPriority(string packageId, string userId);

        #endregion

        #region Enhanced Space Methods

        /// <summary>
        /// Gets packages in a specific space with their details
        /// </summary>
        IList<KPackageProfile> GetPackagesBySpaceIdEnhanced(string spaceId, string userId);

        /// <summary>
        /// Adds a package to space with full package profile creation
        /// </summary>
        bool AddPackageToSpaceEnhanced(string packageId, string spaceId, string displayName, string publisher, string userId);

        /// <summary>
        /// Gets space statistics including app and notification counts
        /// </summary>
        Dictionary<string, (int AppCount, int NotificationCount)> GetSpaceStatistics(string userId);

        #endregion

        #region Feedback Methods

        /// <summary>
        /// Submits user feedback to the database
        /// </summary>
        bool SubmitFeedback(string title, string message, FeedbackCategory category, string email, string userId, string appVersion, string osVersion);

        /// <summary>
        /// Gets all feedback for a user
        /// </summary>
        IList<KFeedback> GetUserFeedback(string userId);

        /// <summary>
        /// Gets feedback by category for a user
        /// </summary>
        IList<KFeedback> GetFeedbackByCategory(FeedbackCategory category, string userId);

        /// <summary>
        /// Updates feedback status
        /// </summary>
        bool UpdateFeedbackStatus(string feedbackId, FeedbackStatus status, string userId);

        #endregion
    }

}
