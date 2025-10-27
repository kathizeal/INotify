using INotifyLibrary.DI;
using System;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IClearPackageNotificationsDataManager
    {
        void ClearPackageNotificationsAsync(ClearPackageNotificationsRequest request, ICallback<ClearPackageNotificationsResponse> callback, CancellationTokenSource cts);
    }

    public interface IClearPackageNotificationsPresenterCallback : ICallback<ClearPackageNotificationsResponse>
    {
    }

    public class ClearPackageNotificationsRequest : ZRequest
    {
        public string PackageFamilyName { get; set; }

        public ClearPackageNotificationsRequest(string packageFamilyName, string userId) : base(RequestType.LocalStorage, userId, default)
        {
            PackageFamilyName = packageFamilyName;
        }
    }

    public class ClearPackageNotificationsResponse
    {
        public bool IsSuccess { get; set; }
        public int ClearedCount { get; set; }
        public string PackageFamilyName { get; set; }

        public ClearPackageNotificationsResponse(bool isSuccess, int clearedCount, string packageFamilyName)
        {
            IsSuccess = isSuccess;
            ClearedCount = clearedCount;
            PackageFamilyName = packageFamilyName;
        }
    }

    public class ClearPackageNotifications : UseCaseBase<ClearPackageNotificationsResponse>
    {
        private ClearPackageNotificationsRequest Request;
        private IClearPackageNotificationsDataManager DataManager;

        public ClearPackageNotifications(ClearPackageNotificationsRequest request, IClearPackageNotificationsPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IClearPackageNotificationsDataManager>();
        }

        protected override async void Action()
        {
            DataManager.ClearPackageNotificationsAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<ClearPackageNotificationsResponse>
        {
            private ClearPackageNotifications Usecase;

            public UsecaseCallback(ClearPackageNotifications usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<ClearPackageNotificationsResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<ClearPackageNotificationsResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<ClearPackageNotificationsResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<ClearPackageNotificationsResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}