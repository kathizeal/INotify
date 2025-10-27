using INotifyLibrary.DataManger;
using INotifyLibrary.DI;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    /// <summary>
    /// Use case for sound mapping operations
    /// Encapsulates business logic and dependency injection of DataManagers
    /// </summary>
    public class SoundMappingUseCase : UseCaseBase<SoundMappingResponse>
    {
        private SoundMappingRequest Request;
        private ISoundMappingDataManager DataManager;

        public SoundMappingUseCase(SoundMappingRequest request, ICallback<SoundMappingResponse> callback) : base(callback, request.CTS)
        {
            Request = request;
            DataManager = INotifyLibraryDIServiceProvider.Instance.GetService<ISoundMappingDataManager>();
        }

        protected override void Action()
        {
            DataManager.ProcessSoundMappingAsync(Request, new UsecaseCallback(this), Request.CTS);
        }

        private sealed class UsecaseCallback : CallbackBase<SoundMappingResponse>
        {
            private SoundMappingUseCase Usecase;

            public UsecaseCallback(SoundMappingUseCase usecase)
            {
                Usecase = usecase;
            }

            public override void OnSuccess(ZResponse<SoundMappingResponse> response)
            {
                Usecase.PresenterCallback?.OnSuccess(response);
            }

            public override void OnProgress(ZResponse<SoundMappingResponse> response)
            {
                Usecase.PresenterCallback?.OnProgress(response);
            }

            public override void OnFailed(ZResponse<SoundMappingResponse> response)
            {
                Usecase.PresenterCallback?.OnFailed(response);
            }

            public override void OnError(ZError error)
            {
                Usecase.PresenterCallback?.OnError(error);
            }

            public override void OnCanceled(ZResponse<SoundMappingResponse> response)
            {
                Usecase.PresenterCallback?.OnCanceled(response);
            }
        }
    }
}