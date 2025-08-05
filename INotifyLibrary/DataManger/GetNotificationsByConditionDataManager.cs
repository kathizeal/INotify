using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class GetNotificationsByConditionDataManager : INotifyBaseDataManager, IGetNotificationsByConditionDataManager
    {
        public GetNotificationsByConditionDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetNotificationsByConditionAsync(GetNotificationsByConditionRequest request, ICallback<GetNotificationsByConditionResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                List<string> packageIds = new List<string>();

                // Get package IDs based on condition type
                switch (request.TargetType)
                {
                    case SelectionTargetType.Priority:
                        packageIds = GetPackageIdsByPriority(request.TargetId, request.UserId);
                        break;

                    case SelectionTargetType.Space:
                        packageIds = GetPackageIdsBySpace(request.TargetId, request.UserId);
                        break;

                    default:
                        var errorResponse = new GetNotificationsByConditionResponse(request.TargetType, request.TargetId, request.IsPackageView);
                        var errorZResponse = new ZResponse<GetNotificationsByConditionResponse>(ResponseType.LocalStorage)
                        { Data = errorResponse, Status = ResponseStatus.Failed };
                        callback.OnFailed(errorZResponse);
                        return;
                }

                if (request.IsPackageView)
                {
                    var packages = GetPackagesWithNotificationCounts(packageIds, request.UserId);
                    var response = new GetNotificationsByConditionResponse(
                        request.TargetType, 
                        request.TargetId, 
                        request.IsPackageView,
                        null,
                        packages);

                    var zResponse = new ZResponse<GetNotificationsByConditionResponse>(ResponseType.LocalStorage)
                    { Data = response, Status = ResponseStatus.Success };
                    callback.OnSuccess(zResponse);
                }
                else
                {
                    var notifications = GetNotificationsByPackageIds(packageIds, request.UserId);
                    var response = new GetNotificationsByConditionResponse(
                        request.TargetType, 
                        request.TargetId, 
                        request.IsPackageView,
                        notifications);

                    var zResponse = new ZResponse<GetNotificationsByConditionResponse>(ResponseType.LocalStorage)
                    { Data = response, Status = ResponseStatus.Success };
                    callback.OnSuccess(zResponse);
                }
            }
            catch (Exception ex)
            {
                callback.OnError(new ZError(ErrorType.Unknown, ex));
            }
        }

        private List<string> GetPackageIdsByPriority(string priorityString, string userId)
        {
            if (!Enum.TryParse<Priority>(priorityString, true, out Priority priority))
            {
                return new List<string>();
            }

            var priorityApps = DBHandler.GetAppsByPriority(priority, userId);
            return priorityApps.Select(app => app.PackageName).ToList();
        }

        private List<string> GetPackageIdsBySpace(string spaceId, string userId)
        {
            var packages = DBHandler.GetPackagesBySpaceId(spaceId, userId);
            return packages.Select(package => package.PackageFamilyName).ToList();
        }

        private ObservableCollection<KToastBObj> GetNotificationsByPackageIds(List<string> packageIds, string userId)
        {
            List<KToastBObj> allNotifications = new List<KToastBObj>();

            foreach (string packageId in packageIds)
            {
                var notifications = DBHandler.GetKToastNotificationsByPackageId(packageId, userId);
                var packageProfile = DBHandler.GetPackageProfile(packageId, userId) ?? INotifyUtil.GetDefaultPackageProfile();

                foreach (var notification in notifications)
                {
                    allNotifications.Add(new KToastBObj(notification, packageProfile));
                }
            }

            // Sort by CreatedTime descending (most recent first)
            var sortedNotifications = allNotifications
                .OrderByDescending(n => n.NotificationData.CreatedTime)
                .ToList();

            return new ObservableCollection<KToastBObj>(sortedNotifications);
        }

        private ObservableCollection<KPackageProfile> GetPackagesWithNotificationCounts(List<string> packageIds, string userId)
        {
            List<KPackageProfile> packages = new List<KPackageProfile>();

            foreach (string packageId in packageIds)
            {
                var package = DBHandler.GetPackageProfile(packageId, userId);
                if (package != null)
                {
                    // Get notification count for this package
                    var notifications = DBHandler.GetKToastNotificationsByPackageId(packageId, userId);
                    
                    // You might want to add a NotificationCount property to KPackageProfile
                    // For now, we'll use the package as is
                    packages.Add(package);
                }
            }

            // Sort packages by name for consistency
            var sortedPackages = packages.OrderBy(p => p.AppDisplayName ?? p.PackageFamilyName).ToList();
            return new ObservableCollection<KPackageProfile>(sortedPackages);
        }
    }
}