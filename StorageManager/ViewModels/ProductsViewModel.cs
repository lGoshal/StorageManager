using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    public class ProductsViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;

        // Коллекции
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        private List<ProductType> _productTypes;
        private List<UnitOfMeasurement> _unitsOfMeasurement;
        private List<Characteristic> _characteristics;
        private List<ExpirationDateUnit> _expirationDateUnits;

        // Текущие объекты
        private Product _currentProduct;
        private Product _selectedProduct;
        private ProductType _selectedProductTypeFilter;

        // Поиск и фильтры
        private string _searchText;
        private string _errorMessage;

        // Флаги состояния
        private bool _isEditing;
        private bool _hasErrors;

        // Свойства
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetField(ref _products, value);
        }

        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetField(ref _filteredProducts, value);
        }

        public List<ProductType> ProductTypes
        {
            get => _productTypes;
            set => SetField(ref _productTypes, value);
        }

        public List<Characteristic> Characteristics
        {
            get => _characteristics;
            set => SetField(ref _characteristics, value);
        }

        public Product CurrentProduct
        {
            get => _currentProduct;
            set => SetField(ref _currentProduct, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetField(ref _selectedProduct, value) && value != null)
                {
                    EditProduct(value);
                }
            }
        }

        public ProductType SelectedProductTypeFilter
        {
            get => _selectedProductTypeFilter;
            set
            {
                if (SetField(ref _selectedProductTypeFilter, value))
                {
                    ApplyFilters();
                    OnPropertyChanged(nameof(HasActiveFilters)); // Уведомляем об изменении
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                {
                    ApplyFilters();
                    OnPropertyChanged(nameof(HasActiveFilters)); // Уведомляем об изменении
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

        public bool HasErrors
        {
            get => _hasErrors;
            set => SetField(ref _hasErrors, value);
        }

        // Вычисляемые свойства
        public string FormTitle => IsEditing ? "Редактирование товара" : "Новый товар";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать товар";
        public bool HasActiveFilters => SelectedProductTypeFilter != null || !string.IsNullOrWhiteSpace(SearchText);

        // Команды
        public ICommand LoadProductsCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand ResetTypeFilterCommand { get; }

        // Конструктор
        public ProductsViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Products = new ObservableCollection<Product>();
            FilteredProducts = new ObservableCollection<Product>();
            ProductTypes = new List<ProductType>();
            Characteristics = new List<Characteristic>();

            // Инициализация команд
            LoadProductsCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddProductCommand = new RelayCommand(_ => AddNewProduct());
            EditProductCommand = new RelayCommand(EditProduct);
            DeleteProductCommand = new RelayCommand(async p => await DeleteProductAsync(p as Product));
            SaveProductCommand = new RelayCommand(async _ => await SaveProductAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());
            ResetFilterCommand = new RelayCommand(_ => ResetFilters());
            ResetTypeFilterCommand = new RelayCommand(_ => ResetTypeFilter());

            // Создаем новый продукт по умолчанию
            CurrentProduct = new Product();

            // Загружаем данные
            LoadProductsCommand.Execute(null);
        }
        private void ResetFilters()
        {
            SelectedProductTypeFilter = null;
            SearchText = string.Empty;
        }

        private void ResetTypeFilter()
        {
            SelectedProductTypeFilter = null;
        }

        // Метод загрузки данных
        private async Task LoadDataAsync()
        {
            try
            {
                // Загружаем товары
                var products = await _dbService.GetProductsAsync();
                Products.Clear();

                foreach (var product in products)
                {
                    Products.Add(product);
                }

                // Загружаем типы товаров
                ProductTypes = await _dbService.GetProductTypesAsync();
                OnPropertyChanged(nameof(ProductTypes));

                // Применяем фильтры
                ApplyFilters();

                // Сбрасываем форму
                CancelEditing();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
                HasErrors = true;
            }
        }

        // Добавление нового товара
        private void AddNewProduct()
        {
            CurrentProduct = new Product();
            IsEditing = true;
            HasErrors = false;
        }

        // Редактирование товара
        private void EditProduct(object parameter)
        {
            if (parameter is Product product)
            {
                CurrentProduct = new Product
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ProductDescription = product.ProductDescription,
                    ProductQuantity = product.ProductQuantity,
                    ProductTypeId = product.ProductTypeId,
                    ProductTypeName = product.ProductTypeName
                };

                IsEditing = true;
                HasErrors = false;
            }
        }

        // Удаление товара
        private async Task DeleteProductAsync(Product product)
        {
            if (product == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить товар '{product.ProductName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.DeleteProductAsync(product.ProductId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }

        // Сохранение товара
        private async Task SaveProductAsync()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(CurrentProduct.ProductName))
            {
                ErrorMessage = "Название товара обязательно для заполнения";
                HasErrors = true;
                return;
            }

            if (CurrentProduct.ProductQuantity < 0)
            {
                ErrorMessage = "Количество не может быть отрицательным";
                HasErrors = true;
                return;
            }

            try
            {
                if (IsEditing && CurrentProduct.ProductId > 0)
                {
                    // Обновление существующего товара
                    await _dbService.UpdateProductAsync(CurrentProduct);
                }
                else
                {
                    // Добавление нового товара
                    await _dbService.AddProductAsync(CurrentProduct);
                }

                // Обновляем список
                await LoadDataAsync();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                HasErrors = true;
            }
        }

        // Отмена редактирования
        private void CancelEditing()
        {
            CurrentProduct = new Product();
            IsEditing = false;
            HasErrors = false;
        }

        // Применение фильтров
        private void ApplyFilters()
        {
            if (Products == null || !Products.Any())
            {
                FilteredProducts.Clear();
                return;
            }

            IEnumerable<Product> filtered = Products;

            // Фильтр по типу товара
            if (SelectedProductTypeFilter != null)
            {
                filtered = filtered.Where(p => p.ProductTypeId == SelectedProductTypeFilter.ProductTypeId);
            }

            // Фильтр по тексту поиска
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(searchLower)) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(searchLower)));
            }

            // Обновляем отфильтрованную коллекцию
            FilteredProducts.Clear();
            foreach (var product in filtered.ToList())
            {
                FilteredProducts.Add(product);
            }
        }
    }
}