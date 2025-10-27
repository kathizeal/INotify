using INotifyLibrary.DI;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System.Collections.ObjectModel;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IGetNotificationsByConditionDataManager
    {
        void GetNotificationsByConditionAsync(GetNotificationsByConditionRequest request, ICallback<GetNotificationsByConditionResponse> callback, CancellationTokenSource cts);
    }

    public interface IGetNotificationsByConditionPresenterCallback : ICallback<GetNotificationsByConditionResponse>
    {
    }

    public class GetNotificationsByConditionRequest : ZRequest
    {
        public SelectionTargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public bool IsPackageView { get; set; }

        public GetNotificationsByConditionRequest(SelectionTargetType targetType, string targetId, bool isPackageView, string userId) 
            : base(RequestType.LocalStorage, userId, default)
        {
            TargetType = targetType;
            TargetId = targetId;
            IsPackageView = isPackageView;
        }
    }

    public class GetNotificationsByConditionResponse
    {
        public SelectionTargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public bool IsPackageView { get; set; }
        public ObservableCollection<KToastBObj> Notifications { get; set; }
        public ObservableCollection<KPackageProfile> Packages { get; set; }
        public int TotalNotificationCount { get; set; }

        public GetNotificationsByConditionResponse(SelectionTargetType targetType, string targetId, bool isPackageView, 
            ObservableCollection<KToastBObj> notifications = null, ObservableCollection<KPackageProfile> packages = null)
        {
            TargetType = targetType;
            TargetId = targetId;
            IsPackageView = isPackageView;
            Notifications = notifications ?? new ObservableCollection<KToastBObj>();
            Packages = packages ?? new ObservableCollection<KPackageProfile>();
            TotalNotificationCount = Notifications?.Count ?? 0;
        }
    }

    public class GetNotificationsByCondition : UseCaseBase<GetNotificationsByConditionResponse>
    {
        private GetNotificationsByConditionRequest Request;
        private IGetNotificationsByConditionDataManager DataManager;

        public GetNotificationsByCondition(GetNotificationsByConditionRequest request, IGetNotificationsByConditionPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IGetNotificationsByConditionDataManager>();
        }

        protected override async void Action()
        {
            DataManager.GetNotificationsByConditionAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<GetNotificationsByConditionResponse>
        {
            private GetNotificationsByCondition Usecase;

            public UsecaseCallback(GetNotificationsByCondition usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<GetNotificationsByConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<GetNotificationsByConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<GetNotificationsByConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<GetNotificationsByConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}