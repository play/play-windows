using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using System.Windows.Media;
using ReactiveUI;
using ReactiveUI.Xaml;

namespace Play
{
    public class BorderlessWindowBehavior : Behavior<Window>
    {
        HwndSource hwndSource;
        IntPtr hwnd;
        POINT minimumSize;
        IDisposable dragMoveHandle;
        bool isHardwareRenderingEnabled;

        protected override void OnAttached()
        {
            if (AssociatedObject.IsInitialized)
                AddHwndHook();
            else
                AssociatedObject.SourceInitialized += AssociatedObject_SourceInitialized;

            AssociatedObject.WindowStyle = WindowStyle.None;
            AssociatedObject.ResizeMode = ResizeMode.CanResizeWithGrip;

            dragMoveHandle = Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(x => AssociatedObject.MouseLeftButtonDown += x, x => AssociatedObject.MouseLeftButtonDown -= x)
                .Where(e => e.EventArgs.LeftButton == MouseButtonState.Pressed)
                .Subscribe(_ => AssociatedObject.DragMove());

            Observable.Merge(AssociatedObject.ObservableFromDP(x => x.MinHeight), AssociatedObject.ObservableFromDP(x => x.MinWidth))
                .StartWith((ObservedChange<Window, double>)null)
                .ObserveOn(RxApp.DeferredScheduler)
                .Subscribe(_ =>
                {
                    var source = PresentationSource.FromVisual(AssociatedObject);
                    if (source == null) return;
                    var deviceMinSize = source.CompositionTarget.TransformToDevice.Transform(new Point(AssociatedObject.MinWidth, AssociatedObject.MinHeight));
                    minimumSize = new POINT((int)deviceMinSize.X, (int)deviceMinSize.Y);
                });

            isHardwareRenderingEnabled = (Environment.OSVersion.Version.Major >= 6 && !HardwareRenderingHelper.IsInSoftwareMode);

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            dragMoveHandle.Dispose();
            RemoveHwndHook();
            base.OnDetaching();
        }

        void AddHwndHook()
        {
            hwndSource = PresentationSource.FromVisual(AssociatedObject) as HwndSource;
            hwndSource.AddHook(HwndHook);
            hwnd = new WindowInteropHelper(AssociatedObject).Handle;

            SizeWindowToScreen();
        }

        void SizeWindowToScreen()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
            if (screen.WorkingArea.Width < AssociatedObject.Width || screen.WorkingArea.Height < AssociatedObject.Height)
            {
                if (screen.WorkingArea.Width < AssociatedObject.Width)
                    AssociatedObject.Width = screen.WorkingArea.Width;
                if (screen.WorkingArea.Height < AssociatedObject.Height)
                    AssociatedObject.Height = screen.WorkingArea.Height;
                AssociatedObject.Left = screen.WorkingArea.Left;
                AssociatedObject.Top = screen.WorkingArea.Top;
            }
        }

        void RemoveHwndHook()
        {
            AssociatedObject.SourceInitialized -= AssociatedObject_SourceInitialized;
            hwndSource.RemoveHook(HwndHook);
        }

        void AssociatedObject_SourceInitialized(object sender, EventArgs e)
        {
            AddHwndHook();
            SetDefaultBackgroundColor();
        }

        // From https://github.com/MahApps/MahApps.Metro/blob/master/MahApps.Metro/Behaviours/BorderlessWindowBehavior.cs thanks to @aeoth
        void SetDefaultBackgroundColor()
        {
            var bgSolidColorBrush = AssociatedObject.Background as SolidColorBrush;

            if (bgSolidColorBrush != null)
            {
                var rgb = bgSolidColorBrush.Color.R | (bgSolidColorBrush.Color.G << 8) | (bgSolidColorBrush.Color.B << 16);

                // set the default background color of the window -> this avoids the black stripes when resizing
                var hBrushOld = SetClassLong(hwnd, NativeConstants.GCLP_HBRBACKGROUND, UnsafeNativeMethods.CreateSolidBrush(rgb));

                if (hBrushOld != IntPtr.Zero)
                    UnsafeNativeMethods.DeleteObject(hBrushOld);
            }
        }

