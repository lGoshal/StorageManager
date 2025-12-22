using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StorageManager.Models;
using StorageManager.Services;
using StorageManager.Views;

namespace StorageManager.ViewModels
{
    public class DocumentsViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private Window _ownerWindow;

        // Коллекции
        private ObservableCollection<DocumentListItem> _documents;
        private ObservableCollection<DocumentTypeFilter> _documentTypes;
        private ObservableCollection<PeriodFilter> _periods;

        // Текущие фильтры
        private DocumentTypeFilter _selectedDocumentTypeFilter;
        private PeriodFilter _selectedPeriodFilter;
        private ObservableCollection<DocumentListItem> _filteredDocuments;
        private DocumentListItem _selectedDocument;
        private string _searchText;

        // Сообщения об ошибках
        private string _errorMessage;
        private bool _hasErrors;
        private bool _isLoading;

        // Свойства
        public ObservableCollection<DocumentListItem> FilteredDocuments
        {
            get => _filteredDocuments;
            set => SetField(ref _filteredDocuments, value);
        }
        public ObservableCollection<DocumentListItem> Documents
        {
            get => _documents;
            set => SetField(ref _documents, value);
        }

        public ObservableCollection<DocumentTypeFilter> DocumentTypes
        {
            get => _documentTypes;
            set => SetField(ref _documentTypes, value);
        }

        public ObservableCollection<PeriodFilter> Periods
        {
            get => _periods;
            set => SetField(ref _periods, value);
        }

