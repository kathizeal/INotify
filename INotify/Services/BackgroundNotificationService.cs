using INotify.KToastDI;
using INotify.KToastView.Model;
using INotify.KToastViewModel.ViewModelContract;
using INotify.Util;
using INotifyLibrary.Model.Entity;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace INotify.Services
{
    /// <summary>
    /// Background service that handles notification listening independently of the UI
    /// </summary>
    public class BackgroundNotificationService : IDisposable
    {
        private UserNotificationListener? _listener;
        private KToastListVMBase? _viewModel;
        private INotifyToastService? _toastService;
        private readonly SemaphoreSlim _fileAccessSemaphore = new SemaphoreSlim(1, 1);
        private bool _isRunning = false;
        private bool _disposed = false;

        public bool IsRunning => _isRunning;

        public async Task<bool> StartAsync()
        {
            if (_isRunning) return true;

            try
            {
                Debug.WriteLine("Starting Background Notification Service...");

                // Get ViewModel
                _viewModel = KToastDIServiceProvider.Instance.GetService<KToastListVMBase>();

                // Initialize INotify Toast Service
                _toastService = INotifyToastService.Instance;

                // Check feature support
                if (!ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
                {
                    Debug.WriteLine("UserNotificationListener not supported on this system");
                    return false;
                }

                // Initialize listener
                _listener = UserNotificationListener.Current;
                var accessStatus = await _listener.RequestAccessAsync();

                switch (accessStatus)
                {
                    case UserNotificationListenerAccessStatus.Allowed:
                        Debug.WriteLine("Background notification access granted");
                        break;
                    case UserNotificationListenerAccessStatus.Denied:
                        Debug.WriteLine("Background notification access denied");
                        return false;
                    case UserNotificationListenerAccessStatus.Unspecified:
                        Debug.WriteLine("Background notification access status unspecified");
                        return false;
                }

                // Subscribe to notification changes
                _listener.NotificationChanged += OnNotificationChanged;

                // Fetch existing notifications
                await FetchExistingNotifications();

                _isRunning = true;
                Debug.WriteLine("Background Notification Service started successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting Background Notification Service: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                Debug.WriteLine("Stopping Background Notification Service...");

                if (_listener != null)
                {
                    _listener.NotificationChanged -= OnNotificationChanged;
                }

                _isRunning = false;
                Debug.WriteLine("Background Notification Service stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping Background Notification Service: {ex.Message}");
            }
        }

        private async Task FetchExistingNotifications()
        {
            try
            {
                if (_listener == null) return;

                var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                foreach (UserNotification notification in notifications)
                {
                    await ProcessNotification(notification);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching existing notifications: {ex.Message}");
            }
        }

        private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            try
            {
                var notification = sender.GetNotification(args.UserNotificationId);
                if (notification != null)
                {
                    _ = Task.Run(async () => await ProcessNotification(notification));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling notification change: {ex.Message}");
            }
        }

        private async Task ProcessNotification(UserNotification notification)
        {
            try
            {
                string appDisplayName = notification.AppInfo.DisplayInfo.DisplayName;
                string appId = notification.AppInfo.AppUserModelId;
                uint notificationId = notification.Id;
                string packageName = string.IsNullOrWhiteSpace(notification.AppInfo.PackageFamilyName) ? appId : notification.AppInfo.PackageFamilyName;

                // Skip notifications from INotify itself to prevent infinite loops
                if (IsINotifyNotification(appId, packageName, appDisplayName))
                {
                    Debug.WriteLine($"Skipping INotify notification to prevent loop: {appDisplayName}");
                    return;
                }

                NotificationBinding toastBinding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                string iconLocation = string.Empty;

                try
                {
                    // Get the app's logo
                    BitmapImage appLogo = new BitmapImage();
                    RandomAccessStreamReference appLogoStream = notification.AppInfo?.DisplayInfo?.GetLogo(new Windows.Foundation.Size(64, 64));
                    if (appLogoStream != null)
                    {
                        iconLocation = await SaveAppIconToLocalFolder(appLogo, appLogoStream, appDisplayName);
                    }
                }
                catch (COMException ex)
                {
                    Debug.WriteLine($"Error getting app logo: {ex.Message}");
                }

                // Get the toast notification content
                if (toastBinding != null)
                {
                    IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();
                    string titleText = textElements.FirstOrDefault()?.Text ?? "No Title";
                    string bodyText = "\n";
                    foreach (var text in textElements)
                    {
                        bodyText += "\n" + text.Text;
                    }

                    KToastNotification data = new KToastNotification
                    {
                        NotificationTitle = titleText?.Trim(),
                        NotificationMessage = bodyText?.Trim(),
                        NotificationId = notificationId.ToString(),
                        CreatedTime = notification.CreationTime,
                        PackageFamilyName = packageName
                    };

                    KPackageProfile packageProfile = new KPackageProfile()
                    {
                        PackageFamilyName = packageName,
                        LogoFilePath = iconLocation,
                        AppDescription = string.Empty,
                        AppDisplayName = appDisplayName
                    };

                    KToastVObj kToastViewData = new KToastVObj(data, packageProfile);

                    // Update ViewModel (this will also store in database)
                    _viewModel?.UpdateKToastNotification(kToastViewData);

                    // Raise event for UI updates
                    NotificationEventInokerUtil.NotifyNotificationListened(new NotificationReceivedEventArgs(kToastViewData));

                    // Process for INotify toast creation (new feature)
                    await ProcessForINotifyToast(kToastViewData);

                    Debug.WriteLine($"Processed notification from {appDisplayName}: {titleText}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a notification is from INotify itself to prevent infinite loops
        /// </summary>
        private bool IsINotifyNotification(string appId, string packageName, string appDisplayName)
        {
            try
            {
                // Check if it's from INotify by comparing package info
                var currentPackage = Package.Current;
                var currentPackageFamily = currentPackage.Id.FamilyName;
                var currentAppDisplayName = currentPackage.DisplayName;

                return packageName == currentPackageFamily ||
                       appDisplayName == "INotify" ||
                       appDisplayName == currentAppDisplayName ||
                       appId.Contains("INotify");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking if INotify notification: {ex.Message}");
                // If we can't determine, err on the side of caution and allow the notification
                return false;
            }
        }

        /// <summary>
        /// Processes notification for potential INotify toast creation
        /// Only creates toast if the app is categorized (has priority or space assignment)
        /// </summary>
        private async Task ProcessForINotifyToast(KToastVObj kToastViewData)
        {
            try
            {
                if (_toastService != null)
                {
                    await _toastService.ProcessNotificationAsync(kToastViewData);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing notification for INotify toast: {ex.Message}");
            }
        }

        private async Task<string> SaveAppIconToLocalFolder(
            BitmapImage appLogo,
            RandomAccessStreamReference inputStream,
            string appName)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string fileName = $"{appName}.png";

            await _fileAccessSemaphore.WaitAsync();
            try
            {
                StorageFile existingFile = await localFolder.TryGetItemAsync(fileName) as StorageFile;
                if (existingFile != null)
                {
                    return existingFile.Path;
                }

                StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                using (IRandomAccessStream input = await inputStream.OpenReadAsync())
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(input);

                    var pixelData = await decoder.GetPixelDataAsync();
                    encoder.SetPixelData(
                        decoder.BitmapPixelFormat,
                        decoder.BitmapAlphaMode,
                        decoder.PixelWidth,
                        decoder.PixelHeight,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixelData.DetachPixelData());

                    await encoder.FlushAsync();
                }

                using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await appLogo.SetSourceAsync(readStream);
                }

                return file.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving app icon: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                _fileAccessSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            
            // UserNotificationListener doesn't implement IDisposable, so we just clear the reference
            _listener = null;
            _fileAccessSemaphore?.Dispose();
            _toastService?.Dispose();
            _disposed = true;
        }
    }
}