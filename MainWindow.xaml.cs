using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace WallpaperBlur
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public double BlurStrength { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            var blur = new BlurEffect();
            blur.KernelType = KernelType.Gaussian;
            DoubleAnimation animation;
            if (CheckActive())
            {
                if (((BlurEffect)Paper.Effect).Radius >= BlurStrength)
                {
                    animation = new DoubleAnimation
                    {
                        From = BlurStrength,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(.3d),
                        IsCumulative = true,
                    };
                    blur.BeginAnimation(BlurEffect.RadiusProperty, animation);
                    blur.RenderingBias = RenderingBias.Performance;
                    Paper.Effect = blur;
                }
            }
            else
            {
                if (((BlurEffect)Paper.Effect).Radius <= 0)
                {
                    animation = new DoubleAnimation
                    {
                        From = 0,
                        To = BlurStrength,
                        Duration = TimeSpan.FromSeconds(.3d),
                        IsCumulative = true,
                    };
                    blur.BeginAnimation(BlurEffect.RadiusProperty, animation);
                    blur.RenderingBias = RenderingBias.Performance;
                    Paper.Effect = blur;
                }
            }

        }

        private bool CheckActive()
        {
            if (GetWindowText((IntPtr)GetForegroundWindow()) == string.Empty)
            {
                return true;
            }
            return false;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string wallpaperPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", null);
            if (File.Exists(wallpaperPath))
            {
                Paper.Source = (ImageSourceFromBitmap(new Bitmap(wallpaperPath)));
            }


            if (BlurStrength == 0) BlurStrength = 25;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(TimerTick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 700);
            dispatcherTimer.Start();


            BorderBrush = null;

            IntPtr hProgman = FindWindow("ProgMan", "Program Manager");
            IntPtr result = IntPtr.Zero;

            SendMessageTimeout(hProgman,
                       0x052C,
                       new IntPtr(0),
                       IntPtr.Zero,
                       0,
                       1000,
                       out result);

            IntPtr hWorkerW = IntPtr.Zero;

            EnumWindows(new EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            string.Empty);

                if (p != IntPtr.Zero)
                {
                    hWorkerW = FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               string.Empty);
                }

                return true;
            }), IntPtr.Zero);


            var hThis = new WindowInteropHelper(this).Handle;
            SetParent(hThis, hWorkerW);
            Visibility = Visibility.Visible;
            WindowState = WindowState.Maximized;
        }

        [DllImport("user32.dll")]
        static extern int GetDesktopWindow();
        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetClassName(int hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("User32.dll", SetLastError = true)]
        public static extern int SendMessageTimeout(
            IntPtr hWnd,
            int uMsg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        public void GetActiveWindow()
        {
            const int maxChars = 256;
            int handle = 0;
            StringBuilder className = new StringBuilder(maxChars);

            handle = GetForegroundWindow();

            if (GetClassName(handle, className, maxChars) > 0)
            {
                string cName = className.ToString();
                if (cName == "ProgMan" || cName == "WorkerW")
                {

                    MessageBox.Show(cName);
                }
            }
        }

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void Window_Closed()
        {
            SetImage();
            this.Close();

            Thread.Sleep(100);
            TrayIcon.Visibility = Visibility.Hidden;
            Application.Current.Shutdown(0);
        }
        public void SetImage()
        {
            RegistryKey theCurrentMachine = Registry.CurrentUser;
            RegistryKey theControlPanel = theCurrentMachine.OpenSubKey("Control Panel");
            RegistryKey theDesktop = theControlPanel.OpenSubKey("Desktop");
            SystemParametersInfo(20, 0, Convert.ToString(theDesktop.GetValue("Wallpaper")), 0x01);
        }

        [DllImport("user32.dll")]
        static extern bool SystemParametersInfo(int uiAction, uint uiParam, string pvParam, int fWinIni);

        private void Label_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window_Closed();
        }
    }
}
