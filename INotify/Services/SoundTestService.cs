using INotifyLibrary.Util.Enums;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace INotify.Services
{
    /// <summary>
    /// Service for testing and playing notification sounds
    /// Supports both custom sounds and system sounds
    /// </summary>
    public class SoundTestService : IDisposable
    {
        private static readonly Lazy<SoundTestService> _instance = new(() => new SoundTestService());
        public static SoundTestService Instance => _instance.Value;

        private MediaPlayer _mediaPlayer;
        private bool _disposed = false;

        private SoundTestService()
        {
            try
            {
                _mediaPlayer = new MediaPlayer();
                _mediaPlayer.MediaFailed += OnMediaFailed;
                _mediaPlayer.MediaEnded += OnMediaEnded;
                Debug.WriteLine("SoundTestService initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing SoundTestService: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests a notification sound by playing it
        /// </summary>
        /// <param name="sound">The notification sound to test</param>
        /// <returns>True if the sound was played successfully</returns>
        public async Task<bool> TestSoundAsync(NotificationSounds sound)
        {
            try
            {
                Debug.WriteLine($"?? Testing sound: {sound} ({NotificationSoundHelper.GetSoundTypeDescription(sound)})");

                if (sound == NotificationSounds.None)
                {
                    return await TestSystemSoundAsync(AppNotificationSoundEvent.Default);
                }

                if (NotificationSoundHelper.IsCustomSound(sound))
                {
                    return await TestCustomSoundAsync(sound);
                }

                if (NotificationSoundHelper.IsSystemSound(sound))
                {
                    var systemSound = NotificationSoundHelper.GetSystemSoundEvent(sound);
                    return await TestSystemSoundAsync(systemSound);
                }

                Debug.WriteLine($"?? Unknown sound type: {sound}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error testing sound {sound}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests a custom sound from the Assets folder
        /// </summary>
        private async Task<bool> TestCustomSoundAsync(NotificationSounds sound)
        {
            try
            {
                var soundPath = NotificationSoundHelper.GetCustomSoundPath(sound);
                Debug.WriteLine($"?? Testing custom sound: {sound} -> {soundPath}");

                // Convert ms-appx URI to StorageFile
                var uri = new Uri(soundPath);
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
                
                if (storageFile != null)
                {
                    var mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                    _mediaPlayer.Source = mediaSource;
                    _mediaPlayer.Play();
                    
                    Debug.WriteLine($"? Playing custom sound: {sound}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"?? Could not load custom sound file: {soundPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error playing custom sound {sound}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests a system sound using direct MediaPlayer playback (no toast notification)
        /// </summary>
        private async Task<bool> TestSystemSoundAsync(AppNotificationSoundEvent systemSound)
        {
            try
            {
                Debug.WriteLine($"?? Testing system sound: {systemSound}");

                // Get the system sound URI for direct playback
                Uri soundUri = GetSystemSoundUri(systemSound);
                
                try
                {
                    var mediaSource = MediaSource.CreateFromUri(soundUri);
                    _mediaPlayer.Source = mediaSource;
                    _mediaPlayer.Play();
                    
                    Debug.WriteLine($"? Playing system sound: {systemSound} -> {soundUri}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"?? Could not play system sound directly: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error testing system sound {systemSound}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Maps AppNotificationSoundEvent enum values to their corresponding system sound URIs
        /// </summary>
        private Uri GetSystemSoundUri(AppNotificationSoundEvent soundEvent)
        {
            string uriString = soundEvent switch
            {
                AppNotificationSoundEvent.Default => "ms-winsoundevent:Notification.Default",
                AppNotificationSoundEvent.IM => "ms-winsoundevent:Notification.IM",
                AppNotificationSoundEvent.Mail => "ms-winsoundevent:Notification.Mail",
                AppNotificationSoundEvent.Reminder => "ms-winsoundevent:Notification.Reminder",
                AppNotificationSoundEvent.SMS => "ms-winsoundevent:Notification.SMS",
                AppNotificationSoundEvent.Alarm => "ms-winsoundevent:Notification.Looping.Alarm",
                AppNotificationSoundEvent.Alarm2 => "ms-winsoundevent:Notification.Looping.Alarm2",
                AppNotificationSoundEvent.Alarm3 => "ms-winsoundevent:Notification.Looping.Alarm3",
                AppNotificationSoundEvent.Alarm4 => "ms-winsoundevent:Notification.Looping.Alarm4",
                AppNotificationSoundEvent.Alarm5 => "ms-winsoundevent:Notification.Looping.Alarm5",
                AppNotificationSoundEvent.Alarm6 => "ms-winsoundevent:Notification.Looping.Alarm6",
                AppNotificationSoundEvent.Alarm7 => "ms-winsoundevent:Notification.Looping.Alarm7",
                AppNotificationSoundEvent.Alarm8 => "ms-winsoundevent:Notification.Looping.Alarm8",
                AppNotificationSoundEvent.Alarm9 => "ms-winsoundevent:Notification.Looping.Alarm9",
                AppNotificationSoundEvent.Alarm10 => "ms-winsoundevent:Notification.Looping.Alarm10",
                AppNotificationSoundEvent.Call => "ms-winsoundevent:Notification.Looping.Call",
                AppNotificationSoundEvent.Call2 => "ms-winsoundevent:Notification.Looping.Call2",
                AppNotificationSoundEvent.Call3 => "ms-winsoundevent:Notification.Looping.Call3",
                AppNotificationSoundEvent.Call4 => "ms-winsoundevent:Notification.Looping.Call4",
                AppNotificationSoundEvent.Call5 => "ms-winsoundevent:Notification.Looping.Call5",
                AppNotificationSoundEvent.Call6 => "ms-winsoundevent:Notification.Looping.Call6",
                AppNotificationSoundEvent.Call7 => "ms-winsoundevent:Notification.Looping.Call7",
                AppNotificationSoundEvent.Call8 => "ms-winsoundevent:Notification.Looping.Call8",
                AppNotificationSoundEvent.Call9 => "ms-winsoundevent:Notification.Looping.Call9",
                AppNotificationSoundEvent.Call10 => "ms-winsoundevent:Notification.Looping.Call10",
                _ => "ms-winsoundevent:Notification.Default"
            };

            return new Uri(uriString);
        }
        /// <summary>
        /// Stops any currently playing sound
        /// </summary>
        public void StopSound()
        {
            try
            {
                if (_mediaPlayer != null && _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    _mediaPlayer.Pause();
                    Debug.WriteLine("?? Stopped playing sound");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"?? Error stopping sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the estimated duration for a sound test (for UI feedback)
        /// </summary>
        public TimeSpan GetSoundTestDuration(NotificationSounds sound)
        {
            if (NotificationSoundHelper.IsCustomSound(sound))
            {
                // Custom sounds are typically 1-3 seconds
                return TimeSpan.FromSeconds(2);
            }
            
            if (NotificationSoundHelper.IsSystemSound(sound))
            {
                // System sounds are typically short
                return TimeSpan.FromSeconds(1);
            }
            
            return TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Tests all system sounds sequentially for debugging purposes
        /// </summary>
        public async Task TestAllSystemSoundsAsync()
        {
            try
            {
                Debug.WriteLine("?? === Testing All System Sounds ===");
                
                var systemSounds = Enum.GetValues<AppNotificationSoundEvent>();
                
                foreach (var systemSound in systemSounds)
                {
                    Debug.WriteLine($"?? Testing: {systemSound}");
                    await TestSystemSoundAsync(systemSound);
                    
                    // Wait between tests to avoid overlapping sounds
                    await Task.Delay(2000);
                }
                
                Debug.WriteLine("? === All System Sound Tests Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error during system sound testing: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests a specific system sound by its URI for verification
        /// </summary>
        public async Task<bool> TestSystemSoundUriAsync(string soundUri)
        {
            try
            {
                Debug.WriteLine($"?? Testing system sound URI: {soundUri}");
                
                var uri = new Uri(soundUri);
                var mediaSource = MediaSource.CreateFromUri(uri);
                _mediaPlayer.Source = mediaSource;
                _mediaPlayer.Play();
                
                Debug.WriteLine($"? Playing system sound from URI: {soundUri}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"? Error testing system sound URI {soundUri}: {ex.Message}");
                return false;
            }
        }

        #region Event Handlers

        private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine($"? Media playback failed: {args.ErrorMessage}");
        }

        private void OnMediaEnded(MediaPlayer sender, object args)
        {
            Debug.WriteLine($"? Media playback completed");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _mediaPlayer?.Dispose();
                _mediaPlayer = null;
                Debug.WriteLine("SoundTestService disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing SoundTestService: {ex.Message}");
            }

            _disposed = true;
        }

        #endregion
    }
}