using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StorageManager.Models;
using StorageManager.Services;
using StorageManager.Views;

namespace StorageManager.ViewModels
{
    /// <summary>
    /// Логика взаимодействия для MainViewModel.cs
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private bool _isDashboard = true;
        private User _currentUser;
        private ObservableCollection<Product> _products;
       
        public bool IsDashboard
        {
            get => _isDashboard;
            set => SetField(ref _isDashboard, value);
        }
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetField(ref _products, value);
        }
        public User CurrentUser
        {
            get => _currentUser;
            set => SetField(ref _currentUser, value);
        }

        public ICommand LoadProductsCommand { get; }
        public ICommand AddProductCommand { get; }

        public MainViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);
            Products = new ObservableCollection<Product>();

            LoadProductsCommand = new RelayCommand(async _ => await LoadProductsAsync());
            AddProductCommand = new RelayCommand(_ => ShowAddProductDialog());

            CurrentUser = new User
            {
                UserId = 1,
                UserName = "Администратор",
                Login = "admin"
            };

            LoadProductsCommand.Execute(null);
        }

        /// <summary>
        /// Служебные методы
        /// </summary>
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
    }
}