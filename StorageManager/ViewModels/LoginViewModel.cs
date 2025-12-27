using StorageManager.Helpers;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace StorageManager.ViewModels
{
    /// <summary>
    /// Логика взаимодействия для LoginViewModel.cs
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Свойства/Команды
        /// </summary>
        private readonly string _connectionString = @"Server=GOSHA\SQLEXPRESS;Database=StorageManagement;Trusted_Connection=True;TrustServerCertificate=True;";

        private string _username;
        private string _password;
        private string _errorMessage;
        private bool _hasError;
        private bool _isSuccess;
        private bool _isLoading;
        private bool _isMessageVisible;
        public event EventHandler RequestClose;
        public event EventHandler LoginSuccessful;

        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }
        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }
        public bool HasError
        {
            get => _hasError;
            set => SetField(ref _hasError, value);
        }
        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetField(ref _isSuccess, value);
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }
        public bool IsMessageVisible
        {
            get => _isMessageVisible;
            set => SetField(ref _isMessageVisible, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand CloseCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(async _ => await LoginAsync());
            RegisterCommand = new RelayCommand(async _ => await RegisterAsync());
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        /// <summary>
        /// Методы авторизации и регистрации
        /// </summary>
        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Username))
            {
                ShowMessage("Введите имя пользователя", false);
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                ShowMessage("Введите пароль", false);
                return;
            }

            IsLoading = true;
            IsMessageVisible = false;

            try
            {
                bool isValid = await ValidateCredentialsAsync(Username, Password);

                if (isValid)
                {
                    ShowMessage("Вход выполнен успешно!", true);
                    await Task.Delay(500);
                    LoginSuccessful?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ShowMessage("Неверное имя пользователя или пароль", false);
                }
            }
            catch (SqlException ex)
            {
                ShowMessage($"Ошибка подключения к базе данных: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Произошла ошибка: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task RegisterAsync()
        {
            if (string.IsNullOrEmpty(Username))
            {
                ShowMessage("Введите имя пользователя", false);
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                ShowMessage("Введите пароль", false);
                return;
            }

            if (Password.Length < 4)
            {
                ShowMessage("Пароль должен содержать не менее 4 символов", false);
                return;
            }

            IsLoading = true;
            IsMessageVisible = false;

            try
            {
                bool success = await RegisterUserAsync(Username, Password);

                if (success)
                {
                    ShowMessage("Регистрация успешна! Теперь вы можете войти.", true);
                    Password = string.Empty;
                }
                else
                {
                    ShowMessage("Пользователь с таким именем уже существует", false);
                }
            }
            catch (SqlException ex)
            {
                ShowMessage($"Ошибка подключения к базе данных: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                ShowMessage($"Произошла ошибка: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task<bool> RegisterUserAsync(string username, string password)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

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
                            return false;
                        }
                    }

                    var personQuery = @"
                        INSERT INTO Person (PersonName, PersonMiddleName, PersonLastName, PersonPhone, PersonEmail, PersonContactInformationID)
                        VALUES (@Username, '', '', '', @Email, NULL);
                        SELECT SCOPE_IDENTITY();";

                    using (var personCmd = new SqlCommand(personQuery, conn))
                    {
                        personCmd.Parameters.AddWithValue("@Username", username);
                        personCmd.Parameters.AddWithValue("@Email", $"{username}@sklad.local");
                        int personId = Convert.ToInt32(await personCmd.ExecuteScalarAsync());

                        string salt = PasswordHasher.GenerateSalt();
                        string passwordHash = PasswordHasher.HashPassword(password, salt);

                        var userQuery = @"
                            INSERT INTO Users (UsersName, UsersPersonID, UsersLogin, PasswordSalt, PasswordHash)
                            VALUES (@FullName, @PersonID, @Username, @Salt, @Hash)";

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
                await Task.Delay(500);
                return true;
            }
        }

        /// <summary>
        /// Служебные методы
        /// </summary>
        private void ShowMessage(string message, bool isSuccess)
        {
            ErrorMessage = message;
            IsSuccess = isSuccess;
            HasError = !isSuccess;
            IsMessageVisible = true;
        }
        private async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

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
                            return false;
                        }
                    }
                }
            }
            catch
            {
                await Task.Delay(500);
                return false;
            }
        }
    }
}