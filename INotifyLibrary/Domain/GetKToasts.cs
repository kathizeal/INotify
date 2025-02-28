using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IGetKToastsDataManager
    {
        void GetKToastsAsync(GetKToastsRequest request, ICallback<GetKToastsResponse> callback, CancellationTokenSource cts);
    }

    public interface IGetKToastPresenterCalback : ICallback<GetKToastsResponse>
    {
    }

    public class GetKToastsRequest : ZRequest
    {
        public ViewType ViewType { get; set; }
        public string Id { get; set; }

        public GetKToastsRequest(ViewType viewType, string id, string userId) : base(RequestType.LocalStorage, userId, default)
        {
            ViewType = viewType;
            Id = id;
        }
    }

    public class GetKToastsResponse
    {
        public ObservableCollection<KToastNotification> KToastNotifications { get; set; }
        public string Id { get; set; }
        public GetKToastsResponse(string id, ObservableCollection<KToastNotification> kToastNotifications)
        {
            KToastNotifications = kToastNotifications;
            Id = id;
        }

    }


    public class GetKToasts : UseCaseBase<GetKToastsResponse>
    {
        private GetKToastsRequest Request;
        private IGetKToastsDataManager DataManager;

        public GetKToasts(GetKToastsRequest request, IGetKToastPresenterCalback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IGetKToastsDataManager>();
        }

        protected override async void Action()
        {
            DataManager.GetKToastsAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<GetKToastsResponse>
        {
            private GetKToasts Usecase;

            public UsecaseCallback(GetKToasts usecase)
            {
                Usecase = usecase;
            }
            public override void OnSuccess(ZResponse<GetKToastsResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }
            public override void OnProgress(ZResponse<GetKToastsResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<GetKToastsResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<GetKToastsResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}
