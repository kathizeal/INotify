using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class AddPackageToSpaceDataManager : INotifyBaseDataManager, IAddPackageToSpaceDataManager
    {
        public AddPackageToSpaceDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void AddPackageToSpaceAsync(AddPackageToSpaceRequest request, ICallback<AddPackageToSpaceResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                KSpaceMapper mapper = new KSpaceMapper
                {
                    SpaceId = request.SpaceId,
                    PackageId = request.PackageId
                };

                bool isSuccess = DBHandler.AddPackageToSpace(mapper, request.UserId);
                AddPackageToSpaceResponse response = new AddPackageToSpaceResponse(isSuccess);
                ZResponse<AddPackageToSpaceResponse> zResponse = new ZResponse<AddPackageToSpaceResponse>(ResponseType.LocalStorage)
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
