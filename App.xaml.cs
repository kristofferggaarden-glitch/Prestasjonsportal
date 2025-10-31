using System.Windows;

namespace ABBsPrestasjonsportal
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Vis login-vindu først
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                // Hvis login vellykket, åpne hovedvindu
                var mainWindow = new MainWindow(loginWindow.IsAdmin, loginWindow.CurrentUser);
                mainWindow.Show();
            }
            else
            {
                // Hvis login mislyktes, avslutt
                Shutdown();
            }
        }
    }
}