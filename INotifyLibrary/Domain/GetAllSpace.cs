using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using System.Collections.ObjectModel;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IGetAllSpaceDataManager
    {
        void GetAllSpacesAsync(GetAllSpaceRequest request, ICallback<GetAllSpaceResponse> callback, CancellationTokenSource cts);
    }

    public interface IGetAllSpacePresenterCallback : ICallback<GetAllSpaceResponse>
    {
    }

    public class GetAllSpaceRequest : ZRequest
    {
        public GetAllSpaceRequest(string userId) : base(RequestType.LocalStorage, userId, default)
        {
        }
    }

    public class GetAllSpaceResponse
    {
        public ObservableCollection<KSpace> Spaces { get; set; }

        public GetAllSpaceResponse(ObservableCollection<KSpace> spaces)
        {
            Spaces = spaces;
        }
    }

    public class GetAllSpace : UseCaseBase<GetAllSpaceResponse>
    {
        private GetAllSpaceRequest Request;
        private IGetAllSpaceDataManager DataManager;

        public GetAllSpace(GetAllSpaceRequest request, IGetAllSpacePresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IGetAllSpaceDataManager>();
        }

        protected override async void Action()
        {
            DataManager.GetAllSpacesAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<GetAllSpaceResponse>
        {
            private GetAllSpace Usecase;

            public UsecaseCallback(GetAllSpace usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<GetAllSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<GetAllSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<GetAllSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<GetAllSpaceResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}
