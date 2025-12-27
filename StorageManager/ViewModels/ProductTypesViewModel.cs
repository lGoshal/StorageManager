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
    /// Логика взаимодействия для ProductTypesViewModel.cs
    /// </summary>
    public class ProductTypesViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<ProductType> _productTypes;
        private ObservableCollection<ProductType> _filteredProductTypes;

        private ProductType _currentProductType;
        private ProductType _selectedProductType;

        private string _searchText;
        private string _errorMessage;

        private bool _isEditing;
        private bool _hasErrors;

        public ObservableCollection<ProductType> ProductTypes
        {
            get => _productTypes;
            set => SetField(ref _productTypes, value);
        }
        public ObservableCollection<ProductType> FilteredProductTypes
        {
            get => _filteredProductTypes;
            set => SetField(ref _filteredProductTypes, value);
        }
        public ProductType CurrentProductType
        {
            get => _currentProductType;
            set => SetField(ref _currentProductType, value);
        }
        public ProductType SelectedProductType
        {
            get => _selectedProductType;
            set
            {
                if (SetField(ref _selectedProductType, value) && value != null)
                {
                    EditProductType(value);
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

        public string FormTitle => IsEditing ? "Редактирование вида товара" : "Новый вид товара";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать вид товара";

        public ICommand LoadProductTypesCommand { get; }
        public ICommand AddProductTypeCommand { get; }
        public ICommand EditProductTypeCommand { get; }
        public ICommand DeleteProductTypeCommand { get; }
        public ICommand SaveProductTypeCommand { get; }
        public ICommand CancelCommand { get; }

        public ProductTypesViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            ProductTypes = new ObservableCollection<ProductType>();
            FilteredProductTypes = new ObservableCollection<ProductType>();

            LoadProductTypesCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddProductTypeCommand = new RelayCommand(_ => AddNewProductType());
            EditProductTypeCommand = new RelayCommand(EditProductType);
            DeleteProductTypeCommand = new RelayCommand(async p => await DeleteProductTypeAsync(p as ProductType));
            SaveProductTypeCommand = new RelayCommand(async _ => await SaveProductTypeAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());

            CurrentProductType = new ProductType();

            LoadProductTypesCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewProductType()
        {
            CurrentProductType = new ProductType();
            IsEditing = true;
            HasErrors = false;
        }
        private void EditProductType(object parameter)
        {
            if (parameter is ProductType productType)
            {
                CurrentProductType = new ProductType
                {
                    ProductTypeId = productType.ProductTypeId,
                    ProductTypeName = productType.ProductTypeName
                };

                IsEditing = true;
                HasErrors = false;
            }
        }
        private async Task DeleteProductTypeAsync(ProductType productType)
        {
            if (productType == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить вид товара '{productType.ProductTypeName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.DeleteProductTypeAsync(productType.ProductTypeId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
        private async Task SaveProductTypeAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentProductType.ProductTypeName))
            {
                ErrorMessage = "Название вида товара обязательно для заполнения";
                HasErrors = true;
                return;
            }

            try
            {
                if (IsEditing && CurrentProductType.ProductTypeId > 0)
                {
                    await _dbService.UpdateProductTypeAsync(CurrentProductType);
                }
                else
                {
                    await _dbService.AddProductTypeAsync(CurrentProductType);
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
            CurrentProductType = new ProductType();
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
                var productTypes = await _dbService.GetProductTypesAsync();
                ProductTypes.Clear();

                foreach (var type in productTypes)
                {
                    ProductTypes.Add(type);
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
            if (ProductTypes == null || !ProductTypes.Any())
            {
                FilteredProductTypes.Clear();
                return;
            }

            IEnumerable<ProductType> filtered = ProductTypes;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(pt =>
                    pt.ProductTypeName != null &&
                    pt.ProductTypeName.ToLower().Contains(searchLower));
            }

            FilteredProductTypes.Clear();
            foreach (var type in filtered.ToList())
            {
                FilteredProductTypes.Add(type);
            }
        }
    }
}