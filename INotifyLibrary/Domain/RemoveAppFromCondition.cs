using INotifyLibrary.DI;
using INotifyLibrary.Util.Enums;
using System.Collections.Generic;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface IRemoveAppFromConditionDataManager
    {
        void RemoveAppFromConditionAsync(RemoveAppFromConditionRequest request, ICallback<RemoveAppFromConditionResponse> callback, CancellationTokenSource cts);
    }

    public interface IRemoveAppFromConditionPresenterCallback : ICallback<RemoveAppFromConditionResponse>
    {
    }

    public class RemoveAppFromConditionRequest : ZRequest
    {
        public SelectionTargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public string PackageFamilyName { get; set; }
        public string AppDisplayName { get; set; }

        public RemoveAppFromConditionRequest(SelectionTargetType targetType, string targetId, string packageFamilyName, string appDisplayName, string userId) 
            : base(RequestType.LocalStorage, userId, default)
        {
            TargetType = targetType;
            TargetId = targetId;
            PackageFamilyName = packageFamilyName;
            AppDisplayName = appDisplayName;
        }
    }

    public class RemoveAppFromConditionResponse
    {
        public bool IsSuccess { get; set; }
        public SelectionTargetType TargetType { get; set; }
        public string TargetId { get; set; }
        public string PackageFamilyName { get; set; }
        public string AppDisplayName { get; set; }
        public string ErrorMessage { get; set; }

        public RemoveAppFromConditionResponse(bool isSuccess, SelectionTargetType targetType, string targetId, string packageFamilyName, string appDisplayName, string errorMessage = "")
        {
            IsSuccess = isSuccess;
            TargetType = targetType;
            TargetId = targetId;
            PackageFamilyName = packageFamilyName;
            AppDisplayName = appDisplayName;
            ErrorMessage = errorMessage;
        }
    }

    public class RemoveAppFromCondition : UseCaseBase<RemoveAppFromConditionResponse>
    {
        private RemoveAppFromConditionRequest Request;
        private IRemoveAppFromConditionDataManager DataManager;

        public RemoveAppFromCondition(RemoveAppFromConditionRequest request, IRemoveAppFromConditionPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IRemoveAppFromConditionDataManager>();
        }

        protected override async void Action()
        {
            DataManager.RemoveAppFromConditionAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<RemoveAppFromConditionResponse>
        {
            private RemoveAppFromCondition Usecase;

            public UsecaseCallback(RemoveAppFromCondition usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<RemoveAppFromConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<RemoveAppFromConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<RemoveAppFromConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<RemoveAppFromConditionResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}