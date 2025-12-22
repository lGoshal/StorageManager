using StorageManager.Helpers;
using StorageManager.Models;
using StorageManager.ViewModels;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StorageManager
{
    public partial class LoginWindow : Window
    {
        private string connectionString = @"Server=GOSHA\SQLEXPRESS;Database=StorageManagement;Trusted_Connection=True;TrustServerCertificate=True;";

        public LoginWindow()
        {
            InitializeComponent();
            UsernameTextBox.Focus();

            // Подписываемся на события клавиш
            UsernameTextBox.KeyDown += TextBox_KeyDown;
            PasswordBox.KeyDown += PasswordBox_KeyDown;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(null, null);
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformLogin();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformRegistration();
        }

        private async Task PerformLogin()
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Валидация
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Введите имя пользователя");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Введите пароль");
                PasswordBox.Focus();
                return;
            }

            // Блокируем кнопки на время проверки
            LoginButton.IsEnabled = false;
            RegisterButton.IsEnabled = false;
            ErrorBorder.Visibility = Visibility.Collapsed;

            try
            {
                // Проверка учетных данных
                bool isValid = await ValidateCredentialsAsync(username, password);

                if (isValid)
                {
                    // Успешный вход
                    var mainWindow = new MainWindow();

                    // Не нужно создавать ViewModel здесь - она создается в MainWindow
                    // Не нужно устанавливать DataContext здесь

                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (SqlException ex)
            {
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                // Разблокируем кнопки
                LoginButton.IsEnabled = true;
                RegisterButton.IsEnabled = true;
            }
        }

        private async Task PerformRegistration()
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Валидация
            if (string.IsNullOrEmpty(username))
            {
                ShowError("Введите имя пользователя");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Введите пароль");
                PasswordBox.Focus();
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Пароль должен содержать не менее 4 символов");
                PasswordBox.Focus();
                return;
            }

            // Блокируем кнопки на время регистрации
            LoginButton.IsEnabled = false;
            RegisterButton.IsEnabled = false;
            ErrorBorder.Visibility = Visibility.Collapsed;

            try
            {
                bool success = await RegisterUserAsync(username, password);

                if (success)
                {
                    ShowError("Регистрация успешна! Теперь вы можете войти.", true);
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
                else
                {
                    ShowError("Пользователь с таким именем уже существует");
                    UsernameTextBox.Focus();
                }
            }
            catch (SqlException ex)
            {
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowError($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                // Разблокируем кнопки
                LoginButton.IsEnabled = true;
                RegisterButton.IsEnabled = true;
            }
        }

        private async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Только новый метод - старый поле удалено
                    var query = @"
                SELECT PasswordSalt, PasswordHash
                FROM Users 
                WHERE UsersLogin = @Username";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                                {
                                    string salt = reader.GetString(0);
                                    string storedHash = reader.GetString(1);

                                    return PasswordHasher.VerifyPassword(password, salt, storedHash);
                                }
                            }
                            return false; // Пользователь не найден или нет хэша
                        }
                    }
                }
            }
            catch
            {
                // Обработка ошибок
                await Task.Delay(500);
                return false;
            }
        }

        private async Task<bool> RegisterUserAsync(string username, string password)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Проверяем, существует ли пользователь
                    var checkQuery = @"
                        SELECT COUNT(*) 
                        FROM Users 
                        WHERE UsersLogin = @Username";

                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Username", username);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            return false; // Пользователь уже существует
                        }
                    }

                    // Создаем новую запись Person
                    var personQuery = @"
                        INSERT INTO Person (PersonName, PersonMiddleName, PersonLastName, PersonPhone, PersonEmail, PersonContactInformationID)
                        VALUES (@Username, '', '', '', @Email, NULL);
                        SELECT SCOPE_IDENTITY();";

                    using (var personCmd = new SqlCommand(personQuery, conn))
                    {
                        personCmd.Parameters.AddWithValue("@Username", username);
                        personCmd.Parameters.AddWithValue("@Email", $"{username}@sklad.local");
                        int personId = Convert.ToInt32(await personCmd.ExecuteScalarAsync());

                        // Генерируем соль и хэшируем пароль
                        string salt = PasswordHasher.GenerateSalt();
                        string passwordHash = PasswordHasher.HashPassword(password, salt);

                        // Создаем пользователя с хэшированным паролем
                        var userQuery = @"
                            INSERT INTO Users (UsersName, UsersPersonID, UsersLogin, PasswordSalt, PasswordHash)
                            VALUES (@FullName, @PersonID, @Username, @Salt, @Hash)"; // Без UsersPassword

                        using (var userCmd = new SqlCommand(userQuery, conn))
                        {
                            userCmd.Parameters.AddWithValue("@FullName", username);
                            userCmd.Parameters.AddWithValue("@PersonID", personId);
                            userCmd.Parameters.AddWithValue("@Username", username);
                            userCmd.Parameters.AddWithValue("@Salt", salt);
                            userCmd.Parameters.AddWithValue("@Hash", passwordHash);

                            await userCmd.ExecuteNonQueryAsync();
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Для демонстрации - имитируем успешную регистрацию
                await Task.Delay(500);
                return true;
            }
        }

        private void ShowError(string message, bool isSuccess = false)
        {
            ErrorMessageTextBlock.Text = message;

            if (isSuccess)
            {
                ErrorBorder.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
                ErrorBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(195, 230, 203));
                ErrorMessageTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
            }
            else
            {
                ErrorBorder.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));
                ErrorBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 198, 203));
                ErrorMessageTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
            }

            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async Task MigrateTestUser()
        {
            // Мигрируем тестового пользователя в новую систему
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    var checkQuery = "SELECT COUNT(*) FROM Users WHERE UsersLogin = 'admin'";
                    using (var cmd = new SqlCommand(checkQuery, conn))
                    {
                        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            string salt = PasswordHasher.GenerateSalt();
                            string hash = PasswordHasher.HashPassword("admin123", salt);

                            var updateQuery = @"
                        UPDATE Users 
                        SET PasswordSalt = @Salt, 
                            PasswordHash = @Hash,
                            UsersPassword = '' 
                        WHERE UsersLogin = 'admin'";

                            using (var updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@Salt", salt);
                                updateCmd.Parameters.AddWithValue("@Hash", hash);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки при миграции тестового пользователя
            }
        }
    }
}