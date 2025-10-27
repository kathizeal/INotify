using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class ClearPackageNotificationsDataManager : INotifyBaseDataManager, IClearPackageNotificationsDataManager
    {
        public ClearPackageNotificationsDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void ClearPackageNotificationsAsync(ClearPackageNotificationsRequest request, ICallback<ClearPackageNotificationsResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                // Get count before clearing from database
                int notificationCount = DBHandler.GetPackageNotificationCount(request.PackageFamilyName, request.UserId);
                
                // Clear notifications from database
                bool isSuccess = DBHandler.ClearPackageNotifications(request.PackageFamilyName, request.UserId);
                
                // Create response with flag indicating Windows clearing should be attempted by UI layer
                var response = new ClearPackageNotificationsResponse(
                    isSuccess, 
                    isSuccess ? notificationCount : 0, 
                    request.PackageFamilyName);
                
                var zResponse = new ZResponse<ClearPackageNotificationsResponse>(ResponseType.LocalStorage)
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