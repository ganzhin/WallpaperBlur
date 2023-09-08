using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WallpaperBlur
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public MainWindow Background;
        private string[] _settings;
        private string _setFile = AppDomain.CurrentDomain.BaseDirectory + @"WallpaperBlur.set";

        public Settings()
        {
            InitializeComponent();
        }

        private void SaveSettings()
        {
            using (var tw = new StreamWriter(_setFile, false))
            {
                tw.WriteLine(AutoRunBox.IsChecked.ToString());
                tw.WriteLine(BlurRadiusSlider.Value.ToString());
                tw.WriteLine(BlurTypeCombo.SelectedIndex.ToString());
                tw.WriteLine(RainCompatibility.IsChecked.ToString());
            }
        }

        private void TryLoadSettings()
        {
            try
            {
                _settings = File.ReadAllLines(_setFile);
                AutoRunBox.IsChecked = bool.Parse(_settings[0]);
                BlurRadiusSlider.Value = double.Parse(_settings[1]);
                BlurTypeCombo.SelectedIndex = int.Parse(_settings[2]);
                RainCompatibility.IsChecked = bool.Parse(_settings[3]);
                Background.RainCompatibility = (bool)RainCompatibility.IsChecked;
            }
            catch
            {
                SaveSettings();
            }
        }

        private void AutoRunBox_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (AutoRunBox.IsChecked == true)
                rk.SetValue(Application.Current.ToString(), AppDomain.CurrentDomain.BaseDirectory + @"WallpaperBlur.exe");
            else
                rk.DeleteValue(Application.Current.ToString(), false);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Background.CloseApp();
        }

        private void BlurRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Background.BlurStrength = BlurRadiusSlider.Value;
            Background.ShowSettingsEffect();
        }

        private void ApplyBlurButton_Click(object sender, RoutedEventArgs e)
        {
            Background.BlurType = (KernelType)BlurTypeCombo.SelectedItem;
            Background.ShowSettingsEffect();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            Background.SettingsHide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BlurTypeCombo.Items.Add(KernelType.Gaussian);
            BlurTypeCombo.Items.Add(KernelType.Box);
            BlurTypeCombo.SelectedIndex = 0;

            if (File.Exists(_setFile))
            {
                TryLoadSettings();
            }
            else
            {
                MessageBox.Show("Настройки хранятся в файле " + _setFile);
                SaveSettings();
            }
        }

        private void RainCompatibility_Click(object sender, RoutedEventArgs e)
        {
            Background.RainCompatibility = (bool)RainCompatibility.IsChecked;
        }
    }
}
