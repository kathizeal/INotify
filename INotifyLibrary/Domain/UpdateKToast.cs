﻿using INotifyLibrary.DI;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{

    public interface IUpdateKToastDataManager
    {
        void UpdateKToast(UpdateKToastRequest request, ICallback<UpdateKToastResponse> callback);
    }

    public interface IUpdateKToastPresenterCallback : ICallback<UpdateKToastResponse> { }
    public class UpdateKToastRequest:ZRequest
    {
        public KToastBObj ToastData { get; set; }
        public UpdateKToastRequest(KToastBObj toastData, string userId) : base(RequestType.LocalStorage, userId, default)  
        {
            ToastData = toastData;
        }

    }
                                                                                         
    public class UpdateKToastResponse
    {
        public KToastBObj KToastData { get; set; }
        public UpdateKToastResponse(KToastBObj kToastData)
        {
            KToastData = kToastData;
        }
    }

    public class UpdateKToast : UseCaseBase<UpdateKToastResponse>
    {
        private UpdateKToastRequest Request;
        private IUpdateKToastDataManager DataManager;
        public UpdateKToast(UpdateKToastRequest request, IUpdateKToastPresenterCallback callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<IUpdateKToastDataManager>();
        }
        protected override async void Action()
        {
            DataManager.UpdateKToast(Request, new UsecaseCallback(this));
        }

        private sealed class UsecaseCallback : CallbackBase<UpdateKToastResponse>
        {
            private UpdateKToast Usecase;

            public UsecaseCallback(UpdateKToast usecase)
            {
                Usecase = usecase;
            }
            public override void OnSuccess(ZResponse<UpdateKToastResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }
            public override void OnProgress(ZResponse<UpdateKToastResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<UpdateKToastResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<UpdateKToastResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }

    }

}
