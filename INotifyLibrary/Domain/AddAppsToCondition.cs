using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System.Collections.Generic;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IAddAppsToConditionDataManager
    {
        void AddAppsToConditionAsync(AddAppsToConditionRequest request, ICallback<AddAppsToConditionResponse> callback, CancellationTokenSource cts);
    }

    public interface IAddAppsToConditionPresenterCallback : ICallback<AddAppsToConditionResponse>
    {
    }

    public class AddAppsToConditionRequest : ZRequest
    {
        public SelectionTargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public IList<AppConditionData> SelectedApps { get; set; }

        public AddAppsToConditionRequest(SelectionTargetType targetType, string targetId, IList<AppConditionData> selectedApps, string userId) 
            : base(RequestType.LocalStorage, userId, default)
        {
            TargetType = targetType;
            TargetId = targetId;
            SelectedApps = selectedApps;
        }
    }

    public class AppConditionData
    {
        public string PackageId { get; set; }
        public string DisplayName { get; set; }
        public string Publisher { get; set; }

        public AppConditionData(string packageId, string displayName, string publisher)
        {
            PackageId = packageId;
            DisplayName = displayName;
            Publisher = publisher;
        }
    }

    public enum SelectionTargetType
    {
        Priority,
        Space
    }

    public class AddAppsToConditionResponse
    {
        public int SuccessCount { get; set; }
        public int TotalCount { get; set; }
        public SelectionTargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public IList<string> FailedApps { get; set; }

        public AddAppsToConditionResponse(int successCount, int totalCount, SelectionTargetType targetType, string targetId, IList<string> failedApps = null)
        {
            SuccessCount = successCount;
            TotalCount = totalCount;
            TargetType = targetType;
            TargetId = targetId;
            FailedApps = failedApps ?? new List<string>();
        }
    }

    public class AddAppsToCondition : UseCaseBase<AddAppsToConditionResponse>
    {
        private AddAppsToConditionRequest Request;
        private IAddAppsToConditionDataManager DataManager;

        public AddAppsToCondition(AddAppsToConditionRequest request, IAddAppsToConditionPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IAddAppsToConditionDataManager>();
        }

        protected override async void Action()
        {
            DataManager.AddAppsToConditionAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<AddAppsToConditionResponse>
        {
            private AddAppsToCondition Usecase;

            public UsecaseCallback(AddAppsToCondition usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<AddAppsToConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<AddAppsToConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<AddAppsToConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<AddAppsToConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}