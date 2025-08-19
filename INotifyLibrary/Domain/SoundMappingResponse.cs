using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using System.Collections.Generic;

namespace INotifyLibrary.Domain
{
    /// <summary>
    /// Response for sound mapping operations
    /// </summary>
    public class SoundMappingResponse
    {
        /// <summary>
        /// The operation type that was performed
        /// </summary>
        public SoundMappingOperationType OperationType { get; set; }

        /// <summary>
        /// All sound mappings for the user (for GetAll operation)
        /// </summary>
        public IList<KSoundMapper> SoundMappings { get; set; } = new List<KSoundMapper>();

        /// <summary>
        /// Package sound for a specific package (for GetPackageSound operation)
        /// </summary>
        public NotificationSounds PackageSound { get; set; } = NotificationSounds.None;

        /// <summary>
        /// Package family name that was operated on
        /// </summary>
        public string PackageFamilyName { get; set; } = string.Empty;

        /// <summary>
        /// Success status of the operation
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Packages grouped by their assigned sound (for GetPackagesBySound operation)
        /// </summary>
        public Dictionary<NotificationSounds, List<string>> PackagesBySound { get; set; } = new Dictionary<NotificationSounds, List<string>>();

        /// <summary>
        /// Initializes a new sound mapping response
        /// </summary>
        public SoundMappingResponse(SoundMappingOperationType operationType)
        {
            OperationType = operationType;
        }

        /// <summary>
        /// Creates a successful response for GetAll operation
        /// </summary>
        public static SoundMappingResponse CreateGetAllSuccess(IList<KSoundMapper> mappings)
        {
            return new SoundMappingResponse(SoundMappingOperationType.GetAll)
            {
                SoundMappings = mappings,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Creates a successful response for GetPackageSound operation
        /// </summary>
        public static SoundMappingResponse CreateGetPackageSoundSuccess(string packageFamilyName, NotificationSounds sound)
        {
            return new SoundMappingResponse(SoundMappingOperationType.GetPackageSound)
            {
                PackageFamilyName = packageFamilyName,
                PackageSound = sound,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Creates a successful response for SetPackageSound operation
        /// </summary>
        public static SoundMappingResponse CreateSetPackageSoundSuccess(string packageFamilyName)
        {
            return new SoundMappingResponse(SoundMappingOperationType.SetPackageSound)
            {
                PackageFamilyName = packageFamilyName,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Creates a successful response for RemovePackageSound operation
        /// </summary>
        public static SoundMappingResponse CreateRemovePackageSoundSuccess(string packageFamilyName)
        {
            return new SoundMappingResponse(SoundMappingOperationType.RemovePackageSound)
            {
                PackageFamilyName = packageFamilyName,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Creates a successful response for GetPackagesBySound operation
        /// </summary>
        public static SoundMappingResponse CreateGetPackagesBySoundSuccess(Dictionary<NotificationSounds, List<string>> packagesBySound)
        {
            return new SoundMappingResponse(SoundMappingOperationType.GetPackagesBySound)
            {
                PackagesBySound = packagesBySound,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Creates a failed response
        /// </summary>
        public static SoundMappingResponse CreateFailed(SoundMappingOperationType operationType, string errorMessage)
        {
            return new SoundMappingResponse(operationType)
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}