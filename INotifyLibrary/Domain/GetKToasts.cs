using INotifyLibrary.DI;
using INotifyLibrary.Model;
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
        public NotificatioRequestType NotificationRequestType { get; set; }
        public ViewType ViewType { get; set; }
        public string PackageId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        
        // Filter properties
        public string SearchKeyword { get; set; }
        public string FilterByApp { get; set; }
        public DateTimeOffset? FilterDate { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }

        public GetKToastsRequest(
            NotificatioRequestType notificationRequestType, 
            ViewType viewType, 
            string packageId, 
            string userId, 
            int skip = 0, 
            int take = 50, 
            string searchKeyword = null, 
            string filterByApp = null, 
            DateTimeOffset? filterDate = null, 
            DateTimeOffset? fromDate = null, 
            DateTimeOffset? toDate = null) 
            : base(RequestType.LocalStorage, userId, default)
        {
            NotificationRequestType = notificationRequestType;
            ViewType = viewType;
            PackageId = packageId;
            Skip = skip;
            Take = take;
            SearchKeyword = searchKeyword;
            FilterByApp = filterByApp;
            FilterDate = filterDate;
            FromDate = fromDate;
            ToDate = toDate;
        }
    }

    public class GetKToastsResponse
    {
        public NotificatioRequestType NotificationRequestType { get; set; }
        public ViewType ViewType { get; set; }
        public ObservableCollection<KToastBObj> ToastDataNotifications { get; set; }
        public string PackageId { get; set; }
        public int TotalCount { get; set; }
        public bool HasMoreData { get; set; }
        public int CurrentPage { get; set; }

        public GetKToastsResponse(string packageId, NotificatioRequestType notificationRequestType, ViewType viewType, ObservableCollection<KToastBObj> toastDataNotifications, int totalCount = 0, bool hasMoreData = false, int currentPage = 0)
        {
            NotificationRequestType = notificationRequestType;
            ViewType = viewType;
            PackageId = packageId;
            ToastDataNotifications = toastDataNotifications;
            TotalCount = totalCount;
            HasMoreData = hasMoreData;
            CurrentPage = currentPage;
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
