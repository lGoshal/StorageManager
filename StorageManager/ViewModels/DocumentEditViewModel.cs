using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    /// <summary>
    /// Логика взаимодействия для DocumentEditViewModel.cs
    /// </summary>
    public class DocumentEditViewModel : DocumentViewModel
    {
        /// <summary>
        /// Контекст/Свойства
        /// </summary>
        private readonly DatabaseService _dbService;
        private readonly DocumentListItem _originalDocument;
        public event EventHandler DocumentSaved;
        public event EventHandler DocumentCanceled;

        public DocumentEditViewModel(string connectionString, DocumentListItem documentItem)
        : base(connectionString, GetDocumentTypeFromTableName(documentItem.TableName))
        {
            _dbService = new DatabaseService(connectionString);
            _originalDocument = documentItem ?? throw new ArgumentNullException(nameof(documentItem));

            LoadExistingDocumentAsync(documentItem);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        protected override async Task SaveDraftAsync()
        {
            if (!ValidateDocument())
                return;

            try
            {
                CurrentDocument.Status = "Черновик";

                bool isNew = CurrentDocument.DocumentId == 0;

                int documentId = await DbService.SaveDocumentAsync(
                    CurrentDocument,
                    GetTableNameFromDocumentType(DocumentType),
                    isPosting: false,
                    isNew: isNew);

                if (isNew)
                {
                    CurrentDocument.DocumentId = documentId;
                }

                MessageBox.Show($"Документ {CurrentDocument.DocumentNumber} сохранен как черновик", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DocumentSaved?.Invoke(this, EventArgs.Empty);
                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                HasErrors = true;
            }
        }
        protected override async Task PostDocumentAsync()
        {
            if (!ValidateDocument())
                return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите обновить и провести документ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                CurrentDocument.Status = "Проведен";

                bool isNew = CurrentDocument.DocumentId == 0;

                int documentId = await DbService.SaveDocumentAsync(
                    CurrentDocument,
                    GetTableNameFromDocumentType(DocumentType),
                    isPosting: true,
                    isNew: isNew);

                if (isNew)
                {
                    CurrentDocument.DocumentId = documentId;
                }

                MessageBox.Show($"Документ {CurrentDocument.DocumentNumber} обновлен и проведен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DocumentSaved?.Invoke(this, EventArgs.Empty);
                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка проведения: {ex.Message}";
                HasErrors = true;
            }
        }
        protected override void CancelDocument()
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите отменить редактирование? Все несохраненные изменения будут потеряны.",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DocumentCanceled?.Invoke(this, EventArgs.Empty);
            }
        }
        private async Task<bool> UpdateDocumentInDatabaseAsync()
        {
            Console.WriteLine($"=== UpdateDocumentInDatabaseAsync: {CurrentDocument.DocumentNumber} ===");
            Console.WriteLine($"Тип документа: {DocumentType}");
            Console.WriteLine($"ID документа: {CurrentDocument.DocumentId}");

            try
            {
                // Проверяем валидность данных перед сохранением
                if (!ValidateDocumentForUpdate())
                    return false;

                // Используем DatabaseService для обновления документа
                bool documentUpdated = await DbService.UpdateDocumentAsync(CurrentDocument, GetTableNameFromDocumentType(DocumentType));

                if (!documentUpdated)
                {
                    ErrorMessage = "Не удалось обновить документ в БД";
                    return false;
                }

                // Используем DatabaseService для обновления товаров
                bool itemsUpdated = await DbService.UpdateDocumentItemsAsync(CurrentDocument, GetTableNameFromDocumentType(DocumentType));

                if (!itemsUpdated)
                {
                    ErrorMessage = "Не удалось обновить товары документа";
                    return false;
                }

                Console.WriteLine("Документ успешно обновлен в БД");
                return true;
            }
            catch (SqlException sqlEx)
            {
                ErrorMessage = $"Ошибка базы данных: {sqlEx.Message}";
                Console.WriteLine($"SQL ошибка: {sqlEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка обновления документа: {ex.Message}";
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Служебные методы
        /// </summary>
        private static string GetDocumentTypeFromTableName(string tableName)
        {
            return tableName switch
            {
                "SettingTheInitialBalances" => "SettingTheInitialBalances",
                "ProductReceipt" => "ProductReceipt",
                "MovementOfGoods" => "MovementOfGoods",
                "WriteOffOfGoods" => "WriteOffOfGoods",
                "Inventory" => "Inventory",
                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };
        }
        private async Task LoadExistingDocumentAsync(DocumentListItem documentItem)
        {
            IsLoading = true;

            try
            {
                await LoadDocumentFromDatabaseAsync(documentItem);

                CurrentDocument.DocumentNumber = documentItem.DocumentNumber;
                CurrentDocument.DocumentId = documentItem.DocumentId;
                CurrentDocument.Status = documentItem.Status;

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки документа: {ex.Message}";
                HasErrors = true;
                MessageBox.Show(ErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task LoadDocumentFromDatabaseAsync(DocumentListItem documentItem)
        {
            try
            {
                var tableName = GetTableNameFromDocumentType(DocumentType);
                var documentData = await _dbService.GetDocumentDataAsync(documentItem.DocumentId, tableName);

                if (documentData != null)
                {
                    CurrentDocument.DocumentDate = documentData.DocumentDate;
                    CurrentDocument.ResponsibleId = documentData.ResponsibleId;
                    CurrentDocument.ResponsibleName = documentData.ResponsibleName;

                    switch (tableName)
                    {
                        case "SettingTheInitialBalances":
                        case "Inventory":
                            CurrentDocument.StorageId = documentData.StorageId;
                            CurrentDocument.LocationInfo = documentData.LocationInfo;
                            break;
                        case "ProductReceipt":
                            CurrentDocument.SupplierId = documentData.SupplierId;
                            CurrentDocument.StorageId = documentData.StorageId;
                            CurrentDocument.LocationInfo = documentData.LocationInfo;
                            break;
                        case "MovementOfGoods":
                            CurrentDocument.SenderStorageId = documentData.SenderStorageId;
                            CurrentDocument.RecipientStorageId = documentData.RecipientStorageId;
                            CurrentDocument.LocationInfo = documentData.LocationInfo;
                            break;
                    }
                }

                var items = await _dbService.GetDocumentItemsAsync(documentItem.DocumentId, tableName);
                CurrentDocument.Items.Clear();

                foreach (var item in items)
                {
                    CurrentDocument.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки документа: {ex.Message}");
                throw;
            }
        }
        private bool ValidateDocumentForUpdate()
        {
            // Проверяем обязательные поля в зависимости от типа документа
            switch (DocumentType)
            {
                case "SettingTheInitialBalances":
                    if (CurrentDocument.StorageId == null)
                    {
                        ErrorMessage = "Не выбран склад";
                        return false;
                    }
                    break;
                case "Inventory":
                    if (CurrentDocument.StorageId == null)
                    {
                        ErrorMessage = "Не выбран склад";
                        return false;
                    }
                    break;

                case "ProductReceipt":
                    if (CurrentDocument.SupplierId == null)
                    {
                        ErrorMessage = "Не выбран поставщик";
                        return false;
                    }
                    break;

                case "MovementOfGoods":
                    if (CurrentDocument.SenderStorageId == null || CurrentDocument.RecipientStorageId == null)
                    {
                        ErrorMessage = "Не выбраны склады отправитель и получатель";
                        return false;
                    }
                    break;
            }

            return true;
        }
        private string GetTableNameFromDocumentType(string documentType)
        {
            return documentType switch
            {
                "SettingTheInitialBalances" => "SettingTheInitialBalances",
                "ProductReceipt" => "ProductReceipt",
                "MovementOfGoods" => "MovementOfGoods",
                "WriteOffOfGoods" => "WriteOffOfGoods",
                "Inventory" => "Inventory",
                _ => throw new ArgumentException($"Неизвестный тип документа: {documentType}")
            };
        }
    }
}