using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IAddPackageToSpaceDataManager
    {
        void AddPackageToSpaceAsync(AddPackageToSpaceRequest request, ICallback<AddPackageToSpaceResponse> callback, CancellationTokenSource cts);
    }

    public interface IAddPackageToSpacePresenterCallback : ICallback<AddPackageToSpaceResponse>
    {
    }

    public class AddPackageToSpaceRequest : ZRequest
    {
        public string SpaceId { get; set; }
        public string PackageId { get; set; }

        public AddPackageToSpaceRequest(string spaceId, string packageId, string userId) : base(RequestType.LocalStorage, userId, default)
        {
            SpaceId = spaceId;
            PackageId = packageId;
        }
    }

    public class AddPackageToSpaceResponse
    {
        public bool IsSuccess { get; set; }

        public AddPackageToSpaceResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
    }

    public class AddPackageToSpace : UseCaseBase<AddPackageToSpaceResponse>
    {
        private AddPackageToSpaceRequest Request;
        private IAddPackageToSpaceDataManager DataManager;

        public AddPackageToSpace(AddPackageToSpaceRequest request, IAddPackageToSpacePresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IAddPackageToSpaceDataManager>();
        }

        protected override async void Action()
        {
            DataManager.AddPackageToSpaceAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<AddPackageToSpaceResponse>
        {
            private AddPackageToSpace Usecase;

            public UsecaseCallback(AddPackageToSpace usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<AddPackageToSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<AddPackageToSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<AddPackageToSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<AddPackageToSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}
