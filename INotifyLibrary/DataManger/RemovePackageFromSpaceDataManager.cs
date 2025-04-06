using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class RemovePackageFromSpaceDataManager : INotifyBaseDataManager, IRemovePackageFromSpaceDataManager
    {
        public RemovePackageFromSpaceDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void RemovePackageFromSpaceAsync(RemovePackageFromSpaceRequest request, ICallback<RemovePackageFromSpaceResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                bool isSuccess = DBHandler.RemovePackageFromSpace(request.SpaceId, request.PackageId, request.UserId);
                RemovePackageFromSpaceResponse response = new RemovePackageFromSpaceResponse(isSuccess);
                ZResponse<RemovePackageFromSpaceResponse> zResponse = new ZResponse<RemovePackageFromSpaceResponse>(ResponseType.LocalStorage)
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
