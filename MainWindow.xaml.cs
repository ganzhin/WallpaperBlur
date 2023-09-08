using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
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
        public KernelType BlurType { get; set; }
        public bool RainCompatibility { get; set; }

        private bool _settingsShowed = false;
        
        public MainWindow()
        {
            InitializeComponent();
        }
        private void TimerTick(object sender, EventArgs e)
        {
            var blur = new BlurEffect();
            blur.KernelType = BlurType;
            if (CheckActiveWindowUnnamed())
            {
                if (((BlurEffect)Paper.Effect).Radius >= BlurStrength)
                {
                    AnimateBlur(blur, BlurStrength, 0);
                }
            }
            else
            {
                if (((BlurEffect)Paper.Effect).Radius <= 0)
                {
                    AnimateBlur(blur, 0, BlurStrength);
                }
            }
        }
        public void ShowSettingsEffect()
        {
            var blur = new BlurEffect();
            blur.KernelType = BlurType;
            AnimateBlur(blur, BlurStrength-1, BlurStrength);
        }
        private void AnimateBlur(BlurEffect blur, double from, double to)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(.3d),
                IsCumulative = true,
            };
            blur.BeginAnimation(BlurEffect.RadiusProperty, animation);
            blur.RenderingBias = RenderingBias.Performance;
            Paper.Effect = blur;
        }
        private bool CheckActiveWindowUnnamed()
        {
            string windowText = GetWindowText(Win32.GetForegroundWindow());
            if (windowText == string.Empty || windowText == "Пуск" || windowText == "Start" || (RainCompatibility && windowText.Contains("Rainmeter")) || Win32.IsIconic(Win32.GetForegroundWindow()))
            {
                return true;
            }
            return false;
        }
        public static string GetWindowText(IntPtr hWnd)
        {
            int size = Win32.GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                Win32.GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }
        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { Win32.DeleteObject(handle); }
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
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            dispatcherTimer.Start();

            SetBehindIcons();
            ShowSettings();
        }
        private void SetBehindIcons()
        {
            BorderBrush = null;

            IntPtr hProgman = Win32.FindWindow("ProgMan", "Program Manager");
            IntPtr result = IntPtr.Zero;

            Win32.SendMessageTimeout(hProgman,
                0x052C,
                new IntPtr(0),
                IntPtr.Zero,
                0,
                1000,
                out result);

            IntPtr hWorkerW = IntPtr.Zero;

            Win32.EnumWindows(new Win32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = Win32.FindWindowEx(
                    tophandle, IntPtr.Zero, "SHELLDLL_DefView", string.Empty);

                if (p != IntPtr.Zero)
                {
                    hWorkerW = Win32.FindWindowEx(
                        IntPtr.Zero, tophandle, "WorkerW", string.Empty);
                }

                return true;
            }), IntPtr.Zero);

            var hAfterLowest = IntPtr.Zero;
            Win32.EnumChildWindows(hWorkerW, new Win32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = Win32.FindWindowEx(tophandle, IntPtr.Zero, null, null);

                if (p == IntPtr.Zero)
                {
                    hAfterLowest = tophandle;
                    return false;
                }
                return true;

            }), IntPtr.Zero);

            if (hAfterLowest == IntPtr.Zero)
            {
                hAfterLowest = hWorkerW;
            }

            var hThis = new WindowInteropHelper(this).Handle;
            Win32.SetParent(hThis, hAfterLowest);

            Visibility = Visibility.Visible;
            WindowState = WindowState.Maximized;
        }
        public void CloseApp()
        {
            ResetWallpaper();
            this.Close();

            Thread.Sleep(100);
            TrayIcon.Visibility = Visibility.Hidden;
            Application.Current.Shutdown(0);
        }
        public void ResetWallpaper()
        {
            RegistryKey theCurrentMachine = Registry.CurrentUser;
            RegistryKey theControlPanel = theCurrentMachine.OpenSubKey("Control Panel");
            RegistryKey theDesktop = theControlPanel.OpenSubKey("Desktop");
            Win32.SystemParametersInfo(20, 0, Convert.ToString(theDesktop.GetValue("Wallpaper")), 0x01);
        }
        private void Label_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CloseApp();
        }
        private void SettingsContext_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowSettings();
        }
        private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }
        private void ShowSettings()
        {
            if (_settingsShowed) { return; }

            var settings = new Settings()
            {
                Background = this
            };
            settings.Show();
            _settingsShowed = true;
        }
        public void SettingsHide()
        {
            _settingsShowed = false;
        }
    }
}
