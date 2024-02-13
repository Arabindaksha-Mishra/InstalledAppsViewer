using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Microsoft.Win32;



namespace InstalledAppsViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PopulateInstalledApps();
        }
        private ImageSource GetIconPath(string path)
        {
            return IconExtractor.Extract(path);      // Assuming you have an Image control named MyImageControl

        }
        public ImageSource ConvertStringIconPathToImageSource(string iconPath)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(iconPath, UriKind.Absolute);
            image.EndInit();
            return image;
        }

        private ImageSource ToImageSource(Icon icon)
        {
            // Converts an Icon to an ImageSource that can be used in WPF.
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        private void PopulateInstalledApps()
        {
            var appList = FindInstalledApps();

            foreach (var app in appList)
            {
                appListBox.Items.Add(app);
            }
        }

        private List<AppInfo> FindInstalledApps()
        {
            var installedApps = new List<AppInfo>();

            // Search in HKEY_CURRENT_USER
            FindInstalledApps(Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"), installedApps);

            // Search in HKEY_LOCAL_MACHINE
            FindInstalledApps(Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"), installedApps);

            // Search in additional registry paths for Google applications
            FindInstalledApps(Registry.CurrentUser.OpenSubKey(@"Software\Google"), installedApps);
            FindInstalledApps(Registry.LocalMachine.OpenSubKey(@"Software\Google"), installedApps);

            return installedApps;
        }

        private void FindInstalledApps(RegistryKey key, List<AppInfo> installedApps)
        {
            if (key != null)
            {
                foreach (string subKeyName in key.GetSubKeyNames())
                {
                    using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                    {
                        var publisher = subKey?.GetValue("Publisher") as string;
                        var displayName = subKey?.GetValue("DisplayName") as string;
                        if ((publisher != null && (publisher.ToLower().Contains("google") || publisher.ToLower().Contains("google llc"))) ||
                            (displayName != null && displayName.ToLower().Contains("google")))
                        {
                            ImageSource iconPath = ExtractIconPath(subKey);
                            string executablePath = GetExecutablePath(subKey);
                            installedApps.Add(new AppInfo { Name = displayName, IconPath = iconPath, ExecutablePath = executablePath });
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Registry key not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetExecutablePath(RegistryKey key)
        {
            var installLocation = key.GetValue("InstallLocation") as string;
            var uninstallString = key.GetValue("UninstallString") as string;
            if (!string.IsNullOrEmpty(installLocation))
            {
                return installLocation;
            }
            else if (!string.IsNullOrEmpty(uninstallString))
            {
                int startIndex = uninstallString.IndexOf("\"", StringComparison.Ordinal);
                int endIndex = uninstallString.IndexOf("\"", startIndex + 1, StringComparison.Ordinal);
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    return uninstallString.Substring(startIndex + 1, endIndex - startIndex - 1);
                }
            }
            return null;
        }

        private ImageSource ExtractIconPath(RegistryKey key)
        {
            var displayIcon = key.GetValue("DisplayIcon") as string;

            if (!string.IsNullOrEmpty(displayIcon))
            {
                if (displayIcon.Contains(",0"))
                {
                    return GetIconPath(displayIcon.Substring(0, displayIcon.Length - 2));
                }


                return ConvertStringIconPathToImageSource(displayIcon);
            }
            return null;
        }

        private void ExecuteApplication(string executablePath)
        {
            try
            {
                Process.Start(executablePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing application: {ex.Message}");
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var app = ((FrameworkElement)sender).DataContext as AppInfo;
            if (app != null)
            {
                ExecuteApplication(app.ExecutablePath);
            }
        }
    }

    public class AppInfo
    {
        public string Name { get; set; }
        public ImageSource IconPath { get; set; } // Changed from IconPath string to ImageSource
        public string ExecutablePath { get; set; }
    }
}
