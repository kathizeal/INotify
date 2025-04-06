using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System.Collections.ObjectModel;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    public class GetKToastDataManager : INotifyBaseDataManager, IGetKToastsDataManager
    {
        public GetKToastDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        public void GetKToastsAsync(GetKToastsRequest request, ICallback<GetKToastsResponse> callback, CancellationTokenSource cts)
        {
            try
            {

                List<KToastNotification>? list = new();
                List<KToastBObj>? toastDatas = new();
                if (string.IsNullOrEmpty(request.PackageId) || request.PackageId is IKPackageProfileConstant.DefaultAllInPackageId)
                {
                    list = DBHandler.GetToastNotificationByUserId(request.UserId).ToList();
                    toastDatas = PopulatePackageProfile(list, request.UserId);
                }
                else
                {
                    KPackageProfile? Package = DBHandler.GetPackageProfile(request.PackageId, request.UserId);
                    list = DBHandler.GetKToastNotificationsByPackageId(request.PackageId, request.UserId).ToList();
                    toastDatas = PopulatePackageProfile(list, Package);

                }

                GetKToastsResponse? response = new GetKToastsResponse(request.PackageId, request.NotificationRequestType, request.ViewType, new ObservableCollection<KToastBObj>(toastDatas));
                ZResponse<GetKToastsResponse> zResponse = new ZResponse<GetKToastsResponse>(ResponseType.LocalStorage)
                { Data = response, Status = ResponseStatus.Success };

                callback.OnSuccess(zResponse);
            }
            catch (Exception ex)
            {
                callback.OnError(default);
            }
        }

        public List<KToastBObj> PopulatePackageProfile(IList<KToastNotification> toastNotifications, string userId)
        {
            List<KToastBObj> toastDatas = new();
            if (CommonUtil.IsNonEmpty(toastNotifications))
            {
                foreach (var notification in toastNotifications)
                {
                    if (!PackageProfileCache.TryGetValue(notification.PackageId, out KPackageProfile kPackageProfile))
                    {
                        kPackageProfile = DBHandler.GetPackageProfile(notification.PackageId, userId) ?? INotifyUtil.GetDefaultPackageProfile();
                        PackageProfileCache[notification.PackageId] = kPackageProfile;
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
