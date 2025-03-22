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
            return dBConnection.Table<KToastNotification>().Where(x => x.PackageId == packageId).ToList();
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


        public KPackageProfile GetPackageProfile(string packageId, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KPackageProfile>().FirstOrDefault(x => x.PackageId == packageId);
        }

        public IList<KPackageProfile> GetKPackageProfiles(string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KPackageProfile>().ToList();
        }


        public IList<KPackageProfile> GetPackagesBySpaceId(string spaceId, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            var packageIds = dBConnection.Table<KSpaceMapper>()
                                          .Where(x => x.SpaceId == spaceId)
                                          .Select(x => x.PackageId)
                                          .ToList();

            return dBConnection.Table<KPackageProfile>()
                               .Where(x => packageIds.Contains(x.PackageId))
                               .ToList();
        }

        public IList<KSpace> GetAllSpaces(string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KSpace>().ToList();
        }

        public bool AddPackageToSpace(KSpaceMapper mapper)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection();
            dBConnection.InsertOrReplace(mapper);
            return true;
        }


        public void UpdateSpaces(IEnumerable<KSpace> spaces, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            dBConnection.InsertOrReplaceAll(spaces);
        }
    }
}
