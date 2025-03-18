using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using System.Collections.ObjectModel;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IGetPackageBySpaceDataManager
    {
        void GetPackageBySpaceAsync(GetPackageBySpaceRequest request, ICallback<GetPackageBySpaceResponse> callback, CancellationTokenSource cts);
    }

    public interface IGetPackageBySpacePresenterCallback : ICallback<GetPackageBySpaceResponse>
    {
    }

    public class GetPackageBySpaceRequest : ZRequest
    {
        public string SpaceId { get; set; }

        public GetPackageBySpaceRequest(string spaceId, string userId) : base(RequestType.LocalStorage, userId, default)
        {
            SpaceId = spaceId;
        }
    }

    public class GetPackageBySpaceResponse
    {
        public string SpaceId { get; set; }
        public ObservableCollection<KPackageProfile> Packages { get; set; }

        public GetPackageBySpaceResponse(string spaceId,ObservableCollection<KPackageProfile> packages)
        {
            SpaceId = spaceId;
            Packages = packages;
        }
    }

    public class GetPackageBySpace : UseCaseBase<GetPackageBySpaceResponse>
    {
        private GetPackageBySpaceRequest Request;
        private IGetPackageBySpaceDataManager DataManager;

        public GetPackageBySpace(GetPackageBySpaceRequest request, IGetPackageBySpacePresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IGetPackageBySpaceDataManager>();
        }

        protected override async void Action()
        {
            DataManager.GetPackageBySpaceAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<GetPackageBySpaceResponse>
        {
            private GetPackageBySpace Usecase;

            public UsecaseCallback(GetPackageBySpace usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<GetPackageBySpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<GetPackageBySpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<GetPackageBySpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<GetPackageBySpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}
