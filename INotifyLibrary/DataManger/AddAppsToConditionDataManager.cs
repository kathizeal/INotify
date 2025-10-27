using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class AddAppsToConditionDataManager : INotifyBaseDataManager, IAddAppsToConditionDataManager
    {
        public AddAppsToConditionDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void AddAppsToConditionAsync(AddAppsToConditionRequest request, ICallback<AddAppsToConditionResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                int successCount = 0;
                int totalCount = request.SelectedApps?.Count ?? 0;
                List<string> failedApps = new List<string>();

                if (totalCount == 0)
                {
                    var emptyResponse = new AddAppsToConditionResponse(0, 0, request.TargetType, request.TargetId);
                    var emptyZResponse = new ZResponse<AddAppsToConditionResponse>(ResponseType.LocalStorage)
                    { Data = emptyResponse, Status = ResponseStatus.Success };
                    callback.OnSuccess(emptyZResponse);
                    return;
                }

                switch (request.TargetType)
                {
                    case SelectionTargetType.Priority:
                        successCount = ProcessPriorityApps(request, failedApps);
                        break;

                    case SelectionTargetType.Space:
                        successCount = ProcessSpaceApps(request, failedApps);
                        break;

                    default:
                        var errorResponse = new AddAppsToConditionResponse(0, totalCount, request.TargetType, request.TargetId, failedApps);
                        var errorZResponse = new ZResponse<AddAppsToConditionResponse>(ResponseType.LocalStorage)
                        { Data = errorResponse, Status = ResponseStatus.Failed };
                        callback.OnFailed(errorZResponse);
                        return;
                }

                var response = new AddAppsToConditionResponse(successCount, totalCount, request.TargetType, request.TargetId, failedApps);
                var zResponse = new ZResponse<AddAppsToConditionResponse>(ResponseType.LocalStorage)
                { Data = response, Status = ResponseStatus.Success };

                callback.OnSuccess(zResponse);
            }
            catch (Exception ex)
            {
                callback.OnError(new ZError(ErrorType.Unknown, ex));
            }
        }

        private int ProcessPriorityApps(AddAppsToConditionRequest request, List<string> failedApps)
        {
            int successCount = 0;

            // Convert target ID to Priority enum
            if (!Enum.TryParse<Priority>(request.TargetId, true, out Priority priority))
            {
                // Add all apps to failed list if priority is invalid
                foreach (var app in request.SelectedApps)
                {
                    failedApps.Add(app.DisplayName);
                }
                return 0;
            }

            foreach (var app in request.SelectedApps)
            {
                try
                {
                    bool success = DBHandler.AddOrUpdateCustomPriorityApp(
                        app.PackageName,
                        app.DisplayName,
                        app.Publisher,
                        priority,
                        request.UserId);

                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedApps.Add(app.DisplayName);
                    }

                    DBHandler.UpdateKPackageProfileFromAddition(new KPackageProfile() {PackageFamilyName = app.PackageName, AppDisplayName = app.DisplayName, Publisher = app.Publisher, LogoFilePath = string.Empty }, request.UserId);

                }
                catch (Exception)
                {
                    failedApps.Add(app.DisplayName);
                }
            }

            return successCount;
        }

        private int ProcessSpaceApps(AddAppsToConditionRequest request, List<string> failedApps)
        {
            int successCount = 0;

            foreach (var app in request.SelectedApps)
            {
                try
                {
                    var spaceMapper = new KSpaceMapper(request.TargetId, app.PackageName);
                  

                    bool success = DBHandler.AddPackageToSpace(spaceMapper, request.UserId);

                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedApps.Add(app.DisplayName);
                    }
                    DBHandler.UpdateKPackageProfileFromAddition(new KPackageProfile() { PackageFamilyName = app.PackageName, AppDisplayName = app.DisplayName, Publisher = app.Publisher, LogoFilePath = string.Empty }, request.UserId);

                }
                catch (Exception)
                {
                    failedApps.Add(app.DisplayName);
                }
            }

            return successCount;
        }
    }
}