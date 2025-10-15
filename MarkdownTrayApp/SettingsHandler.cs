using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MarkdownTrayApp
{
    public class Settings
    {
        public string Theme { get; set; }
        public string Directory { get; set; }
        public string Filenames { get; set; }

        public static void SaveSettings(MainWindow mainWindow)
        {
            try
            {
                var settings = new
                {
                    Theme = mainWindow.rbDark.IsChecked == true ? "Dark" : "Light",
                    Directory = mainWindow.txtDirectory.Text,
                    Filenames = mainWindow.txtFilenames.Text
                };

                var json = System.Text.Json.JsonSerializer.Serialize(settings,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Properties.Resources.SettingsFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public delegate void SettingsLoadedDelegate(string theme, string directory, string filenames);

        public static void LoadSettings(SettingsLoadedDelegate? consumer = null)
        {
            try
            {
                if (File.Exists(Properties.Resources.SettingsFile))
                {
                    var json = File.ReadAllText(Properties.Resources.SettingsFile);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);

                    if (settings != null)
                    {
                        consumer?.Invoke(
                            theme: settings.Theme,
                            directory: settings.Directory, 
                            filenames: settings.Filenames);
                    }
                }
            }
            catch
            {
                // Use defaults if loading fails
            }
        }
    }
}