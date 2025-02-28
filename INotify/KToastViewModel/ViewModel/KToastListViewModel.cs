using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Extension;
using WinCommon.Util;
using Windows.UI.Core;

namespace INotify.KToastViewModel.ViewModelContract
{
    public class KToastListViewModel : KToastListVMBase
    {
        public KToastListViewModel()
        {

        }



        #region 
        public override void LoadControl()
        {
            GetAllNotifications();
        }

        public override void UpdateKToastNotifications(ObservableCollection<KToastNotification> kToastNotifications)
        {
            UpdateKToastRequest updateKToastRequest = new UpdateKToastRequest(kToastNotifications, "userId");
            UpdateKToast updateKToast = new UpdateKToast(updateKToastRequest, new UpdateKToastsNotificationPresenterCallback(this));
            updateKToast.Execute();
        }

        public void GetAllNotifications()
        {
             GetKToastsRequest getKToastsRequest = new GetKToastsRequest(ViewType.Space, default,"userId");
             GetKToasts getKToasts =  new GetKToasts(getKToastsRequest, new GetAllKToastsNotificationPresenterCallback(this));
             getKToasts.Execute();
        }

        #endregion



        #region PresenterCallBack
        public class UpdateKToastsNotificationPresenterCallback : IUpdateKToastPresenterCallback
        {
            private KToastListViewModel _presenter { get; set; }

            public UpdateKToastsNotificationPresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnIgnored(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnProgress(ZResponse<UpdateKToastResponse> response)
            {
            }

            public void OnSuccess(ZResponse<UpdateKToastResponse> response)
            {
            }
        }
        public class GetAllKToastsNotificationPresenterCallback : IGetKToastPresenterCalback
        {
            private KToastListViewModel _presenter { get; set; }

            public GetAllKToastsNotificationPresenterCallback(KToastListViewModel presenter)
            {
                _presenter = presenter;
            }

            public void OnCanceled(ZResponse<GetKToastsResponse> response)
            {
            }

            public void OnError(ZError error)
            {
            }

            public void OnFailed(ZResponse<GetKToastsResponse> response)
            {
            }

            public void OnIgnored(ZResponse<GetKToastsResponse> response)
            {
            }

            public void OnProgress(ZResponse<GetKToastsResponse> response)
            {
            }

            public async void OnSuccess(ZResponse<GetKToastsResponse> response)
            {
               
            }
        }

        #endregion
    }
}
