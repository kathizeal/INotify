using INotifyLibrary.Util.Enums;
using SQLite;
using System;

namespace INotifyLibrary.Model.Entity
{
    /// <summary>
    /// Maps package family names to notification sounds
    /// PackageFamilyName is the primary key
    /// </summary>
    [Table("KSoundMapper")]
    public class KSoundMapper
    {
        [PrimaryKey]
        public string PackageFamilyName { get; set; } = string.Empty;

        /// <summary>
        /// Associated notification sound for this package
        /// </summary>
        public NotificationSounds Sound { get; set; } = NotificationSounds.None;

        /// <summary>
        /// User who owns this sound mapping
        /// </summary>
        [Indexed]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When this sound mapping was created
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// When this sound mapping was last updated
        /// </summary>
        public DateTimeOffset UpdatedTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Whether this sound mapping is active
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}