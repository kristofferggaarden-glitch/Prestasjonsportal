using System;
using System.Windows;

namespace ABBsPrestasjonsportal
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Vis login-vinduet
            LoginWindow loginWindow = new LoginWindow();
            bool? dialogResult = loginWindow.ShowDialog();

            if (dialogResult == true)
            {
                // Bruker logget inn, vis hovedvinduet
                MainWindow mainWindow = new MainWindow(loginWindow.IsAdmin, loginWindow.CurrentUser);

                // Sett MainWindow som hovedvindu
                this.MainWindow = mainWindow;

                // Endre shutdown mode slik at applikasjonen lukkes når MainWindow lukkes
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // Vis vinduet
                mainWindow.Show();
            }
            else
            {
                // Bruker avbrøt - lukk applikasjonen
                this.Shutdown();
            }
        }
    }
}