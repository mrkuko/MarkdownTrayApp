using System.Windows;

namespace MarkdownTrayApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start minimized to tray if desired
            // Uncomment to hide window on startup
            // MainWindow.Hide();
        }
    }
}