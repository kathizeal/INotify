using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
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
                if(CommonUtil.IsNonEmpty(spaces))
                {
                    spaces = CreateDefaultWorkSpace();
                    DBHandler.UpdateSpaces(spaces, request.UserId);
                }
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


        public List<KSpace> CreateDefaultWorkSpace()
        {
            List<KSpace> defaultWorkSpace = new();
            defaultWorkSpace.Add(INotifyUtil.GetDefaultSpace("1","Space1"));
            defaultWorkSpace.Add(INotifyUtil.GetDefaultSpace("2", "Space2"));
            defaultWorkSpace.Add(INotifyUtil.GetDefaultSpace("3", "Space3"));

            return defaultWorkSpace;
        }
    }
}
