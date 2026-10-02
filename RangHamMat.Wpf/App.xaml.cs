using RangHamMat;
using RangHamMat.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace wpftest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            var login = new DangNhap();

            if (login.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
        
    }

}
