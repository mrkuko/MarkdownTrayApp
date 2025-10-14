using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace MarkdownTrayApp
{
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                //Icon = System.Drawing.SystemIcons.Application,
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                Visible = true,
                Text = Properties.Resources.ApplicationTitle
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Scan Files", null, (s, e) => ScanFiles());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            notifyIcon.ContextMenuStrip = contextMenu;
            //notifyIcon.DoubleClick += (s, e) => ShowWindow();
            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowWindow();
                }
            };
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            notifyIcon.ShowBalloonTip(2000, "Markdown Viewer",
                "Application minimized to tray", ToolTipIcon.Info);
        }

        private void ExitApplication()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void Theme_Changed(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (rbDark?.IsChecked == true)
            {
                Resources["BackgroundBrush"] = Resources["DarkBackgroundBrush"];
                Resources["ForegroundBrush"] = Resources["DarkForegroundBrush"];
                Style = (Style)Resources["DarkTheme"];
                //Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                //Foreground = Brushes.White;
            }
            else
            {
                Resources["BackgroundBrush"] = Resources["LightBackgroundBrush"];
                Resources["ForegroundBrush"] = Resources["LightForegroundBrush"];
                Style = (Style)Resources["LightTheme"];
                //Background = Brushes.White;
                //Foreground = Brushes.Black;
            }
        }

        private void BrowseDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = txtDirectory.Text;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtDirectory.Text = dialog.SelectedPath;
                }
            }
        }

        private void ScanFiles_Click(object sender, RoutedEventArgs e)
        {
            ScanFiles();
        }

        private void ScanFiles()
        {
            tvResults.Items.Clear();

            if (string.IsNullOrWhiteSpace(txtDirectory.Text) ||
                !Directory.Exists(txtDirectory.Text))
            {
                MessageBox.Show("Please select a valid directory.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var filenames = txtFilenames.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();

            if (!filenames.Any())
            {
                MessageBox.Show("Please enter at least one filename to search.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var filename in filenames)
                {
                    SearchAndProcessFiles(txtDirectory.Text, filename);
                }

                if (tvResults.Items.Count == 0)
                {
                    var node = new TreeNode { DisplayText = "No matching files found." };
                    tvResults.Items.Add(node);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning files: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchAndProcessFiles(string directory, string filename)
        {
            Console.WriteLine($"Searching for all \"{filename}\" files inside \"{directory}\"");
            try
            {
                var files = Directory.GetFiles(directory, filename,
                    SearchOption.AllDirectories);
                Console.WriteLine($"Found: {files}");
                foreach (var file in files)
                {
                    ProcessMarkdownFile(file);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we don't have access to
            }
        }

        private void ProcessMarkdownFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();

                var fileNode = new TreeNode
                {
                    DisplayText = $"📄 {Path.GetFileName(filePath)} ({filePath})"
                };

                var rootChildren = ParseIndentedLines(lines, 0, 0);
                foreach (var child in rootChildren)
                {
                    fileNode.Children.Add(child);
                }

                if (fileNode.Children.Any())
                    tvResults.Items.Add(fileNode);
            }
            catch (Exception ex)
            {
                var errorNode = new TreeNode
                {
                    DisplayText = $"❌ Error reading {filePath}: {ex.Message}"
                };
                tvResults.Items.Add(errorNode);
            }
        }

        /// <summary>
        /// Recursively parses lines based on indentation and bullet/heading markers.
        /// </summary>
        private List<TreeNode> ParseIndentedLines(List<string> lines, int startIndex, int currentIndent)
        {
            var nodes = new List<TreeNode>();
            int i = startIndex;

            while (i < lines.Count)
            {
                var line = lines[i];
                int indent = line.TakeWhile(Char.IsWhiteSpace).Count();
                string trimmed = line.Trim();

                // Determine type
                bool isHeader = trimmed.StartsWith("#");
                bool isBullet = trimmed.StartsWith("-");

                // If indent is less than current level, we’re returning to upper level
                if (indent < currentIndent)
                    break;

                // If deeper indent than current, it's a child — stop here and let recursion handle it
                if (indent > currentIndent)
                {
                    var lastNode = nodes.LastOrDefault();
                    if (lastNode != null)
                    {
                        var subChildren = ParseIndentedLines(lines, i, indent);
                        foreach (var child in subChildren)
                        {
                            lastNode.Children.Add(child);
                        }
                        i += subChildren.Count;
                        continue;
                    }
                }

                // If it’s a top-level or same-level line
                if (isHeader)
                {
                    var text = trimmed.TrimStart('#').Trim();
                    nodes.Add(new TreeNode { DisplayText = text });
                }
                else if (isBullet)
                {
                    var text = trimmed.Substring(1).Trim();
                    nodes.Add(new TreeNode { DisplayText = $"• {text}" });
                }
                else if (indent == 0)
                {
                    // Treat any non-bullet, non-header at indent 0 as a heading
                    nodes.Add(new TreeNode { DisplayText = trimmed });
                }

                i++;
            }

            return nodes;
        }


        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Settings saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new
                {
                    Theme = rbDark.IsChecked == true ? "Dark" : "Light",
                    Directory = txtDirectory.Text,
                    Filenames = txtFilenames.Text
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

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(Properties.Resources.SettingsFile))
                {
                    var json = File.ReadAllText(Properties.Resources.SettingsFile);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);

                    if (settings != null)
                    {
                        rbDark.IsChecked = settings.Theme == "Dark";
                        rbLight.IsChecked = settings.Theme != "Dark";
                        txtDirectory.Text = settings.Directory ?? "";
                        txtFilenames.Text = settings.Filenames ?? "README.md\nNOTES.md\nTODO.md";
                        ApplyTheme();
                    }
                }
            }
            catch
            {
                // Use defaults if loading fails
            }
        }

        private void HideToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private class Settings
        {
            public string Theme { get; set; }
            public string Directory { get; set; }
            public string Filenames { get; set; }
        }
    }

    public class TreeNode
    {
        public string DisplayText { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } =
            new ObservableCollection<TreeNode>();
    }
}