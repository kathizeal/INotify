using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using INotify.KToastView.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
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
using WinToast;
using static WinToast.NotificationForm;

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
            _listener.NotificationChanged += _listener_NotificationChanged;
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

        public async void GetAccessFromUser()
        {
            _listener = UserNotificationListener.Current;
            UserNotificationListenerAccessStatus accessStatus = await _listener.RequestAccessAsync();
            switch (accessStatus)
            {
                case UserNotificationListenerAccessStatus.Allowed:

                    // Yay! Proceed as normal
                    break;
                case UserNotificationListenerAccessStatus.Denied:
                    // Show UI explaining that listener features will not work until user allows access.
                    break;
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
            try
            {
                string appDisplayName = notif.AppInfo.DisplayInfo.DisplayName;
                string appId = notif.AppInfo.AppUserModelId;
                uint notificationId = notif.Id;

                string? s2 = notif.AppInfo.PackageFamilyName;
                NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                string iconLocation = string.Empty;
                try
                {
                    // Get the app's logo
                    BitmapImage appLogo = new BitmapImage();
                    RandomAccessStreamReference appLogoStream = notif.AppInfo?.DisplayInfo?.GetLogo(new Windows.Foundation.Size(64, 64));
                    if (appLogoStream != null)
                    {
                        iconLocation = await SaveAppIconToLocalFolder(appLogo, appLogoStream, appDisplayName);
                    }
                }
                catch (COMException exe)
                {

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
                        CreatedTime = notif.CreationTime,
                        PackageId = appId
                    };

                    KPackageProfile packageProfile = new KPackageProfile()
                    {
                        PackageFamilyName = notif.AppInfo.PackageFamilyName,
                        PackageId = appId,
                        LogoFilePath = iconLocation,
                        AppDescription = string.Empty,
                        AppDisplayName = notif.AppInfo.DisplayInfo.DisplayName  
                    };
                    KToastVObj kToastViewData = new KToastVObj(data, packageProfile);
                    KToastListViewControl.AddToastControl(kToastViewData);
                }
            }
            catch (Exception ex)
            {

            }
           
        }
        private static readonly SemaphoreSlim _fileAccessSemaphore = new SemaphoreSlim(1, 1);

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


        private void _listener_NotificationChanged(UserNotificationListener sender, Windows.UI.Notifications.UserNotificationChangedEventArgs args)
        {
            var notification = sender.GetNotification(args.UserNotificationId);
            DispatcherQueue.TryEnqueue(() =>
            {
                if (notification != null) CreateKToastModel(notification);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Show test notifications
        }

        private void TopRight_Click(object sender, RoutedEventArgs e)
        {
            NotificationForm.ShowNotification("Test message. This is a test notification.", NotificationPosition.TopRight);
            NotificationForm.ShowNotification("Another test message.", NotificationPosition.TopRight);
        }

        private void TopLeft_Click(object sender, RoutedEventArgs e)
        {
            NotificationForm.ShowNotification("Test message. This is a test notification.", NotificationPosition.TopLeft);
            NotificationForm.ShowNotification("Another test message.", NotificationPosition.TopLeft);
        }

        private void TopMiddle_Click(object sender, RoutedEventArgs e)
        {
            NotificationForm.ShowNotification("Test message. This is a test notification.", NotificationPosition.TopMiddle);
            NotificationForm.ShowNotification("Another test message.", NotificationPosition.TopMiddle);
        }

        private void BottomRight_Click(object sender, RoutedEventArgs e)
        {
            NotificationForm.ShowNotification("Test message. This is a test notification.", NotificationPosition.BottomRight);
            NotificationForm.ShowNotification("Another test message.", NotificationPosition.BottomRight);
        }

        private void BottomLeft_Click(object sender, RoutedEventArgs e)
        {
            NotificationForm.ShowNotification("Test message. This is a test notification.", NotificationPosition.BottomLeft);
            NotificationForm.ShowNotification("Another test message.", NotificationPosition.BottomLeft);
        }

        private void BottomMiddle_Click(object sender, RoutedEventArgs e)
        {
            NotificationForm.ShowNotification("Test message. This is a test notification.", NotificationPosition.BottomMiddle);
            NotificationForm.ShowNotification("Another test message.", NotificationPosition.BottomMiddle);
        }
       
        private void SpaceControl_SpaceSelected(string spaceId)
        {
            KToastListViewControl.GetPackageBySpace(spaceId);
        }

        private void GetAllPackage_Click(object sender, RoutedEventArgs e)
        {
            KToastListViewControl.UpdateToastView(ViewType.Package);
        }
    }
}
