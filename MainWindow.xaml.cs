using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
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
        static string ExtractIcon(string path)
        {
            // Original path to the GoogleDriveFS.exe file
            string exeFilePathWithIconIndex = @$"{path}";

            // Extract the executable path from the string
            string exeFilePath = path.GetDirectoryName(exeFilePathWithIconIndex);

            // Extract the icon from the .exe file
            Icon icon = Icon.ExtractAssociatedIcon(exeFilePath);

            if (icon != null)
            {
                // Convert the icon to a Bitmap
                Bitmap bitmap = icon.ToBitmap();

                // Save the Bitmap to a file in the current directory with .ico extension
                string iconFilePath = path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoogleDriveFSIcon.ico");
                bitmap.Save(iconFilePath, System.Drawing.Imaging.ImageFormat.Icon);

                return iconFilePath;
            }
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

        private void FindInstalledApps(RegistryKey? key, List<AppInfo> installedApps)
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
                            string iconPath = ExtractIconPath(subKey);
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

        private string ExtractIconPath(RegistryKey key)
        {
            var displayIcon = key.GetValue("DisplayIcon") as string;

            if (!string.IsNullOrEmpty(displayIcon))
            {
                if (displayIcon.Contains(",0"))
                {
                    int i = displayIcon.Length - 2;
                    displayIcon = ExtractIcon(displayIcon.Substring(0, i));
                    MessageBox.Show(displayIcon + " hi akash");
                    return displayIcon;
                }

                return displayIcon;
            }
            return string.Empty;
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
        public string IconPath { get; set; }
        public string ExecutablePath { get; set; }
    }
}