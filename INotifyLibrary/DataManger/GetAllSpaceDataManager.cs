using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class GetAllSpaceDataManager : INotifyBaseDataManager, IGetAllSpaceDataManager
    {
        public GetAllSpaceDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetAllSpacesAsync(GetAllSpaceRequest request, ICallback<GetAllSpaceResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                List<KSpace> spaces = DBHandler.GetAllSpaces(request.UserId).ToList();
                GetAllSpaceResponse response = new GetAllSpaceResponse(new ObservableCollection<KSpace>(spaces));
                ZResponse<GetAllSpaceResponse> zResponse = new ZResponse<GetAllSpaceResponse>(ResponseType.LocalStorage)
                { Data = response, Status = ResponseStatus.Success };

                callback.OnSuccess(zResponse);
            }
            catch (Exception ex)
            {
                callback.OnError(new ZError(ErrorType.Unknown, ex));
            }
        }
    }
}
