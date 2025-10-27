using INotifyLibrary.Util.Enums;
using WinCommon.Util;

namespace INotifyLibrary.Domain
{
    /// <summary>
    /// Request for sound mapping operations (get, add, update, remove)
    /// </summary>
    public class SoundMappingRequest : ZRequest
    {
        /// <summary>
        /// The type of operation to perform
        /// </summary>
        public SoundMappingOperationType OperationType { get; set; }

        /// <summary>
        /// Package family name for the sound mapping
        /// </summary>
        public string PackageFamilyName { get; set; } = string.Empty;

        /// <summary>
        /// Notification sound to assign
        /// </summary>
        public NotificationSounds Sound { get; set; } = NotificationSounds.None;

        /// <summary>
        /// Initializes a new sound mapping request
        /// </summary>
        public SoundMappingRequest(RequestType reqType, string userId, SoundMappingOperationType operationType) 
            : base(reqType, userId, null)
        {
            OperationType = operationType;
        }

        /// <summary>
        /// Creates a request to get all sound mappings for a user
        /// </summary>
        public static SoundMappingRequest GetAllMappings(string userId)
        {
            return new SoundMappingRequest(RequestType.LocalStorage, userId, SoundMappingOperationType.GetAll);
        }

        /// <summary>
        /// Creates a request to get sound for a specific package
        /// </summary>
        public static SoundMappingRequest GetPackageSound(string packageFamilyName, string userId)
        {
            return new SoundMappingRequest(RequestType.LocalStorage, userId, SoundMappingOperationType.GetPackageSound)
            {
                PackageFamilyName = packageFamilyName
            };
        }

        /// <summary>
        /// Creates a request to set sound for a package
        /// </summary>
        public static SoundMappingRequest SetPackageSound(string packageFamilyName, NotificationSounds sound, string userId)
        {
            return new SoundMappingRequest(RequestType.LocalStorage, userId, SoundMappingOperationType.SetPackageSound)
            {
                PackageFamilyName = packageFamilyName,
                Sound = sound
            };
        }

        /// <summary>
        /// Creates a request to remove sound for a package (reset to None)
        /// </summary>
        public static SoundMappingRequest RemovePackageSound(string packageFamilyName, string userId)
        {
            return new SoundMappingRequest(RequestType.LocalStorage, userId, SoundMappingOperationType.RemovePackageSound)
            {
                PackageFamilyName = packageFamilyName
            };
        }

        /// <summary>
        /// Creates a request to get packages grouped by sound
        /// </summary>
        public static SoundMappingRequest GetPackagesBySound(string userId)
        {
            return new SoundMappingRequest(RequestType.LocalStorage, userId, SoundMappingOperationType.GetPackagesBySound);
        }
    }

    /// <summary>
    /// Types of sound mapping operations
    /// </summary>
    public enum SoundMappingOperationType
    {
        GetAll,
        GetPackageSound,
        SetPackageSound,
        RemovePackageSound,
        GetPackagesBySound
    }
}