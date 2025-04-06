using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
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

        IList<KPackageProfile> GetPackagesBySpaceId(string spaceId, string userId);

        IList<KSpace> GetAllSpaces(string userId);
        void UpdateSpaces(IEnumerable<KSpace> kSpaces, string userId);

        bool AddPackageToSpace(KSpaceMapper mapper, string userId);

        bool RemovePackageFromSpace(string spaceId, string packageId, string userId);
    }

}
