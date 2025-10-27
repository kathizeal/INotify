using INotifyLibrary.Domain;
using System.Threading;
using WinCommon.Util;

namespace INotifyLibrary.DataManger
{
    /// <summary>
    /// Interface for sound mapping data management operations
    /// All methods require UserId as mandatory parameter per coding standards
    /// </summary>
    public interface ISoundMappingDataManager
    {
        /// <summary>
        /// Processes sound mapping requests asynchronously
        /// </summary>
        /// <param name="request">Sound mapping request with required UserId</param>
        /// <param name="callback">Callback to handle response</param>
        /// <param name="cts">Cancellation token source</param>
        void ProcessSoundMappingAsync(SoundMappingRequest request, ICallback<SoundMappingResponse> callback, CancellationTokenSource cts);
    }
}