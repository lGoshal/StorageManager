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
    /// <summary>
    /// Логика взаимодействия для ProductsViewModel.cs
    /// </summary>
    public class ProductsViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Листы/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        private List<ProductType> _productTypes;
        private List<UnitOfMeasurement> _unitsOfMeasurement;
        private List<Characteristic> _characteristics;
        private List<ExpirationDateUnit> _expirationDateUnits;

        private Product _currentProduct;
        private Product _selectedProduct;
        private ProductType _selectedProductTypeFilter;

        private string _searchText;
        private string _errorMessage;

        private bool _isEditing;
        private bool _hasErrors;

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
                    OnPropertyChanged(nameof(HasActiveFilters));
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
                    OnPropertyChanged(nameof(HasActiveFilters));
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

        public string FormTitle => IsEditing ? "Редактирование товара" : "Новый товар";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать товар";
        public bool HasActiveFilters => SelectedProductTypeFilter != null || !string.IsNullOrWhiteSpace(SearchText);

        public ICommand LoadProductsCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand ResetTypeFilterCommand { get; }

        public ProductsViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Products = new ObservableCollection<Product>();
            FilteredProducts = new ObservableCollection<Product>();
            ProductTypes = new List<ProductType>();
            Characteristics = new List<Characteristic>();

            LoadProductsCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddProductCommand = new RelayCommand(_ => AddNewProduct());
            EditProductCommand = new RelayCommand(EditProduct);
            DeleteProductCommand = new RelayCommand(async p => await DeleteProductAsync(p as Product));
            SaveProductCommand = new RelayCommand(async _ => await SaveProductAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());
            ResetFilterCommand = new RelayCommand(_ => ResetFilters());
            ResetTypeFilterCommand = new RelayCommand(_ => ResetTypeFilter());

            CurrentProduct = new Product();

            LoadProductsCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewProduct()
        {
            CurrentProduct = new Product();
            IsEditing = true;
            HasErrors = false;
        }
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
        private async Task SaveProductAsync()
        {
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
                    await _dbService.UpdateProductAsync(CurrentProduct);
                }
                else
                {
                    await _dbService.AddProductAsync(CurrentProduct);
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
            CurrentProduct = new Product();
            IsEditing = false;
            HasErrors = false;
        }

        /// <summary>
        /// Служебные методы
        /// </summary>
        private void ResetFilters()
        {
            SelectedProductTypeFilter = null;
            SearchText = string.Empty;
        }
        private void ResetTypeFilter()
        {
            SelectedProductTypeFilter = null;
        }
        private async Task LoadDataAsync()
        {
            try
            {
                var products = await _dbService.GetProductsAsync();
                Products.Clear();

                foreach (var product in products)
                {
                    Products.Add(product);
                }

                ProductTypes = await _dbService.GetProductTypesAsync();
                OnPropertyChanged(nameof(ProductTypes));

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
            if (Products == null || !Products.Any())
            {
                FilteredProducts.Clear();
                return;
            }

            IEnumerable<Product> filtered = Products;

            if (SelectedProductTypeFilter != null)
            {
                filtered = filtered.Where(p => p.ProductTypeId == SelectedProductTypeFilter.ProductTypeId);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.ProductName != null && p.ProductName.ToLower().Contains(searchLower)) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(searchLower)));
            }

            FilteredProducts.Clear();
            foreach (var product in filtered.ToList())
            {
                FilteredProducts.Add(product);
            }
        }
    }
}