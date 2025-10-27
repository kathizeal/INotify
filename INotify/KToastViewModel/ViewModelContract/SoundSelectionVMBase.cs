using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotify.KToastViewModel.ViewModelContract;
using INotify.Services;
using INotify.Util;
using INotifyLibrary.Domain;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WinCommon.Error;
using WinCommon.Util;
using WinLogger;
using WinUI3Component.ViewContract;

namespace INotify.KToastViewModel.ViewModelContract
{
    /// <summary>
    /// Abstract base ViewModel for sound selection functionality
    /// Follows coding standards with [ComponentName]VMBase pattern
    /// </summary>
    public abstract class SoundSelectionVMBase : ToastViewModelBase
    {
        public IView View;

        #region Properties

        private ObservableCollection<KSoundMapper> _soundMappings = new();
        public ObservableCollection<KSoundMapper> SoundMappings
        {
            get => _soundMappings;
            set { _soundMappings = value; OnPropertyChanged(); }
        }

        private Dictionary<string, NotificationSounds> _packageSoundMap = new();
        public Dictionary<string, NotificationSounds> PackageSoundMap
        {
            get => _packageSoundMap;
            set { _packageSoundMap = value; OnPropertyChanged(); }
        }

        private bool _isLoadingSounds = false;
        public bool IsLoadingSounds
        {
            get => _isLoadingSounds;
            set { _isLoadingSounds = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isStatusVisible = false;
        public bool IsStatusVisible
        {
            get => _isStatusVisible;
            set { _isStatusVisible = value; OnPropertyChanged(); }
        }

        #endregion Properties

        #region Commands

        public ICommand LoadSoundMappingsCommand { get; protected set; }
        public ICommand UpdatePackageSoundCommand { get; protected set; }
        public ICommand TestSoundCommand { get; protected set; }

        #endregion Commands

        #region Constructor

        public SoundSelectionVMBase()
        {
            InitializeCommands();
        }

        #endregion Constructor

        #region Virtual Methods

        /// <summary>
        /// Loads all sound mappings for the current user
        /// Must be implemented by concrete ViewModel
        /// </summary>
        public abstract void LoadSoundMappings();

        /// <summary>
        /// Updates sound for a specific package
        /// Must be implemented by concrete ViewModel
        /// </summary>
        /// <param name="packageFamilyName">Package family name with required UserId</param>
        /// <param name="sound">Sound to assign</param>
        public abstract void UpdatePackageSound(string packageFamilyName, NotificationSounds sound);

        /// <summary>
        /// Gets sound for a specific package
        /// Must be implemented by concrete ViewModel
        /// </summary>
        /// <param name="packageFamilyName">Package family name with required UserId</param>
        /// <returns>Associated sound or None if not found</returns>
        public abstract NotificationSounds GetPackageSound(string packageFamilyName);

        /// <summary>
        /// Shows status message for a specified duration
        /// </summary>
        protected virtual void ShowStatusMessage(string message, bool isError = false)
        {
            StatusMessage = message;
            IsStatusVisible = true;

            // Auto-hide after 3 seconds
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(3000);
                DispatcherQueue.TryEnqueue(() =>
                {
                    IsStatusVisible = false;
                });
            });
        }

        /// <summary>
        /// Updates local cache with sound mappings
        /// </summary>
        protected virtual void UpdateSoundCache(IList<KSoundMapper> mappings)
        {
            PackageSoundMap.Clear();
            foreach (var mapping in mappings)
            {
                PackageSoundMap[mapping.PackageFamilyName] = mapping.Sound;
            }
            if (View is IAllPackageView allPackageView)
            {
                allPackageView.SoundPackageUpdated();
            }
        }

        /// <summary>
        /// Gets display text for notification sound
        /// </summary>
        public virtual string GetSoundDisplayText(NotificationSounds sound)
        {
            return NotificationSoundHelper.GetSoundDisplayText(sound);
        }

        /// <summary>
        /// Gets all available notification sounds
        /// </summary>
        public virtual List<NotificationSounds> GetAvailableSounds()
        {
            return Enum.GetValues<NotificationSounds>().ToList();
        }

        /// <summary>
        /// Tests a notification sound by playing it
        /// Must be implemented by concrete ViewModel
        /// </summary>
        /// <param name="sound">The sound to test</param>
        public abstract Task TestSoundAsync(NotificationSounds sound);

        #endregion Virtual Methods

        #region Private Methods

        private void InitializeCommands()
        {
            LoadSoundMappingsCommand = new RelayCommand(() => LoadSoundMappings());
            UpdatePackageSoundCommand = new RelayCommand<object>((param) =>
            {
                if (param is Tuple<string, NotificationSounds> soundUpdate)
                {
                    UpdatePackageSound(soundUpdate.Item1, soundUpdate.Item2);
                }
            });
            TestSoundCommand = new RelayCommand<object>(async (param) =>
            {
                if (param is NotificationSounds sound)
                {
                    await TestSoundAsync(sound);
                }
            });
        }

        #endregion Private Methods

        #region Cleanup

        public override void Dispose()
        {
            base.Dispose();
            SoundMappings?.Clear();
            PackageSoundMap?.Clear();
        }

        #endregion Cleanup
    }
}