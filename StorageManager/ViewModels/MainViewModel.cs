using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private User _currentUser;
        private ObservableCollection<Product> _products;

        // Свойства
        public User CurrentUser
        {
            get => _currentUser;
            set => SetField(ref _currentUser, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetField(ref _products, value);
        }

        // Команды
        public ICommand LoadProductsCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand ShowAboutCommand { get; }

        // Конструктор
        public MainViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);
            Products = new ObservableCollection<Product>();

            // Инициализация команд
            LoadProductsCommand = new RelayCommand(async _ => await LoadProductsAsync());
            AddProductCommand = new RelayCommand(_ => ShowAddProductDialog());
            ShowAboutCommand = new RelayCommand(_ => ShowAboutDialog());

            // Создаем тестового пользователя
            CurrentUser = new User
            {
                UserId = 1,
                UserName = "Администратор",
                Login = "admin"
            };

            // Загружаем данные
            LoadProductsCommand.Execute(null);
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var products = await _dbService.GetProductsAsync();
                Products.Clear();

                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAddProductDialog()
        {
            MessageBox.Show("Форма добавления товара будет реализована позже", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowAboutDialog()
        {
            // Это будет вызываться из View
            // Реализация в MainWindow.xaml.cs
        }

        private bool _isDashboard = true;
        public bool IsDashboard
        {
            get => _isDashboard;
            set => SetField(ref _isDashboard, value);
        }

        // И метод для обновления состояния
        private void UpdatePageState(string pageName)
        {
            IsDashboard = (pageName == "Dashboard");
            OnPropertyChanged(nameof(IsDashboard));
        }
    }
}