        public DocumentTypeFilter SelectedDocumentTypeFilter
        {
            get => _selectedDocumentTypeFilter;
            set
            {
                if (SetField(ref _selectedDocumentTypeFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public PeriodFilter SelectedPeriodFilter
        {
            get => _selectedPeriodFilter;
            set
            {
                if (SetField(ref _selectedPeriodFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        public DocumentListItem SelectedDocument
        {
            get => _selectedDocument;
            set => SetField(ref _selectedDocument, value);
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

        // Команды
        public ICommand LoadDocumentsCommand { get; }
        public ICommand ViewDocumentCommand { get; }
        public ICommand EditDocumentCommand { get; }
        public ICommand DeleteDocumentCommand { get; }

        // Конструктор
        public DocumentsViewModel(string connectionString, Window ownerWindow = null)
        {
            _dbService = new DatabaseService(connectionString);
            _ownerWindow = ownerWindow;

            Documents = new ObservableCollection<DocumentListItem>();
            DocumentTypes = new ObservableCollection<DocumentTypeFilter>();
            Periods = new ObservableCollection<PeriodFilter>();

            // Инициализация команд
            LoadDocumentsCommand = new RelayCommand(async _ => await LoadDataAsync());
            ViewDocumentCommand = new RelayCommand(ViewDocument);
            EditDocumentCommand = new RelayCommand(EditDocument);
            DeleteDocumentCommand = new RelayCommand(async d => await DeleteDocumentAsync(d as DocumentListItem));
            FilteredDocuments = new ObservableCollection<DocumentListItem>();

            // Инициализация фильтров
            InitializeFilters();

            // Загружаем данные
            LoadDocumentsCommand.Execute(null);
        }

        // Инициализация фильтров
        private void InitializeFilters()
        {
            // Типы документов
            DocumentTypes.Clear();
            DocumentTypes.Add(new DocumentTypeFilter { DocumentTypeId = 0, DocumentTypeName = "Все типы" });
            DocumentTypes.Add(new DocumentTypeFilter { DocumentTypeId = 1, DocumentTypeName = "Установка остатков" });
            DocumentTypes.Add(new DocumentTypeFilter { DocumentTypeId = 2, DocumentTypeName = "Поступление" });
            DocumentTypes.Add(new DocumentTypeFilter { DocumentTypeId = 3, DocumentTypeName = "Перемещение" });
            DocumentTypes.Add(new DocumentTypeFilter { DocumentTypeId = 4, DocumentTypeName = "Списание" });
            DocumentTypes.Add(new DocumentTypeFilter { DocumentTypeId = 5, DocumentTypeName = "Инвентаризация" });

            // Периоды
            Periods.Clear();
            Periods.Add(new PeriodFilter { PeriodId = 0, PeriodName = "За все время" });
            Periods.Add(new PeriodFilter { PeriodId = 1, PeriodName = "Сегодня" });
            Periods.Add(new PeriodFilter { PeriodId = 2, PeriodName = "За неделю" });
            Periods.Add(new PeriodFilter { PeriodId = 3, PeriodName = "За месяц" });
            Periods.Add(new PeriodFilter { PeriodId = 4, PeriodName = "За квартал" });
            Periods.Add(new PeriodFilter { PeriodId = 5, PeriodName = "За год" });

            // Устанавливаем значения по умолчанию
            SelectedDocumentTypeFilter = DocumentTypes.First();
            SelectedPeriodFilter = Periods.First();
        }

        // Загрузка данных
        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var documents = await GetAllDocumentsAsync();

                Documents.Clear();
                foreach (var doc in documents)
                {
                    Documents.Add(doc);
                }

                ApplyFilters(); // Вызываем фильтрацию после загрузки
                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки документов: {ex.Message}";
                HasErrors = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Получение всех документов
        private async Task<ObservableCollection<DocumentListItem>> GetAllDocumentsAsync()
        {
            var documents = new ObservableCollection<DocumentListItem>();

            try
            {
                using (var conn = new SqlConnection(_dbService.ConnectionString))
                {
                    await conn.OpenAsync();

                    // Объединенный запрос для всех типов документов
                    var query = @"
                        SELECT 
                            DocumentType,
                            DocumentDate,
                            DocumentNumber,
                            Responsible,
                            LocationInfo,
                            Status,
                            DocumentId,
                            TableName,
                            ItemsCount
                        FROM (
                            -- Установка начальных остатков
                            SELECT 
                                'Установка остатков' AS DocumentType,
                                stib.SettingTheInitialBalancesDate AS DocumentDate,
                                'УО-' + CAST(stib.SettingTheInitialBalancesID AS VARCHAR) AS DocumentNumber,
                                u.UsersName AS Responsible,
                                s.StorageName AS LocationInfo,
                                'Проведен' AS Status,
                                stib.SettingTheInitialBalancesID AS DocumentId,
                                'SettingTheInitialBalances' AS TableName,
                                1 AS ItemsCount
                            FROM SettingTheInitialBalances stib
                            JOIN Users u ON stib.SettingTheInitialBalancesResponsibleID = u.UsersID
                            JOIN Storage s ON stib.SettingTheInitialBalancesStorageID = s.StorageID
                            
                            UNION ALL
                            
                            -- Поступление товаров
                            SELECT 
                                'Поступление',
                                pr.ProductReceiptDate,
                                'ПР-' + CAST(pr.ProductReceiptID AS VARCHAR),
                                u.UsersName,
                                ISNULL(p.PartnersName, s.StorageName),
                                'Проведен',
                                pr.ProductReceiptID,
                                'ProductReceipt',
                                1
                            FROM ProductReceipt pr
                            JOIN Users u ON pr.ProductReceiptResponsibleID = u.UsersID
                            JOIN Storage s ON pr.ProductReceiptStorageID = s.StorageID
                            LEFT JOIN Partners p ON pr.ProductReceiptSupplierID = p.PartnersID
                            
                            UNION ALL
                            
                            -- Перемещение товаров
                            SELECT 
                                'Перемещение',
                                mg.MovementOfGoodsDate,
                                'ПЕ-' + CAST(mg.MovementOfGoodsID AS VARCHAR),
                                u.UsersName,
                                ss.StorageName + ' → ' + rs.StorageName,
                                'Проведен',
                                mg.MovementOfGoodsID,
                                'MovementOfGoods',
                                1
                            FROM MovementOfGoods mg
                            JOIN Users u ON mg.MovementOfGoodsResponsibleID = u.UsersID
                            JOIN Storage ss ON mg.MovementOfGoodsSenderStorageID = ss.StorageID
                            JOIN Storage rs ON mg.MovementOfGoodsResepientStorageID = rs.StorageID
                            
                            UNION ALL
                            
                            -- Списание товаров
                            SELECT 
                                'Списание',
                                wog.WriteOffOfGoodsDate,
                                'СП-' + CAST(wog.WriteOffOfGoodsID AS VARCHAR),
                                u.UsersName,
                                'Списание',
                                'Проведен',
                                wog.WriteOffOfGoodsID,
                                'WriteOffOfGoods',
                                1
                            FROM WriteOffOfGoods wog
                            JOIN Users u ON wog.WriteOffOfGoodsResponsibleID = u.UsersID
                            
                            UNION ALL
                            
                            -- Инвентаризация
                            SELECT 
                                'Инвентаризация',
                                i.InventoryDate,
                                'ИН-' + CAST(i.InventoryID AS VARCHAR),
                                u.UsersName,
                                s.StorageName,
                                'Проведен',
                                i.InventoryID,
                                'Inventory',
                                1
                            FROM Inventory i
                            JOIN Users u ON i.InventoryResponsibleID = u.UsersID
                            JOIN Storage s ON i.InventoryStorageID = s.StorageID
                        ) AS AllDocuments
                        ORDER BY DocumentDate DESC, DocumentId DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var document = new DocumentListItem
                            {
                                DocumentType = reader.GetString(0),
                                DocumentDate = reader.GetDateTime(1),
                                DocumentNumber = reader.GetString(2),
                                ResponsibleName = reader.GetString(3),
                                LocationInfo = reader.GetString(4),
                                Status = reader.GetString(5),
                                DocumentId = reader.GetInt32(6),
                                TableName = reader.GetString(7),
                                ItemsCount = reader.GetInt32(8)
                            };

                            documents.Add(document);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки документов: {ex.Message}");
            }

            return documents;
        }

        // Применение фильтров
        private void ApplyFilters()
        {
            if (Documents == null || !Documents.Any())
            {
                FilteredDocuments.Clear();
                return;
            }

            IEnumerable<DocumentListItem> filtered = Documents;

            // Фильтр по типу документа
            if (SelectedDocumentTypeFilter != null && SelectedDocumentTypeFilter.DocumentTypeId > 0)
            {
                string filterType = GetDocumentTypeNameById(SelectedDocumentTypeFilter.DocumentTypeId);
                filtered = filtered.Where(d => d.DocumentType == filterType);
            }

            // Фильтр по периоду
            if (SelectedPeriodFilter != null && SelectedPeriodFilter.PeriodId > 0)
            {
                DateTime startDate = GetStartDateByPeriod(SelectedPeriodFilter.PeriodId);
                filtered = filtered.Where(d => d.DocumentDate >= startDate);
            }

            // Фильтр по поиску
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(d =>
                    (d.DocumentNumber != null && d.DocumentNumber.ToLower().Contains(searchLower)) ||
                    (d.ResponsibleName != null && d.ResponsibleName.ToLower().Contains(searchLower)) ||
                    (d.LocationInfo != null && d.LocationInfo.ToLower().Contains(searchLower)));
            }

            // Обновляем отфильтрованную коллекцию
            var filteredList = filtered.OrderByDescending(d => d.DocumentDate).ToList();

            FilteredDocuments.Clear();
            foreach (var document in filteredList)
            {
                FilteredDocuments.Add(document);
            }

            // Отладочный вывод
            Console.WriteLine($"Всего документов: {Documents.Count}, Отфильтровано: {FilteredDocuments.Count}");
        }

        private string GetDocumentTypeNameById(int typeId)
        {
            return typeId switch
            {
                1 => "Установка остатков",
                2 => "Поступление",
                3 => "Перемещение",
                4 => "Списание",
                5 => "Инвентаризация",
                _ => ""
            };
        }

        // Получение начальной даты по периоду
        private DateTime GetStartDateByPeriod(int periodId)
        {
            return periodId switch
            {
                1 => DateTime.Today, // Сегодня
                2 => DateTime.Today.AddDays(-7), // Неделя
                3 => DateTime.Today.AddMonths(-1), // Месяц
                4 => DateTime.Today.AddMonths(-3), // Квартал
                5 => DateTime.Today.AddYears(-1), // Год
                _ => DateTime.MinValue
            };
        }

        // Просмотр документа
        private void ViewDocument(object parameter)
        {
            if (parameter is DocumentListItem document)
            {
                var detailsWindow = new DocumentDetailsWindow(document, _dbService.ConnectionString);
                detailsWindow.Owner = _ownerWindow;
                detailsWindow.ShowDialog();
            }
        }

        // Редактирование документа
        private void EditDocument(object parameter)
        {
            if (parameter is DocumentListItem document)
            {
                try
                {
                    // Открываем окно редактирования
                    var editWindow = new EditDocumentWindow(document, _dbService.ConnectionString);
                    editWindow.Owner = _ownerWindow;

                    if (editWindow.ShowDialog() == true)
                    {
                        // Документ успешно сохранен - обновляем список
                        MessageBox.Show($"Документ {document.DocumentNumber} обновлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем список документов
                        LoadDocumentsCommand.Execute(null);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия редактора: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Удаление документа
        private async Task DeleteDocumentAsync(DocumentListItem document)
        {
            if (document == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить документ '{document.DocumentNumber}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Реализовать удаление документа из БД
                    await Task.Delay(100); // Заглушка

                    // Удаляем из коллекции
                    Documents.Remove(document);

                    MessageBox.Show("Документ удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
    }

    // Модели для фильтров
    public class DocumentTypeFilter
    {
        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; }
    }

    public class PeriodFilter
    {
        public int PeriodId { get; set; }
        public string PeriodName { get; set; }
    }

    // Модель для отображения в списке
    public class DocumentListItem
    {
        public string DocumentType { get; set; }
        public DateTime DocumentDate { get; set; }
        public string DocumentNumber { get; set; }
        public string ResponsibleName { get; set; }
        public string LocationInfo { get; set; }
        public string Status { get; set; }
        public int DocumentId { get; set; }
        public string TableName { get; set; }
        public int ItemsCount { get; set; }

        // Цвета для статуса
        public Brush StatusBackground
        {
            get
            {
                return Status switch
                {
                    "Проведен" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    "Черновик" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    "Отменен" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    _ => new SolidColorBrush(Color.FromRgb(255, 193, 7))
                };
            }
        }

        public Brush StatusForeground
        {
            get
            {
                return Status switch
                {
                    "Черновик" => Brushes.White,
                    _ => Brushes.White
                };
            }
        }
    }
}