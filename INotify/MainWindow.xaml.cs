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
using Microsoft.UI.Windowing;
using Microsoft.UI;
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
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using WinRT;
using Microsoft.UI.Input;
using AppWindow = Microsoft.UI.Windowing.AppWindow;

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
        private AppWindow _appWindow;
        private IntPtr _hwnd;
        private OverlappedPresenter _presenter;
        public MainWindow()
        {
            this.InitializeComponent();
            GetAppWindowAndPresenter();
            _appWindow.IsShownInSwitchers = false;
            _presenter.SetBorderAndTitleBar(false, false);
            _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.BackgroundColor = Colors.Transparent;
            _appWindow.TitleBar.InactiveBackgroundColor = Colors.Transparent;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Set initial position & size
            _appWindow.Resize(new SizeInt32(300, 200));
            _appWindow.Move(new PointInt32(100, 100));
            CheckFeatureSupport();
            _listener.NotificationChanged += _listener_NotificationChanged;
        }


        public void GetAppWindowAndPresenter()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(myWndId);

            _presenter = _appWindow.Presenter as OverlappedPresenter;
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            //Gets the close button element. Refer to the Visual Tree.
            //var contentPresenter = VisualTreeHelper.GetParent(this.Content);
            //var layoutRoot = VisualTreeHelper.GetParent(contentPresenter);
            //var titleBar = VisualTreeHelper.GetChild(layoutRoot, 1) as Grid;
            //var buttonContainer = VisualTreeHelper.GetChild(titleBar, 0) as Grid;
            //buttonContainer.Visibility = Visibility.Collapsed;
            //var closeButton = VisualTreeHelper.GetChild(buttonContainer, 2) as Button;
            //if (closeButton != null)
            //{
            //    closeButton.Visibility = Visibility.Collapsed; //Hides the button.
            //}
        }

        //private void OpenNormalWindow(object sender, RoutedEventArgs e)
        //{
        //    NormalWindow normalWindow = new NormalWindow();
        //    normalWindow.Activate();
        //    this.Close(); // Close overlay if needed
        //}

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
            try
            {
                string appDisplayName = notif.AppInfo.DisplayInfo.DisplayName;
                string appId = notif.AppInfo.AppUserModelId;  // Get the App ID
                uint notificationId = notif.Id;  // Get the Notification ID

                string? s2 = notif.AppInfo.PackageFamilyName;
                NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                string iconLocation = string.Empty;
                try
                {
                    // Get the app's logo
                    BitmapImage appLogo = new BitmapImage();
                    RandomAccessStreamReference appLogoStream = notif.AppInfo?.DisplayInfo?.GetLogo(new Size(64,64));
                    if (appLogoStream != null)
                    {
                        iconLocation = await SaveAppIconToLocalFolder(appLogo, appLogoStream, appDisplayName);
                    }
                }
                catch(COMException exe)
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

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }
    }
}
