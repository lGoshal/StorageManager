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
    public class DocumentViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private readonly string _documentType;

        // Основные коллекции
        private ObservableCollection<User> _responsibles;
        private ObservableCollection<Storage> _storages;
        private ObservableCollection<Partner> _suppliers;
        private ObservableCollection<Product> _products;
        private ObservableCollection<UnitOfMeasurement> _unitsOfMeasurement;
        private ObservableCollection<Characteristic> _characteristics;

        // Текущий документ
        private Document _currentDocument;

        // Флаги для отображения полей формы
        private bool _requiresSupplier;
        private bool _requiresStorage;
        private bool _requiresTwoStorages;

        // Сообщения об ошибках
        private string _errorMessage;
        private bool _hasErrors;
        private bool _isLoading;

        // Свойства
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

        // Вычисляемые свойства
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

        // Команды
        public ICommand LoadDataCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand PostDocumentCommand { get; }
        public ICommand CancelCommand { get; }

        // Конструктор
        public DocumentViewModel(string connectionString, string documentType)
        {
            _dbService = new DatabaseService(connectionString);
            _documentType = documentType;

            // Инициализация коллекций
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddItemCommand = new RelayCommand(_ => AddDocumentItem());
            RemoveItemCommand = new RelayCommand(RemoveDocumentItem);
            SaveDraftCommand = new RelayCommand(async _ => await SaveDraftAsync());
            PostDocumentCommand = new RelayCommand(async _ => await PostDocumentAsync());
            CancelCommand = new RelayCommand(_ => CancelDocument());

            // Инициализация документа
            CurrentDocument = new Document
            {
                DocumentType = FormTitle,
                DocumentDate = DateTime.Now,
                DocumentNumber = GenerateDocumentNumber(),
                Status = "Черновик"
            };

            // Настройка видимости полей в зависимости от типа документа
            ConfigureDocumentType(documentType);

            // Инициализация команд
            LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddItemCommand = new RelayCommand(_ => AddDocumentItem());
            RemoveItemCommand = new RelayCommand(RemoveDocumentItem);
            SaveDraftCommand = new RelayCommand(async _ => await SaveDraftAsync());
            PostDocumentCommand = new RelayCommand(async _ => await PostDocumentAsync());
            CancelCommand = new RelayCommand(_ => CancelDocument());

            // Загружаем данные
            LoadDataCommand.Execute(null);
        }

        // Настройка типа документа
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
        // Генерация номера документа
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

        // Загрузка данных
        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Загружаем пользователей
                await LoadUsersAsync();

                // Загружаем склады
                await LoadStoragesAsync();

                // Загружаем поставщиков (только если нужно)
                if (RequiresSupplier)
                {
                    await LoadSuppliersAsync();
                }

                // Загружаем товары
                await LoadProductsAsync();

                // Загружаем единицы измерения
                await LoadUnitsOfMeasurementAsync();

                // Загружаем характеристики
                await LoadCharacteristicsAsync();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки данных: {ex.Message}";
                HasErrors = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Методы загрузки данных (заглушки - реализуйте их в DatabaseService)
        private async Task LoadUsersAsync()
        {
            try
            {
                // TODO: Вызовите метод из DatabaseService
                var users = await _dbService.GetUsersAsync();
                Responsibles.Clear();
                foreach (var user in users)
                {
                    Responsibles.Add(user);
                }

                // Устанавливаем первого пользователя по умолчанию
                if (Responsibles.Any())
                {
                    CurrentDocument.ResponsibleId = Responsibles.First().UserId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        private async Task LoadStoragesAsync()
        {
            try
            {
                // TODO: Вызовите метод из DatabaseService
                var storages = await _dbService.GetStoragesAsync();
                Storages.Clear();
                foreach (var storage in storages)
                {
                    Storages.Add(storage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки складов: {ex.Message}");
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                // TODO: Вызовите метод из DatabaseService
                var suppliers = await _dbService.GetSuppliersAsync();
                Suppliers.Clear();
                foreach (var supplier in suppliers)
                {
                    Suppliers.Add(supplier);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки поставщиков: {ex.Message}");
            }
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
                Console.WriteLine($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private async Task LoadUnitsOfMeasurementAsync()
        {
            try
            {
                var units = await _dbService.GetUnitsOfMeasurementAsync();
                UnitsOfMeasurement.Clear();
                foreach (var unit in units)
                {
                    UnitsOfMeasurement.Add(unit);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки единиц измерения: {ex.Message}");
            }
        }

        private async Task LoadCharacteristicsAsync()
        {
            try
            {
                var characteristics = await _dbService.GetCharacteristicsAsync();
                Characteristics.Clear();
                foreach (var characteristic in characteristics)
                {
                    Characteristics.Add(characteristic);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки характеристик: {ex.Message}");
            }
        }

        // Добавление строки в табличную часть
        private void AddDocumentItem()
        {
            var newItem = new DocumentItem
            {
                ItemId = CurrentDocument.Items.Count + 1,
                Quantity = 1
            };

            CurrentDocument.Items.Add(newItem);
        }

        // Удаление строки из табличной части
        private void RemoveDocumentItem(object parameter)
        {
            if (parameter is DocumentItem item)
            {
                CurrentDocument.Items.Remove(item);
            }
        }

        // Сохранение черновика
        protected virtual async Task SaveDraftAsync()
        {
            if (!ValidateDocument())
                return;

            try
            {
                CurrentDocument.Status = "Черновик";

                // TODO: Реализовать сохранение в БД
                // Пока заглушка
                await Task.Delay(100);

                MessageBox.Show($"Черновик сохранен. Номер: {CurrentDocument.DocumentNumber}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                HasErrors = true;
            }
        }

        // Проведение документа
        protected virtual async Task PostDocumentAsync()
        {
            if (!ValidateDocument())
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите провести документ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                CurrentDocument.Status = "Проведен";

                // TODO: Реализовать проведение в БД
                // Пока заглушка
                await Task.Delay(100);

                MessageBox.Show($"Документ проведен. Номер: {CurrentDocument.DocumentNumber}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Сбрасываем форму
                ResetForm();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка проведения: {ex.Message}";
                HasErrors = true;
            }
        }

        // Валидация документа
        protected virtual bool ValidateDocument()
        {
            // Проверка основных реквизитов
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

            // Проверка табличной части
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

        // Отмена документа
        protected virtual void CancelDocument()
        {
            var result = MessageBox.Show(
                "Отменить создание документа?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetForm();
                MessageBox.Show("Создание отменено", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Сброс формы
        private void ResetForm()
        {
            CurrentDocument = new Document
            {
                DocumentType = FormTitle,
                DocumentDate = DateTime.Now,
                DocumentNumber = GenerateDocumentNumber(),
                Status = "Черновик"
            };

            // Сбрасываем выбранные значения
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