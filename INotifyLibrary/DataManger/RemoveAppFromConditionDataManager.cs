using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Util.Enums;
using System;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class RemoveAppFromConditionDataManager : INotifyBaseDataManager, IRemoveAppFromConditionDataManager
    {
        public RemoveAppFromConditionDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void RemoveAppFromConditionAsync(RemoveAppFromConditionRequest request, ICallback<RemoveAppFromConditionResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                bool isSuccess = false;
                string errorMessage = "";

                switch (request.TargetType)
                {
                    case SelectionTargetType.Priority:
                        isSuccess = RemoveFromPriority(request.PackageFamilyName, request.TargetId, request.UserId);
                        if (!isSuccess)
                        {
                            errorMessage = $"Failed to remove {request.AppDisplayName} from {request.TargetId} priority";
                        }
                        break;

                    case SelectionTargetType.Space:
                        isSuccess = RemoveFromSpace(request.PackageFamilyName, request.TargetId, request.UserId);
                        if (!isSuccess)
                        {
                            errorMessage = $"Failed to remove {request.AppDisplayName} from {request.TargetId}";
                        }
                        break;

                    default:
                        errorMessage = "Unknown target type";
                        break;
                }

                var response = new RemoveAppFromConditionResponse(
                    isSuccess, 
                    request.TargetType, 
                    request.TargetId, 
                    request.PackageFamilyName, 
                    request.AppDisplayName, 
                    errorMessage);

                var zResponse = new ZResponse<RemoveAppFromConditionResponse>(ResponseType.LocalStorage)
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
                var errorResponse = new RemoveAppFromConditionResponse(
                    false, 
                    request.TargetType, 
                    request.TargetId, 
                    request.PackageFamilyName, 
                    request.AppDisplayName, 
                    ex.Message);

                var zResponse = new ZResponse<RemoveAppFromConditionResponse>(ResponseType.LocalStorage)
                { 
                    Data = errorResponse, 
                    Status = ResponseStatus.Failed 
                };

                callback.OnError(new ZError(ErrorType.Unknown, ex));
            }
        }

        /// <summary>
        /// Removes app from priority category
        /// </summary>
        private bool RemoveFromPriority(string packageFamilyName, string priorityLevel, string userId)
        {
            try
            {
                // Use the existing DBHandler method to remove from priority
                return DBHandler.RemoveCustomPriorityApp(packageFamilyName, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing package from priority: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes app from space
        /// </summary>
        private bool RemoveFromSpace(string packageFamilyName, string spaceId, string userId)
        {
            try
            {
                // Use the existing DBHandler method to remove from space
                return DBHandler.RemovePackageFromSpace(spaceId, packageFamilyName, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing package from space: {ex.Message}");
                return false;
            }
        }
    }
}