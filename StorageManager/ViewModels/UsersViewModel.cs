using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    /// <summary>
    /// Логика взаимодействия для UsersViewModel.cs
    /// </summary>
    public class UsersViewModel : BaseViewModel
    {
        /// <summary>
        /// Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<User> _users;
        private ObservableCollection<User> _filteredUsers;

        private User _currentUser;
        private User _selectedUser;

        private string _password;
        private string _confirmPassword;

        private string _searchText;
        private string _errorMessage;

        private bool _isEditing;
        private bool _hasErrors;
        private bool _isChangingPassword;

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetField(ref _users, value);
        }
        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set => SetField(ref _filteredUsers, value);
        }
        public User CurrentUser
        {
            get => _currentUser;
            set => SetField(ref _currentUser, value);
        }
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetField(ref _selectedUser, value) && value != null)
                {
                    EditUser(value);
                }
            }
        }
        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetField(ref _confirmPassword, value);
        }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }
        public bool IsEditing
        {
            get => _isEditing;
            set => SetField(ref _isEditing, value);
        }
        public bool IsChangingPassword
        {
            get => _isChangingPassword;
            set => SetField(ref _isChangingPassword, value);
        }
        public bool HasErrors
        {
            get => _hasErrors;
            set => SetField(ref _hasErrors, value);
        }

        public string FormTitle => IsEditing ? "Редактирование пользователя" : "Новый пользователь";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать пользователя";
        public bool ShowPasswordFields => !IsEditing || IsChangingPassword;

        public ICommand LoadUsersCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ChangePasswordCommand { get; }

        public UsersViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Users = new ObservableCollection<User>();
            FilteredUsers = new ObservableCollection<User>();

            LoadUsersCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddUserCommand = new RelayCommand(_ => AddNewUser());
            EditUserCommand = new RelayCommand(EditUser);
            DeleteUserCommand = new RelayCommand(async p => await DeleteUserAsync(p as User));
            SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());
            ChangePasswordCommand = new RelayCommand(_ => IsChangingPassword = true);

            CurrentUser = new User();

            LoadUsersCommand.Execute(null);
        }
        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewUser()
        {
            CurrentUser = new User();
            Password = "";
            ConfirmPassword = "";
            IsChangingPassword = true;
            IsEditing = true;
            HasErrors = false;
        }
        private void EditUser(object parameter)
        {
            if (parameter is User user)
            {
                CurrentUser = new User
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Login = user.Login,
                    PersonId = user.PersonId,
                    PersonName = user.PersonName
                };

                Password = "";
                ConfirmPassword = "";
                IsChangingPassword = false;
                IsEditing = true;
                HasErrors = false;
            }
        }
        private async Task DeleteUserAsync(User user)
        {
            if (user == null) return;

            // Нельзя удалить текущего пользователя (можно добавить проверку)

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя '{user.UserName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _dbService.DeleteUserAsync(user.UserId);

                    if (success)
                    {
                        MessageBox.Show("Пользователь успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = "Ошибка удаления пользователя";
                        HasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
        private async Task SaveUserAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentUser.UserName))
            {
                ErrorMessage = "Имя пользователя обязательно для заполнения";
                HasErrors = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentUser.Login))
            {
                ErrorMessage = "Логин обязателен для заполнения";
                HasErrors = true;
                return;
            }

            if (ShowPasswordFields)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Пароль обязателен для заполнения";
                    HasErrors = true;
                    return;
                }

                if (Password.Length < 4)
                {
                    ErrorMessage = "Пароль должен содержать не менее 4 символов";
                    HasErrors = true;
                    return;
                }

                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Пароли не совпадают";
                    HasErrors = true;
                    return;
                }
            }

            try
            {
                if (IsEditing && CurrentUser.UserId > 0)
                {
                    string passwordToUpdate = IsChangingPassword ? Password : null;
                    bool success = await _dbService.UpdateUserAsync(CurrentUser, passwordToUpdate);

                    if (success)
                    {
                        MessageBox.Show("Пользователь успешно обновлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка обновления пользователя";
                        HasErrors = true;
                        return;
                    }
                }
                else
                {
                    int newId = await _dbService.AddUserAsync(CurrentUser, Password);

                    if (newId > 0)
                    {
                        MessageBox.Show($"Пользователь успешно создан. ID: {newId}", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка создания пользователя";
                        HasErrors = true;
                        return;
                    }
                }

                await LoadDataAsync();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                HasErrors = true;
            }
        }
        private void CancelEditing()
        {
            CurrentUser = new User();
            Password = "";
            ConfirmPassword = "";
            IsChangingPassword = false;
            IsEditing = false;
            HasErrors = false;
        }
        /// <summary>
        /// Служебные методы
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                var users = await _dbService.GetUsersAsync();
                Users.Clear();

                foreach (var user in users)
                {
                    Users.Add(user);
                }

                ApplyFilters();

                CancelEditing();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
                HasErrors = true;
            }
        }
        private void ApplyFilters()
        {
            if (Users == null || !Users.Any())
            {
                FilteredUsers.Clear();
                return;
            }

            IEnumerable<User> filtered = Users;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                    (u.Login != null && u.Login.ToLower().Contains(searchLower)) ||
                    (u.PersonName != null && u.PersonName.ToLower().Contains(searchLower)));
            }

            FilteredUsers.Clear();
            foreach (var user in filtered.ToList())
            {
                FilteredUsers.Add(user);
            }
        }
    }
}