using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using static SampleNotify.StandaloneNotificationPositioner;

namespace SampleNotify
{
    public class StandaloneNotificationPositioner
    {
        #region Win32 API Declarations

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, [In, Out] MonitorInfo lpmi);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

        [DllImport("shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rectangle rectangle);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // SetWindowPos flags
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_SHOWWINDOW = 0x0040;

        #endregion

        #region Structures and Enums

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class MonitorInfo
        {
            public int Size = 72;
            public Rect Monitor = new Rect();
            public Rect WorkArea = new Rect();
            public uint Flags = 0;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName = "";
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }

        public enum NotificationPosition
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Custom
        }

        #endregion

        #region Properties and Fields

        public NotificationPosition Position { get; set; } = NotificationPosition.TopRight;
        public float CustomPositionPercentX { get; set; } = 0;
        public float CustomPositionPercentY { get; set; } = 0;
        public string PreferredMonitor { get; set; } = "primary";

        // Notification window characteristics
        private const int NOTIFICATION_WIDTH = 396;
        private const int NOTIFICATION_HEIGHT = 152;

        #endregion

        #region Monitor Detection Methods

        public List<string> GetAvailableMonitors()
        {
            var monitors = new List<string>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                {
                    MonitorInfo currentMonitorInfo = new MonitorInfo();
                    if (GetMonitorInfo(hMonitor, currentMonitorInfo))
                    {
                        monitors.Add(currentMonitorInfo.DeviceName);
                    }
                    return true;
                }, IntPtr.Zero);

            return monitors;
        }

        public IntPtr GetPreferredDisplay()
        {
            var returnedMonitor = IntPtr.Zero;

            if (PreferredMonitor != "primary")
            {
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                    {
                        MonitorInfo currentMonitorInfo = new MonitorInfo();
                        if (GetMonitorInfo(hMonitor, currentMonitorInfo) &&
                            currentMonitorInfo.DeviceName == PreferredMonitor)
                        {
                            returnedMonitor = hMonitor;
                        }
                        return true;
                    }, IntPtr.Zero);
            }

            return (returnedMonitor != IntPtr.Zero) ? returnedMonitor : MonitorFromPoint(new Point(0, 0), 0x00000001);
        }

        public Rectangle GetRealResolution()
        {
            var display = GetPreferredDisplay();
            MonitorInfo monitorInfo = new MonitorInfo();
            GetMonitorInfo(display, monitorInfo);
            return new Rectangle(0, 0,
                monitorInfo.Monitor.Right - monitorInfo.Monitor.Left,
                monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top);
        }

        public float GetScale()
        {
            uint dpiX;
            GetDpiForMonitor(GetPreferredDisplay(), DpiType.Effective, out dpiX, out _);
            return dpiX / 96f;
        }

        #endregion

        #region Notification Window Detection

        public List<IntPtr> FindNotificationWindows()
        {
            var notificationWindows = new List<IntPtr>();

            // Method 1: Find by class name (works for most languages)
            EnumWindows((hWnd, lParam) =>
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);

                if (className.ToString() == "Windows.UI.Core.CoreWindow")
                {
                    Rectangle rect = new Rectangle();
                    GetWindowRect(hWnd, ref rect);

                    // Check if it's notification-sized window
                    if (rect.Width == NOTIFICATION_WIDTH && rect.Height == NOTIFICATION_HEIGHT)
                    {
                        notificationWindows.Add(hWnd);
                    }
                }
                return true;
            }, IntPtr.Zero);

            // Method 2: Find by localized window title (you can add specific titles)
            var commonNotificationTitles = new[] { "New notification", "Nueva notificación", "Nouvelle notification" };
            foreach (var title in commonNotificationTitles)
            {
                var hwnd = FindWindow("Windows.UI.Core.CoreWindow", title);
                if (hwnd != IntPtr.Zero && !notificationWindows.Contains(hwnd))
                {
                    notificationWindows.Add(hwnd);
                }
            }

            return notificationWindows;
        }

        #endregion

        #region Positioning Methods

        public Point CalculateNotificationPosition()
        {
            var resolution = GetRealResolution();
            var scale = GetScale();
            var scaledWidth = (int)(NOTIFICATION_WIDTH * scale);
            var scaledHeight = (int)(NOTIFICATION_HEIGHT * scale);

            int x, y;

            switch (Position)
            {
                case NotificationPosition.TopLeft:
                    x = 0;
                    y = 0;
                    break;

                case NotificationPosition.TopRight:
                    x = resolution.Width - scaledWidth;
                    y = 0;
                    break;

                case NotificationPosition.BottomLeft:
                    x = 0;
                    y = resolution.Height - scaledHeight;
                    break;

                case NotificationPosition.BottomRight:
                    x = resolution.Width - scaledWidth;
                    y = resolution.Height - scaledHeight;
                    break;

                case NotificationPosition.Custom:
                    x = (int)(CustomPositionPercentX / 100f * resolution.Width);
                    y = (int)(CustomPositionPercentY / 100f * resolution.Height);
                    break;

                default:
                    x = resolution.Width - scaledWidth;
                    y = 0;
                    break;
            }

            return new Point(x, y);
        }

        public bool PositionNotification(IntPtr windowHandle)
        {
            var position = CalculateNotificationPosition();
            var result = SetWindowPos(windowHandle, 0, position.X, position.Y, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

            return result != IntPtr.Zero;
        }

        public void PositionAllNotifications()
        {
            var notificationWindows = FindNotificationWindows();
            foreach (var window in notificationWindows)
            {
                PositionNotification(window);
            }
        }

        #endregion

        #region Public Usage Methods

        /// <summary>
        /// Starts monitoring and positioning notifications
        /// </summary>
        public void StartPositioning()
        {
            var timer = new Timer();
            timer.Interval = 100; // Check every 100ms
            timer.Elapsed += (s, e) => PositionAllNotifications();
            timer.Start();
        }

        /// <summary>
        /// One-time positioning of current notifications
        /// </summary>
        public void PositionCurrentNotifications()
        {
            PositionAllNotifications();
        }

        #endregion
    }

    // Example usage class
  
}
