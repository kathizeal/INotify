using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using INotifyLibrary.Model.Entity;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace INotify
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        UserNotificationListener _listener = default;
        public MainWindow()
        {
            this.InitializeComponent();
            CheckFeatureSupport();
            _listener.NotificationChanged += _listener_NotificationChanged; ;

        }

     

        private void CheckFeatureSupport()
        {
            if (ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener"))
            {
                GetAccessFromUser();
                FetchToastNotifications();
            }
            else
            {
                // Listener not supported!
            }
        }


        // Todo Need to check with release mode how the access works
        public async void GetAccessFromUser()
        {
            _listener = UserNotificationListener.Current;
            UserNotificationListenerAccessStatus accessStatus = await _listener.RequestAccessAsync();
            switch (accessStatus)
            {
                // This means the user has granted access.
                case UserNotificationListenerAccessStatus.Allowed:

                    // Yay! Proceed as normal
                    break;

                // This means the user has denied access.
                // Any further calls to RequestAccessAsync will instantly
                // return Denied. The user must go to the Windows settings
                // and manually allow access.
                case UserNotificationListenerAccessStatus.Denied:

                    // Show UI explaining that listener features will not
                    // work until user allows access.
                    break;

                // This means the user closed the prompt without
                // selecting either allow or deny. Further calls to
                // RequestAccessAsync will show the dialog again.
                case UserNotificationListenerAccessStatus.Unspecified:

                    // Show UI that allows the user to bring up the prompt again
                    break;
            }

        }

        private async void FetchToastNotifications()
        {
            var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            foreach (UserNotification notification in notifications)
            {
                CreateKToastModel(notification);
            }
        }

        public async void CreateKToastModel(UserNotification notif) 
        {
            string appDisplayName = notif.AppInfo.DisplayInfo.DisplayName;
            string appId = notif.AppInfo.AppUserModelId;  // Get the App ID
            uint notificationId = notif.Id;  // Get the Notification ID

            string? s2 = notif.AppInfo.PackageFamilyName;
            NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);

            // Get the app's logo
            BitmapImage appLogo = new BitmapImage();
            RandomAccessStreamReference appLogoStream = notif.AppInfo?.DisplayInfo?.GetLogo(new Size(32, 32)) ??
                                                                       notif.AppInfo?.DisplayInfo?.GetLogo(new Size(48, 48)) ??
                                                                       notif.AppInfo?.DisplayInfo?.GetLogo(new Size(16, 16));
            if (appLogoStream != null)
            {
                await SaveAppIconToLocalFolder(appLogo, appLogoStream, appDisplayName);
                //using (IRandomAccessStream stream = await appLogoStream.OpenReadAsync())
                //{
                //    await appLogo.SetSourceAsync(stream);
                //}

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
                    NotificationTitle = titleText,
                    NotificationMessage = bodyText,
                    NotificationId = notificationId.ToString(),
                    PackageId = appId
                };

                KToastListViewControl.AddToastControl(data);
            }
        }
        private async Task<string> SaveAppIconToLocalFolder(BitmapImage appLogo, RandomAccessStreamReference inputStream, string appName)
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                string fileName = $"{appName}.png";  // Unique name based on AppInfo
                StorageFile file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(await inputStream.OpenReadAsync());
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

                return fileName; // Store only this in SQLite
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving image: {ex.Message}");
                return null;
            }
        }


        private void _listener_NotificationChanged(UserNotificationListener sender, Windows.UI.Notifications.UserNotificationChangedEventArgs args)
        {

        }
    }
}
