using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System.Collections.ObjectModel;
using WinCommon.Util;
using WinCommon.Error;

namespace INotifyLibrary.DataManger
{
    public class GetKToastDataManager : INotifyBaseDataManager, IGetKToastsDataManager
    {
        public GetKToastDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetKToastsAsync(GetKToastsRequest request, ICallback<GetKToastsResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                List<KToastNotification> allNotifications = new();
                List<KToastBObj> toastDatas = new();
                
                // Get all notifications first (we'll implement filtering logic here)
                if (string.IsNullOrEmpty(request.PackageId) || request.PackageId is IKPackageProfileConstant.DefaultAllInPackageIdFamilyName)
                {
                    // Get all notifications for user (sorted by CreatedTime descending for latest first)
                    allNotifications = DBHandler.GetToastNotificationByUserId(request.UserId)
                        .OrderByDescending(n => n.CreatedTime)
                        .ToList();
                }
                else
                {
                    // Get notifications for specific package
                    allNotifications = DBHandler.GetKToastNotificationsByPackageId(request.PackageId, request.UserId)
                        .OrderByDescending(n => n.CreatedTime)
                        .ToList();
                }

                // Apply filters
                allNotifications = ApplyFilters(allNotifications, request);

                // Apply pagination after filtering
                var totalCount = allNotifications.Count;
                var pagedNotifications = allNotifications
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToList();

                // Determine if there's more data
                var hasMoreData = (request.Skip + request.Take) < totalCount;
                var currentPage = request.Skip / request.Take;

                // Populate package profiles for paged results
                if (string.IsNullOrEmpty(request.PackageId) || request.PackageId is IKPackageProfileConstant.DefaultAllInPackageIdFamilyName)
                {
                    toastDatas = PopulatePackageProfile(pagedNotifications, request.UserId);
                }
                else
                {
                    KPackageProfile? package = DBHandler.GetPackageProfile(request.PackageId, request.UserId);
                    toastDatas = PopulatePackageProfile(pagedNotifications, package);
                }

                GetKToastsResponse response = new GetKToastsResponse(
                    request.PackageId, 
                    request.NotificationRequestType, 
                    request.ViewType, 
                    new ObservableCollection<KToastBObj>(toastDatas),
                    totalCount,
                    hasMoreData,
                    currentPage);

                ZResponse<GetKToastsResponse> zResponse = new ZResponse<GetKToastsResponse>(ResponseType.LocalStorage)
                { Data = response, Status = ResponseStatus.Success };

                callback.OnSuccess(zResponse);
            }
            catch (Exception ex)
            {
                callback.OnError(default);
            }
        }

        private List<KToastNotification> ApplyFilters(List<KToastNotification> notifications, GetKToastsRequest request)
        {
            var filtered = notifications.AsEnumerable();

            // Search keyword filter (searches in title and message)
            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var keyword = request.SearchKeyword.ToLower();
                filtered = filtered.Where(n => 
                    (!string.IsNullOrEmpty(n.NotificationTitle) && n.NotificationTitle.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(n.NotificationMessage) && n.NotificationMessage.ToLower().Contains(keyword)));
            }

            // App filter (filter by specific app package family name)
            if (!string.IsNullOrWhiteSpace(request.FilterByApp))
            {
                filtered = filtered.Where(n => 
                    !string.IsNullOrEmpty(n.PackageFamilyName) && 
                    n.PackageFamilyName.Equals(request.FilterByApp, StringComparison.OrdinalIgnoreCase));
            }

            // Specific date filter (notifications on exact date)
            if (request.FilterDate.HasValue)
            {
                var filterDate = request.FilterDate.Value.Date;
                filtered = filtered.Where(n => n.CreatedTime.Date == filterDate);
            }

            // Date range filter (notifications between from and to dates)
            if (request.FromDate.HasValue || request.ToDate.HasValue)
            {
                if (request.FromDate.HasValue)
                {
                    var fromDate = request.FromDate.Value.Date;
                    filtered = filtered.Where(n => n.CreatedTime.Date >= fromDate);
                }

                if (request.ToDate.HasValue)
                {
                    var toDate = request.ToDate.Value.Date;
                    filtered = filtered.Where(n => n.CreatedTime.Date <= toDate);
                }
            }

            return filtered.ToList();
        }

        public List<KToastBObj> PopulatePackageProfile(IList<KToastNotification> toastNotifications, string userId)
        {
            List<KToastBObj> toastDatas = new();
            if (CommonUtil.IsNonEmpty(toastNotifications))
            {
                foreach (var notification in toastNotifications)
                {
                    if (!PackageProfileCache.TryGetValue(notification.PackageFamilyName, out KPackageProfile kPackageProfile))
                    {
                        kPackageProfile = DBHandler.GetPackageProfile(notification.PackageFamilyName, userId) ?? INotifyUtil.GetDefaultPackageProfile();
                        PackageProfileCache[notification.PackageFamilyName] = kPackageProfile;
                    }
                    toastDatas.Add(new KToastBObj(notification, kPackageProfile));
                }
            }
            return toastDatas;
        }

        public List<KToastBObj> PopulatePackageProfile(IList<KToastNotification> toastNotifications, KPackageProfile packageProfile)
        {
            List<KToastBObj> toastDatas = new();
            if (CommonUtil.IsNonEmpty(toastNotifications))
            {
                packageProfile = packageProfile ?? INotifyUtil.GetDefaultPackageProfile();
                foreach (var notification in toastNotifications)
                {
                    toastDatas.Add(new KToastBObj(notification, packageProfile));
                }
            }
            return toastDatas;
        }
    }
}
