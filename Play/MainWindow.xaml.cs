using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hardcodet.Wpf.TaskbarNotification;
using Play.ViewModels;
using ReactiveUI;
using ReactiveUI.Xaml;

namespace Play
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public AppBootstrapper AppBootstrapper { get; protected set; }

        readonly Dictionary<ResizeDirection, Cursor> resizeCursors = new Dictionary<ResizeDirection, Cursor> {
            { ResizeDirection.Top, Cursors.SizeNS },
            { ResizeDirection.Bottom, Cursors.SizeNS },
            { ResizeDirection.Left, Cursors.SizeWE },
            { ResizeDirection.Right, Cursors.SizeWE },
            { ResizeDirection.TopLeft, Cursors.SizeNWSE },
            { ResizeDirection.TopRight, Cursors.SizeNESW },
            { ResizeDirection.BottomLeft, Cursors.SizeNESW },
            { ResizeDirection.BottomRight, Cursors.SizeNWSE },
        };

        TaskbarIcon taskbarIcon;

        public MainWindow()
        {
            InitializeComponent();

            AppBootstrapper = new AppBootstrapper();
            DataContext = AppBootstrapper;

            UpdateDwmBorder();

            taskbarIcon = new TaskbarIcon();

            MessageBus.Current.Listen<bool>("IsPlaying").Subscribe(x => {
                taskbarIcon.IconSource = x ?
                   new BitmapImage(new Uri("pack://application:,,,/Play;component/Images/status-icon-on.ico")) :
                   new BitmapImage(new Uri("pack://application:,,,/Play;component/Images/status-icon-off.ico"));

                taskbarIcon.Visibility = Visibility.Visible;
            });

            taskbarIcon.LeftClickCommand = ReactiveCommand.Create(_ => true, _ => {
                if (WindowState == WindowState.Minimized) {
                    WindowState = WindowState.Normal;
                }

                Show();
            });
        }

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        public void HideDragBorder()
        {
            windowFocusBorder.Visibility = Visibility.Hidden;
        }

        public void ShowDragBorder()
        {
            windowFocusBorder.Visibility = Visibility.Visible;
        }

        [DebuggerStepThrough]
        void HandleHeaderPreviewMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                // double click to maximize/restore is only enable for top 28 px of the window
                if (Mouse.GetPosition(this).Y <= 28)
                    Restore(sender, e);
            }
        }

        [DebuggerStepThrough]
        void HandlePreviewMouseMove(Object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) return;
            Cursor = Cursors.Arrow;
        }

        [DebuggerStepThrough]
        void HandleRectangleMouseMove(Object sender, MouseEventArgs e)
        {
            Resize(sender as Rectangle);
        }

        [DebuggerStepThrough]
        void HandleRectanglePreviewMouseDown(Object sender, MouseButtonEventArgs e)
        {
            Resize(sender as Rectangle, true);
        }

        void Minimize(Object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            WindowResized(WindowState == WindowState.Maximized);
        }

        void Resize(Rectangle borderRectangle, bool doResize = false)
        {
            ResizeDirection direction;
            if (borderRectangle == top)
                direction = ResizeDirection.Top;
            else if (borderRectangle == bottom)
                direction = ResizeDirection.Bottom;
            else if (borderRectangle == left)
                direction = ResizeDirection.Left;
            else if (borderRectangle == right)
                direction = ResizeDirection.Right;
            else if (borderRectangle == topLeft)
                direction = ResizeDirection.TopLeft;
            else if (borderRectangle == topRight)
                direction = ResizeDirection.TopRight;
            else if (borderRectangle == bottomLeft)
                direction = ResizeDirection.BottomLeft;
            else if (borderRectangle == bottomRight)
                direction = ResizeDirection.BottomRight;
            else return;

            Cursor = resizeCursors[direction];

            if (doResize)
            {
                var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
                if (hwndSource == null) return;

                const uint WM_SYSCOMMAND = 0x112;
                SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
            }
        }

        void Restore(Object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        void UpdateDwmBorder()
        {
            if (WindowState == WindowState.Maximized)
            {
                noDwmBorder.Visibility = Visibility.Hidden;
                return;
            }

            var dwmEnabled = (Environment.OSVersion.Version.Major >= 6 && !HardwareRenderingHelper.IsInSoftwareMode && DwmIsCompositionEnabled());

            noDwmBorder.Visibility = (dwmEnabled && IsActive) ? Visibility.Hidden : Visibility.Visible;
        }

        [DebuggerStepThrough]
        void WindowResized(bool maximized)
        {
            frameGrid.IsHitTestVisible = !maximized;
            UpdateDwmBorder();
        }

        void Closeify(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public enum ResizeDirection {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8,
    }
}
