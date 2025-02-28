using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSQLiteDBAdapter.Contract;

namespace INotifyLibrary.DBHandler.Contract
{
    public interface INotifyDBHandler : IDBHandler
    {
        Task InitializeDBAsync(string dbFolderPath, string dbuserId, string dbRefId = null);
        List<Type> GetDBModels();
        List<Type> GetServiceDBModels();
        IList<KToastNotification> GetKToastAllNotifications(string userId);
        void UpdateOrReplaceKToastNotification(ObservableCollection<KToastNotification> toastNotifications, string userId);

    }
}
