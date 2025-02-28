using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class UpdateKToastDataManager : INotifyBaseDataManager, IUpdateKToastDataManager
    {
        public UpdateKToastDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void UpdateKToast(UpdateKToastRequest request, ICallback<UpdateKToastResponse> callback)
        {
            try
            {
                DBHandler.UpdateOrReplaceKToastNotification(request.KToastNotifications, request.UserId);
            }
            catch (Exception ex)
            {
                callback.OnError(default);
            }
        }
    }
}
