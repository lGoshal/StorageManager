using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    public class DocumentDetailsViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private Document _document;
        private bool _isLoading;
        private string _errorMessage;
        private bool _hasErrors;

        public Document Document
        {
            get => _document;
            set => SetField(ref _document, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
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

        public DocumentDetailsViewModel(DocumentListItem documentItem, string connectionString)
        {
            if (documentItem == null)
                throw new ArgumentNullException(nameof(documentItem));

            _dbService = new DatabaseService(connectionString);
            LoadDocumentDetailsAsync(documentItem);
        }

        private async Task LoadDocumentDetailsAsync(DocumentListItem documentItem)
        {
            IsLoading = true;
            HasErrors = false;

            try
            {
                // Создаем документ с данными из списка
                Document = new Document
                {
                    DocumentId = documentItem.DocumentId,
                    DocumentType = documentItem.DocumentType,
                    DocumentNumber = documentItem.DocumentNumber,
                    DocumentDate = documentItem.DocumentDate,
                    ResponsibleName = documentItem.ResponsibleName,
                    Status = documentItem.Status,
                    LocationInfo = documentItem.LocationInfo
                };

                // Загружаем детали документа из БД
                await LoadDocumentItemsAsync(documentItem);

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки документа: {ex.Message}";
                HasErrors = true;

                // Если произошла ошибка, добавляем тестовые данные
                await AddTestItemsAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDocumentItemsAsync(DocumentListItem documentItem)
        {
            try
            {
                // TODO: Реализовать загрузку товаров документа из БД
                // В зависимости от TableName и DocumentId

                // Пока добавляем тестовые данные
                await AddTestItemsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки товаров документа: {ex.Message}");
                throw;
            }
        }

        private async Task AddTestItemsAsync()
        {
            if (Document == null) return;

            // Очищаем существующие товары
            Document.Items.Clear();

            // Тестовые данные
            Document.Items.Add(new DocumentItem
            {
                ItemId = 1,
                ProductName = "Молоко 'Домик в деревне'",
                ProductId = 1,
                Quantity = 100,
                UnitOfMeasurementName = "шт.",
                UnitOfMeasurementId = 1
            });

            Document.Items.Add(new DocumentItem
            {
                ItemId = 2,
                ProductName = "Телевизор Samsung 55\"",
                ProductId = 2,
                Quantity = 5,
                UnitOfMeasurementName = "шт.",
                UnitOfMeasurementId = 1
            });

            Document.Items.Add(new DocumentItem
            {
                ItemId = 3,
                ProductName = "Футболка хлопковая",
                ProductId = 3,
                Quantity = 50,
                UnitOfMeasurementName = "шт.",
                UnitOfMeasurementId = 1
            });

            await Task.CompletedTask;
        }
    }
}