using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using System.Collections.ObjectModel;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IGetAllKPackageProfilesDataManager
    {
        void GetAllKPackageProfilesAsync(GetAllKPackageProfilesRequest request, ICallback<GetAllKPackageProfilesResponse> callback, CancellationTokenSource cts);
    }

    public interface IGetAllKPackageProfilesPresenterCallback : ICallback<GetAllKPackageProfilesResponse>
    {
    }

    public class GetAllKPackageProfilesRequest : ZRequest
    {
        public GetAllKPackageProfilesRequest(string userId) : base(RequestType.LocalStorage, userId, default)
        {
        }
    }

    public class GetAllKPackageProfilesResponse
    {
        public ObservableCollection<KPackageProfile> KPackageProfiles { get; set; }

        public GetAllKPackageProfilesResponse(ObservableCollection<KPackageProfile> kPackageProfiles)
        {
            KPackageProfiles = kPackageProfiles;
        }
    }

    public class GetAllKPackageProfiles : UseCaseBase<GetAllKPackageProfilesResponse>
    {
        private GetAllKPackageProfilesRequest Request;
        private IGetAllKPackageProfilesDataManager DataManager;

        public GetAllKPackageProfiles(GetAllKPackageProfilesRequest request, IGetAllKPackageProfilesPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IGetAllKPackageProfilesDataManager>();
        }

        protected override async void Action()
        {
            DataManager.GetAllKPackageProfilesAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<GetAllKPackageProfilesResponse>
        {
            private GetAllKPackageProfiles Usecase;

            public UsecaseCallback(GetAllKPackageProfiles usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<GetAllKPackageProfilesResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<GetAllKPackageProfilesResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<GetAllKPackageProfilesResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<GetAllKPackageProfilesResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}
