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
    public class GetKToastDataManager : INotifyBaseDataManager, IGetKToastsDataManager
    {
        public GetKToastDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetKToastsAsync(GetKToastsRequest request, ICallback<GetKToastsResponse> callback, CancellationTokenSource cts)
        {
            try
            {

                var list = DBHandler.GetKToastAllNotifications(request.UserId);
                GetKToastsResponse? response = new GetKToastsResponse(request.Id, new ObservableCollection<KToastNotification>(list));
                ZResponse<GetKToastsResponse> zResponse = new ZResponse<GetKToastsResponse>(ResponseType.LocalStorage)
                { Data = response , Status = ResponseStatus.Success};


                callback.OnSuccess(zResponse);
                //return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                callback.OnError(default);
            }
        }
    }
}
