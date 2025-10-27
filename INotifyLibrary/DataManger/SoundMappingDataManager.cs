using INotifyLibrary.DBHandler.Contract;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;

namespace INotifyLibrary.DataManger
{
    /// <summary>
    /// Sound mapping data manager implementation
    /// All operations require UserId as mandatory parameter per coding standards
    /// </summary>
    public class SoundMappingDataManager : INotifyBaseDataManager, ISoundMappingDataManager
    {
        public SoundMappingDataManager(INotifyDBHandler dBHandler) : base(dBHandler) { }

        /// <summary>
        /// Processes sound mapping requests asynchronously
        /// </summary>
        public void ProcessSoundMappingAsync(SoundMappingRequest request, ICallback<SoundMappingResponse> callback, CancellationTokenSource cts)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId))
                {
                    var errorResponse = SoundMappingResponse.CreateFailed(request.OperationType, "UserId is required for all sound mapping operations");
                    var errorZResponse = new ZResponse<SoundMappingResponse>(ResponseType.LocalStorage)
                    { Data = errorResponse, Status = ResponseStatus.Failed };
                    callback.OnFailed(errorZResponse);
                    return;
                }

                SoundMappingResponse response;

                switch (request.OperationType)
                {
                    case SoundMappingOperationType.GetAll:
                        response = ProcessGetAllMappings(request);
                        break;

                    case SoundMappingOperationType.GetPackageSound:
                        response = ProcessGetPackageSound(request);
                        break;

                    case SoundMappingOperationType.SetPackageSound:
                        response = ProcessSetPackageSound(request);
                        break;

                    case SoundMappingOperationType.RemovePackageSound:
                        response = ProcessRemovePackageSound(request);
                        break;

                    case SoundMappingOperationType.GetPackagesBySound:
                        response = ProcessGetPackagesBySound(request);
                        break;

                    default:
                        response = SoundMappingResponse.CreateFailed(request.OperationType, "Unknown operation type");
                        break;
                }

                var zResponse = new ZResponse<SoundMappingResponse>(ResponseType.LocalStorage)
                {
                    Data = response,
                    Status = response.IsSuccess ? ResponseStatus.Success : ResponseStatus.Failed
                };

                if (response.IsSuccess)
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
                var errorResponse = SoundMappingResponse.CreateFailed(request.OperationType, ex.Message);
                var errorZResponse = new ZResponse<SoundMappingResponse>(ResponseType.LocalStorage)
                { Data = errorResponse, Status = ResponseStatus.Failed };
                callback.OnError(new ZError(ErrorType.Unknown, ex));
            }
        }

        private SoundMappingResponse ProcessGetAllMappings(SoundMappingRequest request)
        {
            try
            {
                var mappings = DBHandler.GetSoundMappings(request.UserId);
                return SoundMappingResponse.CreateGetAllSuccess(mappings);
            }
            catch (Exception ex)
            {
                return SoundMappingResponse.CreateFailed(request.OperationType, ex.Message);
            }
        }

        private SoundMappingResponse ProcessGetPackageSound(SoundMappingRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PackageFamilyName))
                {
                    return SoundMappingResponse.CreateFailed(request.OperationType, "PackageFamilyName is required");
                }

                var sound = DBHandler.GetPackageSound(request.PackageFamilyName, request.UserId);
                return SoundMappingResponse.CreateGetPackageSoundSuccess(request.PackageFamilyName, sound);
            }
            catch (Exception ex)
            {
                return SoundMappingResponse.CreateFailed(request.OperationType, ex.Message);
            }
        }

        private SoundMappingResponse ProcessSetPackageSound(SoundMappingRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PackageFamilyName))
                {
                    return SoundMappingResponse.CreateFailed(request.OperationType, "PackageFamilyName is required");
                }

                var success = DBHandler.AddOrUpdateSoundMapping(request.PackageFamilyName, request.Sound, request.UserId);
                if (success)
                {
                    return SoundMappingResponse.CreateSetPackageSoundSuccess(request.PackageFamilyName);
                }
                else
                {
                    return SoundMappingResponse.CreateFailed(request.OperationType, "Failed to set package sound");
                }
            }
            catch (Exception ex)
            {
                return SoundMappingResponse.CreateFailed(request.OperationType, ex.Message);
            }
        }

        private SoundMappingResponse ProcessRemovePackageSound(SoundMappingRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PackageFamilyName))
                {
                    return SoundMappingResponse.CreateFailed(request.OperationType, "PackageFamilyName is required");
                }

                var success = DBHandler.RemoveSoundMapping(request.PackageFamilyName, request.UserId);
                if (success)
                {
                    return SoundMappingResponse.CreateRemovePackageSoundSuccess(request.PackageFamilyName);
                }
                else
                {
                    return SoundMappingResponse.CreateFailed(request.OperationType, "Failed to remove package sound");
                }
            }
            catch (Exception ex)
            {
                return SoundMappingResponse.CreateFailed(request.OperationType, ex.Message);
            }
        }

        private SoundMappingResponse ProcessGetPackagesBySound(SoundMappingRequest request)
        {
            try
            {
                var packagesBySound = DBHandler.GetPackagesBySound(request.UserId);
                return SoundMappingResponse.CreateGetPackagesBySoundSuccess(packagesBySound);
            }
            catch (Exception ex)
            {
                return SoundMappingResponse.CreateFailed(request.OperationType, ex.Message);
            }
        }
    }
}