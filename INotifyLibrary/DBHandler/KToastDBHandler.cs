using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSQLiteDBAdapter.Contract;
using WinSQLiteDBAdapter.Model.Entity;

namespace INotifyLibrary.DBHandler
{
    public sealed partial class NotifyDBHandler     
    {
        public IList<KToastNotification> GetToastNotificationByUserId(string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KToastNotification>().ToList();
        }

        public IList<KToastNotification> GetKToastNotificationsByPackageId(string packageId, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KToastNotification>().Where(x => x.PackageFamilyName == packageId).ToList();
        }

        public void UpdateOrReplaceKToastNotification(ObservableCollection<KToastNotification> toastNotifications, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            dBConnection.InsertOrReplaceAll(toastNotifications);
        }

        public void UpdateOrReplaceKToastNotification(KToastBObj toastData, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);          
            dBConnection.RunInTransaction(() => {
                if (toastData != null)
                {
                    if (toastData.NotificationData != null)
                    {
                        dBConnection.InsertOrReplace(toastData.NotificationData, typeof(KToastNotification));
                    }
                    if (toastData.ToastPackageProfile != null)
                    {
                        dBConnection.InsertOrReplace(toastData.ToastPackageProfile, typeof(KPackageProfile));
                    }
                }
            });
          
        }


        public KPackageProfile GetPackageProfile(string packageFamilyName, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KPackageProfile>().FirstOrDefault(x => x.PackageFamilyName == packageFamilyName);
        }

        public IList<KPackageProfile> GetKPackageProfiles(string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KPackageProfile>().ToList();
        }


        public IList<KPackageProfile> GetPackagesBySpaceId(string spaceId, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            var packageFamilyNames = dBConnection.Table<KSpaceMapper>()
                                          .Where(x => x.SpaceId == spaceId)
                                          .Select(x => x.PackageFamilyName)
                                          .ToHashSet<string>();

            return dBConnection.Table<KPackageProfile>()
                               .Where(x => packageFamilyNames.Contains(x.PackageFamilyName))
                               .ToList();
        }

        public IList<KSpaceMapper> GetAllSpaceMappers(string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KSpaceMapper>().ToList();
        }

        public bool AddPackageToSpace(KSpaceMapper mapper, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            dBConnection.InsertOrReplace(mapper);
            return true;
        }

        public void UpdateKPackageProfileFromAddition(KPackageProfile packageProfile, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            dBConnection.InsertOrReplace(packageProfile);
        }   

        public void UpdateSpaces(IEnumerable<KSpace> spaces, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            dBConnection.InsertOrReplaceAll(spaces);
        }

        public bool RemovePackageFromSpace(string spaceId, string packageId, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            var spaceMapper = dBConnection.Table<KSpaceMapper>()
                                          .FirstOrDefault(x => x.SpaceId == spaceId && x.PackageFamilyName == packageId);

            if (spaceMapper != null)
            {
                dBConnection.Delete(spaceMapper);
                return true;
            }
            return false;
        }
    }
}
