using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using INotifyLibrary.Util.Enums;
using SQLite;
using WinCommon.Util;

namespace INotifyLibrary.Model.Entity
{
    /// <summary>
    /// Custom priority app entity for storing user-defined app priorities
    /// </summary>
    [Table("KCustomPriorityApp")]
    public class KCustomPriorityApp : ObservableObject
    {
        private string _id = string.Empty;
        private string _PackageName = string.Empty;
        private string _displayName = string.Empty;
        private string _publisher = string.Empty;
        private Priority _priority = Priority.None;
        private string _userId = string.Empty;
        private DateTimeOffset _createdTime;
        private DateTimeOffset _updatedTime;
        private bool _isEnabled = true;
        private string _iconPath = string.Empty;
        private string _description = string.Empty;

        [PrimaryKey]
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        [Indexed]
        public string PackageName
        {
            get => _PackageName;
            set { _PackageName = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Publisher
        {
            get => _publisher;
            set { _publisher = value; OnPropertyChanged(); }
        }

        [Indexed]
        public Priority Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }

        [Indexed]
        public string UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        public DateTimeOffset CreatedTime
        {
            get => _createdTime;
            set { _createdTime = value; OnPropertyChanged(); }
        }

        public DateTimeOffset UpdatedTime
        {
            get => _updatedTime;
            set { _updatedTime = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public string IconPath
        {
            get => _iconPath;
            set { _iconPath = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Helper property for UI display
        /// </summary>
        [Ignore]
        public string PriorityText => Priority switch
        {
            Priority.High => "?? High Priority",
            Priority.Medium => "?? Medium Priority",
            Priority.Low => "?? Low Priority",
            _ => "? No Priority"
        };

        /// <summary>
        /// Helper property for priority sorting
        /// </summary>
        [Ignore]
        public int PriorityOrder => Priority switch
        {
            Priority.High => 1,
            Priority.Medium => 2,
            Priority.Low => 3,
            _ => 4
        };

        /// <summary>
        /// Updates this entity with new data
        /// </summary>
        public void Update(KCustomPriorityApp newData)
        {
            if (newData == null) return;

            DisplayName = newData.DisplayName;
            Publisher = newData.Publisher;
            Priority = newData.Priority;
            IsEnabled = newData.IsEnabled;
            IconPath = newData.IconPath;
            Description = newData.Description;
            UpdatedTime = DateTimeOffset.Now;
        }

        /// <summary>
        /// Creates a deep clone of this entity
        /// </summary>
        public KCustomPriorityApp DeepClone()
        {
            return new KCustomPriorityApp
            {
                Id = Id,
                PackageName = PackageName,
                DisplayName = DisplayName,
                Publisher = Publisher,
                Priority = Priority,
                UserId = UserId,
                CreatedTime = CreatedTime,
                UpdatedTime = UpdatedTime,
                IsEnabled = IsEnabled,
                IconPath = IconPath,
                Description = Description
            };
        }
    }
}