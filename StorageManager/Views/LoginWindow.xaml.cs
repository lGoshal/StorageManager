using StorageManager.ViewModels;
using System.Windows;
using StorageManager.Views;

namespace StorageManager.Views
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            var viewModel = new LoginViewModel();
            viewModel.LoginSuccessful += ViewModel_LoginSuccessful;
            viewModel.RequestClose += ViewModel_RequestClose;
            this.DataContext = viewModel;

            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }
        /// <summary>
        /// Обработчики авторизации
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }
        private void ViewModel_LoginSuccessful(object sender, EventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void ViewModel_RequestClose(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}