        // From https://github.com/MahApps/MahApps.Metro/blob/master/MahApps.Metro/Behaviours/BorderlessWindowBehavior.cs thanks to @aeoth
        static IntPtr SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size > 4)
                return UnsafeNativeMethods.SetClassLongPtr64(hWnd, nIndex, dwNewLong);

            return new IntPtr(UnsafeNativeMethods.SetClassLongPtr32(hWnd, nIndex, unchecked((uint)dwNewLong.ToInt32())));
        }

        readonly IntPtr intPtrOne = new IntPtr(1);

        IntPtr HwndHook(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (message)
            {
                case NativeConstants.WM_NCCALCSIZE:
                    /* Hides the border */
                    if (wParam == intPtrOne) handled = true;
                    break;
                case NativeConstants.WM_NCPAINT:
                    if (isHardwareRenderingEnabled)
                    {
                        var m = new MARGINS { bottomHeight = 1, leftWidth = 1, rightWidth = 1, topHeight = 1 };
                        UnsafeNativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref m);
                    }
                    break;
                case NativeConstants.WM_NCACTIVATE:
                    /* As per http://msdn.microsoft.com/en-us/library/ms632633(VS.85).aspx , "-1" lParam
                     * "does not repaint the nonclient area to reflect the state change." */
                    var res = UnsafeNativeMethods.DefWindowProc(hWnd, message, wParam, new IntPtr(-1));
                    handled = true;
                    return res;
                case NativeConstants.WM_GETMINMAXINFO:
                    /* From Lester's Blog (thanks @aeoth):
                     * http://blogs.msdn.com/b/llobo/archive/2006/08/01/maximizing-window-_2800_with-windowstyle_3d00_none_2900_-considering-taskbar.aspx */
                    UnsafeNativeMethods.WmGetMinMaxInfo(hWnd, lParam, minimumSize);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// POINT aka POINTAPI
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;

        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MONITORINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        public static readonly RECT Empty;

        public int Width
        {
            get { return Math.Abs(right - left); } // Abs needed for BIDI OS
        }

        public int Height
        {
            get { return bottom - top; }
        }

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public RECT(RECT rcSrc)
        {
            left = rcSrc.left;
            top = rcSrc.top;
            right = rcSrc.right;
            bottom = rcSrc.bottom;
        }

        public bool IsEmpty
        {
            get
            {
                // BUGBUG : On Bidi OS (hebrew arabic) left > right
                return left >= right || top >= bottom;
            }
        }

        /// <summary> Return a user friendly representation of this struct </summary>
        public override string ToString()
        {
            if (this == Empty)
            {
                return "RECT {Empty}";
            }
            return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
        }

        /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Rect))
            {
                return false;
            }
            return (this == (RECT)obj);
        }

        /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
        public override int GetHashCode()
        {
            return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
        }

        /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
        public static bool operator ==(RECT rect1, RECT rect2)
        {
            return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
        }

        /// <summary> Determine if 2 RECT are different(deep compare)</summary>
        public static bool operator !=(RECT rect1, RECT rect2)
        {
            return !(rect1 == rect2);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int leftWidth;
        public int rightWidth;
        public int topHeight;
        public int bottomHeight;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public int showCmd;
        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;
    }

    // Pulled just the ones we needed from 
    // https://github.com/MahApps/MahApps.Metro/blob/master/MahApps.Metro/Native/Constants.cs
    internal static class NativeConstants
    {
        public const int GCLP_HBRBACKGROUND = -0x0A;
        public const int WM_NCCALCSIZE = 0x83;
        public const int WM_NCPAINT = 0x85;
        public const int WM_NCACTIVATE = 0x86;
        public const int WM_GETMINMAXINFO = 0x24;
    }

    // http://msdn.microsoft.com/en-us/library/ms182161.aspx
    // Thanks to @aeoth and https://github.com/MahApps/MahApps.Metro/blob/master/MahApps.Metro/Native/UnsafeNativeMethods.cs
    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        /// <summary>
        /// 
        /// </summary>
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        internal static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam, POINT minSize)
        {
            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
                mmi.ptMinTrackSize.x = minSize.x;
                mmi.ptMinTrackSize.y = minSize.y;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr DefWindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        /// <devdoc>http://msdn.microsoft.com/en-us/library/dd144901%28v=VS.85%29.aspx</devdoc>
        [DllImport("user32", EntryPoint = "GetMonitorInfoW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo([In] IntPtr hMonitor, [Out] MONITORINFO lpmi);

        [DllImport("user32.dll", EntryPoint = "SetClassLong")]
        internal static extern uint SetClassLongPtr32(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
        internal static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateSolidBrush(int crColor);

        [DllImport("user32.dll")]
        internal static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
    }
}
