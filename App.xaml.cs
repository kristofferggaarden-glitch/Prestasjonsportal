using System.Windows;

namespace ABBsPrestasjonsportal
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Vis login-vinduet
            LoginWindow loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                // Bruker logget inn, vis hovedvinduet
                MainWindow mainWindow = new MainWindow(loginWindow.IsAdmin, loginWindow.CurrentUser);
                mainWindow.Show();
            }
            else
            {
                // Bruker avbrøt, lukk applikasjonen
                Application.Current.Shutdown();
            }
        }
    }
}
