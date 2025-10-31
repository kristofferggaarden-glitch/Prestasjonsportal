using System.Windows;
using System.Windows.Input;

namespace ABBsPrestasjonsportal
{
    public partial class LoginWindow : Window
    {
        public bool IsAdmin { get; private set; }
        public string CurrentUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            UsernameBox.Focus();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            // Kun admin trenger passord
            if (username == "admin" && password == "admin123")
            {
                IsAdmin = true;
                CurrentUser = "Administrator";
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = "Feil brukernavn eller passord!\nBruk 'admin' / 'admin123' eller logg inn som gjest.";
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            // Gjest-innlogging krever ikke passord
            IsAdmin = false;
            CurrentUser = "Gjest";
            DialogResult = true;
            Close();
        }
    }
}
