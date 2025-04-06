using INotifyLibrary.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IRemovePackageFromSpaceDataManager
    {
        void RemovePackageFromSpaceAsync(RemovePackageFromSpaceRequest request, ICallback<RemovePackageFromSpaceResponse> callback, CancellationTokenSource cts);
    }

    public interface IRemovePackageFromSpacePresenterCallback : ICallback<RemovePackageFromSpaceResponse>
    {
    }

    public class RemovePackageFromSpaceRequest : ZRequest
    {
        public string SpaceId { get; set; }
        public string PackageId { get; set; }

        public RemovePackageFromSpaceRequest(string spaceId, string packageId, string userId) : base(RequestType.LocalStorage, userId, default)
        {
            SpaceId = spaceId;
            PackageId = packageId;
        }
    }

    public class RemovePackageFromSpaceResponse
    {
        public bool IsSuccess { get; set; }

        public RemovePackageFromSpaceResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
    }

    public class RemovePackageFromSpace : UseCaseBase<RemovePackageFromSpaceResponse>
    {
        private RemovePackageFromSpaceRequest Request;
        private IRemovePackageFromSpaceDataManager DataManager;

        public RemovePackageFromSpace(RemovePackageFromSpaceRequest request, IRemovePackageFromSpacePresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IRemovePackageFromSpaceDataManager>();
        }

        protected override async void Action()
        {
            DataManager.RemovePackageFromSpaceAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<RemovePackageFromSpaceResponse>
        {
            private RemovePackageFromSpace Usecase;

            public UsecaseCallback(RemovePackageFromSpace usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<RemovePackageFromSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}
