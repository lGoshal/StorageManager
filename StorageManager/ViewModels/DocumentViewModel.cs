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
    /// Логика взаимодействия для DocumentViewModel.cs
    /// </summary>
    public class DocumentViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;
        protected DatabaseService DbService => _dbService;

        private ObservableCollection<User> _responsibles;
        private ObservableCollection<Storage> _storages;
        private ObservableCollection<Partner> _suppliers;
        private ObservableCollection<Product> _products;
        private ObservableCollection<UnitOfMeasurement> _unitsOfMeasurement;
        private ObservableCollection<Characteristic> _characteristics;
        public ObservableCollection<User> Responsibles
        {
            get => _responsibles;
            set => SetField(ref _responsibles, value);
        }
        public ObservableCollection<Storage> Storages
        {
            get => _storages;
            set => SetField(ref _storages, value);
        }
        public ObservableCollection<Partner> Suppliers
        {
            get => _suppliers;
            set => SetField(ref _suppliers, value);
        }
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetField(ref _products, value);
        }
        public ObservableCollection<UnitOfMeasurement> UnitsOfMeasurement
        {
            get => _unitsOfMeasurement;
            set => SetField(ref _unitsOfMeasurement, value);
        }
        public ObservableCollection<Characteristic> Characteristics
        {
            get => _characteristics;
            set => SetField(ref _characteristics, value);
        }

        private readonly string _documentType;
        protected string DocumentType => _documentType;
        private Document _currentDocument;
        private bool _requiresSupplier;
        private bool _requiresStorage;
        private bool _requiresTwoStorages;
        private string _errorMessage;
        private bool _hasErrors;
        private bool _isLoading;
        public Document CurrentDocument
        {
            get => _currentDocument;
            set => SetField(ref _currentDocument, value);
        }
        public bool RequiresSupplier
        {
            get => _requiresSupplier;
            set => SetField(ref _requiresSupplier, value);
        }
        public bool RequiresStorage
        {
            get => _requiresStorage;
            set => SetField(ref _requiresStorage, value);
        }
        public bool RequiresTwoStorages
        {
            get => _requiresTwoStorages;
            set => SetField(ref _requiresTwoStorages, value);
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }
        public bool HasErrors
        {
            get => _hasErrors;
            set => SetField(ref _hasErrors, value);
        }
        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public string FormTitle
        {
            get
            {
                return _documentType switch
                {
                    "SettingTheInitialBalances" => "Установка начальных остатков",
                    "ProductReceipt" => "Поступление товаров",
                    "MovementOfGoods" => "Перемещение товаров",
                    "WriteOffOfGoods" => "Списание товаров",
                    "Inventory" => "Инвентаризация",
                    _ => "Новый документ"
                };
            }
        }

        public ICommand LoadDataCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand PostDocumentCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand QuickSaveCommand { get; }

        public DocumentViewModel(string connectionString, string documentType)
        {
            _dbService = new DatabaseService(connectionString);
            _documentType = documentType;

            Responsibles = new ObservableCollection<User>();
            Storages = new ObservableCollection<Storage>();
            Suppliers = new ObservableCollection<Partner>();
            Products = new ObservableCollection<Product>();
            UnitsOfMeasurement = new ObservableCollection<UnitOfMeasurement>();
            Characteristics = new ObservableCollection<Characteristic>();

            CurrentDocument = new Document
            {
                DocumentType = FormTitle,
                DocumentDate = DateTime.Now,
                DocumentNumber = GenerateDocumentNumber()
            };

            ConfigureDocumentType(documentType);

            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddItemCommand = new RelayCommand(_ => AddDocumentItem());
            RemoveItemCommand = new RelayCommand(RemoveDocumentItem);
            SaveDraftCommand = new RelayCommand(async _ => await SaveDraftAsync());
            PostDocumentCommand = new RelayCommand(async _ => await PostDocumentAsync());
            CancelCommand = new RelayCommand(_ => CancelDocument());

            LoadDataCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddDocumentItem()
        {
            CurrentDocument.Items.Add(new DocumentItem
            {
                ItemId = CurrentDocument.Items.Count + 1,
                Quantity = 1
            });
        }
        private void RemoveDocumentItem(object parameter)
        {
            if (parameter is DocumentItem item)
            {
                CurrentDocument.Items.Remove(item);
            }
        }
        protected virtual async Task SaveDraftAsync()
        {
            if (!ValidateDocument())
                MessageBox.Show($"Документ не был создан", "Неудача",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            return;

            try
            {
                CurrentDocument.Status = "Черновик";

                int documentId = await _dbService.SaveDocumentAsync(
                    CurrentDocument,
                    _documentType,
                    isPosting: false,
                    isNew: true);

                CurrentDocument.DocumentId = documentId;

                MessageBox.Show($"Документ {CurrentDocument.DocumentNumber} создан и сохранен как черновик", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                HasErrors = true;
            }
        }
        protected virtual async Task PostDocumentAsync()
        {
            if (!ValidateDocument())
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите провести документ? После проведения изменения будут отражены в учете.",
                "Подтверждение проведения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                CurrentDocument.Status = "Проведен";

                int documentId = await _dbService.SaveDocumentAsync(
                    CurrentDocument,
                    _documentType,
                    isPosting: true,
                    isNew: true);

                CurrentDocument.DocumentId = documentId;

                MessageBox.Show("Документ успешно создан и проведен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ResetForm();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка проведения документа: {ex.Message}";
                HasErrors = true;
            }
        }
        protected virtual void CancelDocument()
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите отменить создание документа? Все несохраненные данные будут потеряны.",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetForm();

                MessageBox.Show("Создание документа отменено", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Служебные методы
        /// </summary>
        protected virtual bool ValidateDocument()
        {
            if (CurrentDocument.ResponsibleId == 0)
            {
                ErrorMessage = "Выберите ответственного";
                HasErrors = true;
                return false;
            }

            if (RequiresStorage && CurrentDocument.StorageId == null)
            {
                ErrorMessage = "Выберите склад";
                HasErrors = true;
                return false;
            }

            if (RequiresSupplier && CurrentDocument.SupplierId == null)
            {
                ErrorMessage = "Выберите поставщика";
                HasErrors = true;
                return false;
            }

            if (RequiresTwoStorages &&
                (CurrentDocument.SenderStorageId == null || CurrentDocument.RecipientStorageId == null))
            {
                ErrorMessage = "Выберите склад-отправитель и склад-получатель";
                HasErrors = true;
                return false;
            }

            if (RequiresTwoStorages && CurrentDocument.SenderStorageId == CurrentDocument.RecipientStorageId)
            {
                ErrorMessage = "Склад-отправитель и склад-получатель не могут совпадать";
                HasErrors = true;
                return false;
            }

            if (!CurrentDocument.Items.Any())
            {
                ErrorMessage = "Добавьте хотя бы один товар в документ";
                HasErrors = true;
                return false;
            }

            foreach (var item in CurrentDocument.Items)
            {
                if (item.ProductId == 0)
                {
                    ErrorMessage = "Выберите товар для всех позиций";
                    HasErrors = true;
                    return false;
                }

                if (item.Quantity <= 0)
                {
                    ErrorMessage = "Количество должно быть больше 0";
                    HasErrors = true;
                    return false;
                }
            }

            HasErrors = false;
            return true;
        }
        private void ConfigureDocumentType(string documentType)
        {
            switch (documentType)
            {
                case "SettingTheInitialBalances":
                    RequiresStorage = true;
                    RequiresSupplier = false;
                    RequiresTwoStorages = false;
                    break;

                case "ProductReceipt":
                    RequiresStorage = true;
                    RequiresSupplier = true;
                    RequiresTwoStorages = false;
                    break;

                case "MovementOfGoods":
                    RequiresStorage = false;
                    RequiresSupplier = false;
                    RequiresTwoStorages = true;
                    break;

                case "WriteOffOfGoods":
                case "Inventory":
                    RequiresStorage = true;
                    RequiresSupplier = false;
                    RequiresTwoStorages = false;
                    break;
            }
        }
        private string GenerateDocumentNumber()
        {
            var prefix = _documentType switch
            {
                "SettingTheInitialBalances" => "УО",
                "ProductReceipt" => "ПР",
                "MovementOfGoods" => "ПЕ",
                "WriteOffOfGoods" => "СП",
                "Inventory" => "ИН",
                _ => "ДОК"
            };

            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
        private async Task LoadDataAsync()
        {
            try
            {
                // Загружаем все данные параллельно
                var responsiblesTask = _dbService.GetUsersAsync();
                var storagesTask = _dbService.GetStoragesAsync();
                var suppliersTask = _dbService.GetSuppliersAsync();
                var productsTask = _dbService.GetProductsAsync();
                var unitsTask = _dbService.GetUnitsOfMeasurementAsync();
                var characteristicsTask = _dbService.GetCharacteristicsAsync();

                await Task.WhenAll(
                    responsiblesTask,
                    storagesTask,
                    suppliersTask,
                    productsTask,
                    unitsTask,
                    characteristicsTask
                );

                // Заполняем коллекции
                Responsibles.Clear();
                foreach (var user in await responsiblesTask)
                    Responsibles.Add(user);

                Storages.Clear();
                foreach (var storage in await storagesTask)
                    Storages.Add(storage);

                Suppliers.Clear();
                foreach (var supplier in await suppliersTask)
                    Suppliers.Add(supplier);

                Products.Clear();
                foreach (var product in await productsTask)
                    Products.Add(product);

                UnitsOfMeasurement.Clear();
                foreach (var unit in await unitsTask)
                    UnitsOfMeasurement.Add(unit);

                Characteristics.Clear();
                foreach (var characteristic in await characteristicsTask)
                    Characteristics.Add(characteristic);

                // Устанавливаем ответственного по умолчанию (текущего пользователя)
                if (Responsibles.Any())
                    CurrentDocument.ResponsibleId = Responsibles.First().UserId;

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
                HasErrors = true;
            }
        }
        private void ResetForm()
        {
            CurrentDocument = new Document
            {
                DocumentType = FormTitle,
                DocumentDate = DateTime.Now,
                DocumentNumber = GenerateDocumentNumber()
            };

            if (Responsibles.Any())
                CurrentDocument.ResponsibleId = Responsibles.First().UserId;

            CurrentDocument.StorageId = null;
            CurrentDocument.SupplierId = null;
            CurrentDocument.SenderStorageId = null;
            CurrentDocument.RecipientStorageId = null;

            HasErrors = false;
        }
    }
}