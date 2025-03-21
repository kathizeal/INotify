using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using INotify.KToastView.Model;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;

namespace INotifyLibrary.Util
{
    public static class KToastUtil
    {



        public static async Task<(BitmapImage, bool)> SetAppIcon(this KToastVObj kToastViewData)
        {
            try
            {
                if (kToastViewData.ToastPackageProfile != null)
                {
                    if (!string.IsNullOrWhiteSpace(kToastViewData.ToastPackageProfile.LogoFilePath))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(kToastViewData.ToastPackageProfile.LogoFilePath);
                        if (file != null)
                        {
                            kToastViewData.AppIcon = new BitmapImage();
                            using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                            {
                                await kToastViewData.AppIcon.SetSourceAsync(readStream);
                                return (kToastViewData.AppIcon, true);
                            }
                        }
                    }
                }
                return kToastViewData.SetDefaultAppIcon();
            }
            catch (Exception ex)
            {
                return kToastViewData.SetDefaultAppIcon();
            }
        }

        public static (BitmapImage, bool) SetDefaultAppIcon(this KToastVObj kToastViewData)
        {
            try
            {
                var defaultIconPath = IKSpaceConstant.DefaultWorkSpaceIcon;
                var defaultIcon = new BitmapImage(new Uri(defaultIconPath));
                kToastViewData.AppIcon = defaultIcon;
                return (kToastViewData.AppIcon, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set default app icon: {ex.Message}");
                return (null, false);
            }
        }

        public static async Task<(BitmapImage, bool)> PopulateAppIconAsync(this KPackageProfileVObj kPackageProfileVObj)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(kPackageProfileVObj.LogoFilePath))
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(kPackageProfileVObj.LogoFilePath);
                    if (file != null)
                    {
                        using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            await kPackageProfileVObj.AppIcon.SetSourceAsync(readStream);
                            return (kPackageProfileVObj.AppIcon, true);
                        }
                    }
                }
                return kPackageProfileVObj.SetDefaultAppIcon();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load app icon: {ex.Message}");
                return kPackageProfileVObj.SetDefaultAppIcon();
            }
        }

        private static (BitmapImage, bool) SetDefaultAppIcon(this KPackageProfileVObj kPackageProfileVObj)
        {
            try
            {
                var defaultIconPath = IKSpaceConstant.DefaultWorkSpaceIcon;
                var defaultIcon = new BitmapImage(new Uri(defaultIconPath));
                kPackageProfileVObj.AppIcon = defaultIcon;
                return (kPackageProfileVObj.AppIcon, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set default app icon: {ex.Message}");
                return (null, false);
            }
        }


        public static async Task<(BitmapImage, bool)> SetViewSpaceIcon(this KSpaceVObj kToastViewSpace)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(kToastViewSpace.SpaceIconLogoPath) && !kToastViewSpace.IsDefaultWorkSpace)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(kToastViewSpace.SpaceIconLogoPath);
                    if (file != null)
                    {
                        kToastViewSpace.AppIcon = new BitmapImage();
                        using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            await kToastViewSpace.AppIcon.SetSourceAsync(readStream);
                            return (kToastViewSpace.AppIcon, true);
                        }
                    }
                }
                return kToastViewSpace.SetDefaultViewSpaceIcon();
            }
            catch (Exception ex)
            {
                return kToastViewSpace.SetDefaultViewSpaceIcon();
            }
        }

        public static (BitmapImage, bool) SetDefaultViewSpaceIcon(this KSpaceVObj kToastViewSpace)
        {
            try
            {
                var defaultIconPath = IKSpaceConstant.DefaultWorkSpaceIcon;
                var defaultIcon = new BitmapImage(new Uri(defaultIconPath));
                kToastViewSpace.AppIcon = defaultIcon;
                return (kToastViewSpace.AppIcon, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set default app icon: {ex.Message}");
                return (null, false);
            }
        }

        /// <summary>
        /// Inserts a KToastNotification into a sorted list based on the CreatedTime property.
        /// </summary>
        /// <typeparam name="T">The type of the list elements, must be KToastNotification or derived from it.</typeparam>
        /// <param name="list">The list of KToastNotification objects.</param>
        /// <param name="notification">The KToastNotification object to insert.</param>
        public static void InsertNotificationByCreatedTime(IList<KToastVObj> list, KToastVObj notification)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            // Check if the list already contains a notification with the same NotificationId
            if (list.Any(n => n.NotificationData.NotificationId == notification.NotificationData.NotificationId))
                return;

            int index = BinarySearch(list, notification, new KToastNotificationComparer());
            if (index < 0)
                index = ~index;

            list.Insert(index, notification);
        }

        private static int BinarySearch(IList<KToastVObj> list, KToastVObj item, IComparer<KToastVObj> comparer)
        {
            int low = 0;
            int high = list.Count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                int cmp = comparer.Compare(list[mid], item);

                if (cmp == 0)
                    return mid;
                if (cmp > 0) // Change comparison to sort in descending order
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return ~low;
        }

        private class KToastNotificationComparer : IComparer<KToastVObj>
        {
            public int Compare(KToastVObj x, KToastVObj y)
            {
                if (x == null || y == null)
                    throw new ArgumentNullException();

                return x.NotificationData.CreatedTime.CompareTo(y.NotificationData.CreatedTime); // Change comparison to sort in descending order
            }
        }



    }
}
