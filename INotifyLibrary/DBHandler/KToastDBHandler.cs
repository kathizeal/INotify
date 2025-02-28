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
        public IList<KToastNotification> GetKToastAllNotifications(string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            return dBConnection.Table<KToastNotification>().ToList();
        }
        public void UpdateOrReplaceKToastNotification(ObservableCollection<KToastNotification> toastNotifications, string userId)
        {
            IDBConnection dBConnection = DBAdapter.GetDBConnection(userId);
            dBConnection.InsertOrReplaceAll(toastNotifications);
        }

    }
}
