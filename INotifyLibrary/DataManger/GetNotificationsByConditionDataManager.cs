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
                List<string> packageFamilyNames = new List<string>();

                // Get package family names based on condition type
                switch (request.TargetType)
                {
                    case SelectionTargetType.Priority:
                        packageFamilyNames = GetPackageFamilyNamesByPriority(request.TargetId, request.UserId);
                        break;

                    case SelectionTargetType.Space:
                        packageFamilyNames = GetPackageFamilyNamesBySpace(request.TargetId, request.UserId);
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
                    // For package view, get both packages and all their notifications for grouping
                    var packages = GetPackagesWithNotificationCounts(packageFamilyNames, request.UserId);
                    var allNotifications = GetNotificationsByPackageFamilyNames(packageFamilyNames, request.UserId);
                    
                    var response = new GetNotificationsByConditionResponse(
                        request.TargetType, 
                        request.TargetId, 
                        request.IsPackageView,
                        allNotifications,
                        packages);

                    var zResponse = new ZResponse<GetNotificationsByConditionResponse>(ResponseType.LocalStorage)
                    { Data = response, Status = ResponseStatus.Success };
                    callback.OnSuccess(zResponse);
                }
                else
                {
                    var notifications = GetNotificationsByPackageFamilyNames(packageFamilyNames, request.UserId);
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

        private List<string> GetPackageFamilyNamesByPriority(string priorityString, string userId)
        {
            if (!Enum.TryParse<Priority>(priorityString, true, out Priority priority))
            {
                return new List<string>();
            }

            var priorityApps = DBHandler.GetAppsByPriority(priority, userId);
            return priorityApps.Select(app => app.PackageName).ToList();
        }

        private List<string> GetPackageFamilyNamesBySpace(string spaceId, string userId)
        {
            var packages = DBHandler.GetPackagesBySpaceId(spaceId, userId);
            return packages.Select(package => package.PackageFamilyName).ToList();
        }

        private ObservableCollection<KToastBObj> GetNotificationsByPackageFamilyNames(List<string> packageFamilyNames, string userId)
        {
            List<KToastBObj> allNotifications = new List<KToastBObj>();

            foreach (string packageFamilyName in packageFamilyNames)
            {
                var notifications = DBHandler.GetKToastNotificationsByPackageId(packageFamilyName, userId);
                var packageProfile = DBHandler.GetPackageProfile(packageFamilyName, userId) ?? INotifyUtil.GetDefaultPackageProfile();

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

        private ObservableCollection<KPackageProfile> GetPackagesWithNotificationCounts(List<string> packageFamilyNames, string userId)
        {
            List<KPackageProfile> packages = new List<KPackageProfile>();

            foreach (string packageFamilyName in packageFamilyNames)
            {
                var package = DBHandler.GetPackageProfile(packageFamilyName, userId);
                if (package != null)
                {
                    packages.Add(package);
                }
                else
                {
                    // Create a default package profile if none exists
                    var defaultPackage = INotifyUtil.GetDefaultPackageProfile();
                    defaultPackage.PackageFamilyName = packageFamilyName;
                    packages.Add(defaultPackage);
                }
            }

            // Sort packages by name for consistency
            var sortedPackages = packages.OrderBy(p => p.AppDisplayName ?? p.PackageFamilyName).ToList();
            return new ObservableCollection<KPackageProfile>(sortedPackages);
        }
    }
}