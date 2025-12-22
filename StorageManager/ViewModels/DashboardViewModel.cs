using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;

        // Статистика
        private int _productsCount;
        private int _warehousesCount;
        private int _partnersCount;
        private int _documentsCount;
        private string _productsChange;
        private string _warehousesChange;
        private string _partnersChange;
        private string _documentsChange;
        private bool _isLoading;

        // Коллекции
        private ObservableCollection<RecentDocument> _recentDocuments;

        // Свойства
        public int ProductsCount
        {
            get => _productsCount;
            set => SetField(ref _productsCount, value);
        }

        public int WarehousesCount
        {
            get => _warehousesCount;
            set => SetField(ref _warehousesCount, value);
        }

        public int PartnersCount
        {
            get => _partnersCount;
            set => SetField(ref _partnersCount, value);
        }

        public int DocumentsCount
        {
            get => _documentsCount;
            set => SetField(ref _documentsCount, value);
        }

        public string ProductsChange
        {
            get => _productsChange;
            set => SetField(ref _productsChange, value);
        }

        public string WarehousesChange
        {
            get => _warehousesChange;
            set => SetField(ref _warehousesChange, value);
        }

        public string PartnersChange
        {
            get => _partnersChange;
            set => SetField(ref _partnersChange, value);
        }

        public string DocumentsChange
        {
            get => _documentsChange;
            set => SetField(ref _documentsChange, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public ObservableCollection<RecentDocument> RecentDocuments
        {
            get => _recentDocuments;
            set => SetField(ref _recentDocuments, value);
        }

        // Команды
        public ICommand LoadDashboardDataCommand { get; }

        // Конструктор
        public DashboardViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);
            RecentDocuments = new ObservableCollection<RecentDocument>();

            LoadDashboardDataCommand = new RelayCommand(async _ => await LoadDashboardDataAsync());

            // Загружаем данные при создании
            LoadDashboardDataCommand.Execute(null);
        }

        // Метод загрузки данных дашборда
        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;

            try
            {
                await Task.Run(async () =>
                {
                    // Загружаем статистику
                    await LoadStatisticsAsync();

                    // Загружаем последние документы
                    await LoadRecentDocumentsAsync();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                // Товары - сравниваем с прошлой неделей
                var products = await _dbService.GetProductsAsync();
                ProductsCount = products.Count;

                var productsLastWeek = await _dbService.GetProductsCountLastWeekAsync();
                var productsDiff = ProductsCount - productsLastWeek;
                ProductsChange = FormatChangeText(productsDiff, "за неделю");

                // Склады - сравниваем с прошлым месяцем
                var warehouses = await _dbService.GetStoragesAsync();
                WarehousesCount = warehouses.Count;

                var warehousesLastMonth = await _dbService.GetWarehousesCountLastMonthAsync();
                var warehousesDiff = WarehousesCount - warehousesLastMonth;
                WarehousesChange = FormatChangeText(warehousesDiff, "за месяц");

                // Контрагенты - сравниваем с прошлым месяцем
                var partnersCount = await GetPartnersCountAsync();
                PartnersCount = partnersCount;

                var partnersLastMonth = await _dbService.GetPartnersCountLastMonthAsync();
                var partnersDiff = PartnersCount - partnersLastMonth;
                PartnersChange = FormatChangeText(partnersDiff, "за месяц");

                // Документы - сравниваем с прошлой неделей
                var documentsCount = await GetDocumentsCountAsync();
                DocumentsCount = documentsCount;

                var documentsLastWeek = await _dbService.GetDocumentsCountLastWeekAsync();
                var documentsDiff = DocumentsCount - documentsLastWeek;
                DocumentsChange = FormatChangeText(documentsDiff, "за неделю");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки статистики: {ex.Message}");
                // Устанавливаем заглушки при ошибке
                SetDefaultChangeTexts();
            }
        }

        // Метод для форматирования текста изменения
        private string FormatChangeText(int difference, string period)
        {
            if (difference > 0)
            {
                return $"+{difference} {period}";
            }
            else if (difference < 0)
            {
                return $"{difference} {period}";
            }
            else
            {
                return $"Без изменений {period}";
            }
        }

        // Метод для установки значений по умолчанию при ошибке
        private void SetDefaultChangeTexts()
        {
            ProductsChange = "Нет данных";
            WarehousesChange = "Нет данных";
            PartnersChange = "Нет данных";
            DocumentsChange = "Нет данных";
        }

        private async Task LoadRecentDocumentsAsync()
        {
            try
            {
                var documents = await GetRecentDocumentsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    RecentDocuments.Clear();
                    foreach (var doc in documents)
                    {
                        RecentDocuments.Add(doc);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки документов: {ex.Message}");
            }
        }

        // Вспомогательные методы для работы с БД
        private async Task<int> GetPartnersCountAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_dbService.ConnectionString))
                {
                    await conn.OpenAsync();
                    var query = "SELECT COUNT(*) FROM Partners";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetDocumentsCountAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_dbService.ConnectionString))
                {
                    await conn.OpenAsync();
                    var query = @"
                        SELECT COUNT(*) FROM (
                            SELECT SettingTheInitialBalancesID FROM SettingTheInitialBalances
                            UNION ALL
                            SELECT ProductReceiptID FROM ProductReceipt
                            UNION ALL
                            SELECT MovementOfGoodsID FROM MovementOfGoods
                            UNION ALL
                            SELECT WriteOffOfGoodsID FROM WriteOffOfGoods
                            UNION ALL
                            SELECT InventoryID FROM Inventory
                        ) AS AllDocuments";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private async Task<ObservableCollection<RecentDocument>> GetRecentDocumentsAsync()
        {
            var documents = new ObservableCollection<RecentDocument>();

            try
            {
                using (var conn = new SqlConnection(_dbService.ConnectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                        SELECT TOP 10 
                            DocumentType,
                            DocumentDate,
                            DocumentNumber,
                            Responsible,
                            Location,
                            Counterparty,
                            Status
                        FROM (
                            SELECT 
                                'Установка остатка' AS DocumentType,
                                stib.SettingTheInitialBalancesDate AS DocumentDate,
                                'УО-' + CAST(stib.SettingTheInitialBalancesID AS VARCHAR) AS DocumentNumber,
                                u.UsersName AS Responsible,
                                s.StorageName AS Location,
                                NULL AS Counterparty,
                                'Завершен' AS Status,
                                stib.SettingTheInitialBalancesDate AS SortDate
                            FROM SettingTheInitialBalances stib
                            JOIN Users u ON stib.SettingTheInitialBalancesResponsibleID = u.UsersID
                            JOIN Storage s ON stib.SettingTheInitialBalancesStorageID = s.StorageID
                            
                            UNION ALL
                            
                            SELECT 
                                'Поступление',
                                pr.ProductReceiptDate,
                                'ПР-' + CAST(pr.ProductReceiptID AS VARCHAR),
                                u.UsersName,
                                s.StorageName,
                                p.PartnersName,
                                'Принят',
                                pr.ProductReceiptDate
                            FROM ProductReceipt pr
                            JOIN Users u ON pr.ProductReceiptResponsibleID = u.UsersID
                            JOIN Storage s ON pr.ProductReceiptStorageID = s.StorageID
                            LEFT JOIN Partners p ON pr.ProductReceiptSupplierID = p.PartnersID
                            
                            UNION ALL
                            
                            SELECT 
                                'Перемещение',
                                mg.MovementOfGoodsDate,
                                'ПЕ-' + CAST(mg.MovementOfGoodsID AS VARCHAR),
                                u.UsersName,
                                ss.StorageName + ' → ' + rs.StorageName,
                                NULL,
                                'Выполнено',
                                mg.MovementOfGoodsDate
                            FROM MovementOfGoods mg
                            JOIN Users u ON mg.MovementOfGoodsResponsibleID = u.UsersID
                            JOIN Storage ss ON mg.MovementOfGoodsSenderStorageID = ss.StorageID
                            JOIN Storage rs ON mg.MovementOfGoodsResepientStorageID = rs.StorageID
                            
                            UNION ALL
                            
                            SELECT 
                                'Списание',
                                wog.WriteOffOfGoodsDate,
                                'СП-' + CAST(wog.WriteOffOfGoodsID AS VARCHAR),
                                u.UsersName,
                                'Списание',
                                NULL,
                                'Подтверждено',
                                wog.WriteOffOfGoodsDate
                            FROM WriteOffOfGoods wog
                            JOIN Users u ON wog.WriteOffOfGoodsResponsibleID = u.UsersID
                            
                            UNION ALL
                            
                            SELECT 
                                'Инвентаризация',
                                i.InventoryDate,
                                'ИН-' + CAST(i.InventoryID AS VARCHAR),
                                u.UsersName,
                                s.StorageName,
                                NULL,
                                'Проведена',
                                i.InventoryDate
                            FROM Inventory i
                            JOIN Users u ON i.InventoryResponsibleID = u.UsersID
                            JOIN Storage s ON i.InventoryStorageID = s.StorageID
                        ) AS AllDocuments
                        ORDER BY SortDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new RecentDocument
                            {
                                DocumentType = reader.GetString(0),
                                DocumentDate = reader.GetDateTime(1),
                                DocumentNumber = reader.GetString(2),
                                Responsible = reader.GetString(3),
                                Location = reader.GetString(4),
                                Counterparty = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Status = reader.GetString(6)
                            });
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

        // Добавим свойство для доступа к строке подключения
        public string ConnectionString => _dbService.ConnectionString;
    }
}