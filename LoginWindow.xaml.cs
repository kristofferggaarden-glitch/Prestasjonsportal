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

            // Hardkodede brukere (kan utvides til database senere)
            if (username == "admin" && password == "admin123")
            {
                IsAdmin = true;
                CurrentUser = "Administrator";
                DialogResult = true;
                Close();
            }
            else if (username == "bruker" && password == "passord123")
            {
                IsAdmin = false;
                CurrentUser = username;
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorText.Text = "Feil brukernavn eller passord!";
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }
    }
}