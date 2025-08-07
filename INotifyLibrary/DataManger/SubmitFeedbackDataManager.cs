using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Util.Enums;
using System;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class SubmitFeedbackDataManager : INotifyBaseDataManager, ISubmitFeedbackDataManager
    {
        public SubmitFeedbackDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void SubmitFeedbackAsync(SubmitFeedbackRequest request, ICallback<SubmitFeedbackResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                bool isSuccess = DBHandler.SubmitFeedback(
                    request.Title,
                    request.Message,
                    request.Category,
                    request.Email,
                    request.UserId,
                    request.AppVersion,
                    request.OSVersion
                );

                var response = new SubmitFeedbackResponse(
                    isSuccess, 
                    isSuccess ? Guid.NewGuid().ToString() : null,
                    isSuccess ? "Feedback submitted successfully" : "Failed to submit feedback"
                );

                var zResponse = new ZResponse<SubmitFeedbackResponse>(ResponseType.LocalStorage)
                { 
                    Data = response, 
                    Status = isSuccess ? ResponseStatus.Success : ResponseStatus.Failed 
                };

                if (isSuccess)
                {
                    callback.OnSuccess(zResponse);
                }
                else
                {
                    callback.OnFailed(zResponse);
                }
            }
            catch (Exception ex)
            {
                callback.OnError(new ZError(ErrorType.Unknown, ex));
            }
        }
    }
}