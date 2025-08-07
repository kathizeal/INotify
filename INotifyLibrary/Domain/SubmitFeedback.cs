using INotifyLibrary.DI;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    public interface ISubmitFeedbackDataManager
    {
        void SubmitFeedbackAsync(SubmitFeedbackRequest request, ICallback<SubmitFeedbackResponse> callback, CancellationTokenSource cts);
    }

    public interface ISubmitFeedbackPresenterCallback : ICallback<SubmitFeedbackResponse>
    {
    }

    public class SubmitFeedbackRequest : ZRequest
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public FeedbackCategory Category { get; set; }
        public string Email { get; set; }
        public string AppVersion { get; set; }
        public string OSVersion { get; set; }

        public SubmitFeedbackRequest(string title, string message, FeedbackCategory category, string email, string appVersion, string osVersion, string userId) 
            : base(RequestType.LocalStorage, userId, default)
        {
            Title = title;
            Message = message;
            Category = category;
            Email = email;
            AppVersion = appVersion;
            OSVersion = osVersion;
        }
    }

    public class SubmitFeedbackResponse
    {
        public bool IsSuccess { get; set; }
        public string FeedbackId { get; set; }
        public string Message { get; set; }

        public SubmitFeedbackResponse(bool isSuccess, string feedbackId = null, string message = null)
        {
            IsSuccess = isSuccess;
            FeedbackId = feedbackId;
            Message = message;
        }
    }

    public class SubmitFeedback : UseCaseBase<SubmitFeedbackResponse>
    {
        private SubmitFeedbackRequest Request;
        private ISubmitFeedbackDataManager DataManager;

        public SubmitFeedback(SubmitFeedbackRequest request, ISubmitFeedbackPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<ISubmitFeedbackDataManager>();
        }

        protected override async void Action()
        {
            DataManager.SubmitFeedbackAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<SubmitFeedbackResponse>
        {
            private SubmitFeedback Usecase;

            public UsecaseCallback(SubmitFeedback usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<SubmitFeedbackResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<SubmitFeedbackResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<SubmitFeedbackResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<SubmitFeedbackResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}