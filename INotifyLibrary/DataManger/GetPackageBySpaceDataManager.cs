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
    public class GetPackageBySpaceDataManager : INotifyBaseDataManager, IGetPackageBySpaceDataManager
    {
        public GetPackageBySpaceDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetPackageBySpaceAsync(GetPackageBySpaceRequest request, ICallback<GetPackageBySpaceResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                List<KPackageProfile> packages = DBHandler.GetPackagesBySpaceId(request.SpaceId, request.UserId).ToList();
                GetPackageBySpaceResponse response = new GetPackageBySpaceResponse(request.SpaceId,new ObservableCollection<KPackageProfile>(packages));
                ZResponse<GetPackageBySpaceResponse> zResponse = new ZResponse<GetPackageBySpaceResponse>(ResponseType.LocalStorage)
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
