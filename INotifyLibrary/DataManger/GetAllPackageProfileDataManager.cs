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
    public class GetAllKPackageProfilesDataManager : INotifyBaseDataManager, IGetAllKPackageProfilesDataManager
    {
        public GetAllKPackageProfilesDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetAllKPackageProfilesAsync(GetAllKPackageProfilesRequest request, ICallback<GetAllKPackageProfilesResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                List<KPackageProfile> packageProfiles = DBHandler.GetKPackageProfiles(request.UserId).ToList();
                GetAllKPackageProfilesResponse response = new GetAllKPackageProfilesResponse(new ObservableCollection<KPackageProfile>(packageProfiles));
                ZResponse<GetAllKPackageProfilesResponse> zResponse = new ZResponse<GetAllKPackageProfilesResponse>(ResponseType.LocalStorage)
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
