using System;
using System.Threading.Tasks;
using System.Windows;
using StorageManager.Models;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    public class DocumentEditViewModel : DocumentViewModel
    {
        private readonly DocumentListItem _originalDocument;
        public event EventHandler DocumentSaved;
        public event EventHandler DocumentCanceled;

        public DocumentEditViewModel(string connectionString, DocumentListItem documentItem)
            : base(connectionString, GetDocumentTypeFromTableName(documentItem.TableName))
        {
            _originalDocument = documentItem ?? throw new ArgumentNullException(nameof(documentItem));

            // Загружаем существующий документ
            LoadExistingDocumentAsync(documentItem);
        }

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
                // Загружаем данные документа из БД
                await LoadDocumentFromDatabaseAsync(documentItem);

                // Обновляем номер документа (сохраняем оригинальный)
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
            // TODO: Реализовать загрузку документа из БД
            // В зависимости от TableName и DocumentId

            // Пока создаем тестовый документ
            CurrentDocument.DocumentType = documentItem.DocumentType;
            CurrentDocument.DocumentDate = documentItem.DocumentDate;
            CurrentDocument.ResponsibleName = documentItem.ResponsibleName;
            CurrentDocument.LocationInfo = documentItem.LocationInfo;

            // Добавляем тестовые товары
            await AddTestItemsAsync();
        }

        // Переопределяем метод сохранения черновика
        protected override async Task SaveDraftAsync()
        {
            if (!ValidateDocument())
                return;

            try
            {
                // Обновляем статус
                CurrentDocument.Status = "Черновик";

                // TODO: Реализовать обновление документа в БД
                bool success = await UpdateDocumentInDatabaseAsync();

                if (success)
                {
                    MessageBox.Show($"Документ {CurrentDocument.DocumentNumber} обновлен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    DocumentSaved?.Invoke(this, EventArgs.Empty);
                    HasErrors = false;
                }
                else
                {
                    ErrorMessage = "Ошибка обновления документа";
                    HasErrors = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка сохранения: {ex.Message}";
                HasErrors = true;
            }
        }

        // Переопределяем метод проведения документа
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

                // TODO: Реализовать обновление и проведение документа в БД
                bool success = await UpdateDocumentInDatabaseAsync();

                if (success)
                {
                    MessageBox.Show($"Документ {CurrentDocument.DocumentNumber} обновлен и проведен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    DocumentSaved?.Invoke(this, EventArgs.Empty);
                    HasErrors = false;
                }
                else
                {
                    ErrorMessage = "Ошибка проведения документа";
                    HasErrors = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка проведения: {ex.Message}";
                HasErrors = true;
            }
        }

        // Переопределяем метод отмены
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
            // TODO: Реализовать обновление документа в БД
            // В зависимости от типа документа и TableName

            await Task.Delay(500); // Имитация сохранения

            // Возвращаем true для тестирования
            return true;
        }

        private async Task AddTestItemsAsync()
        {
            // Очищаем существующие товары
            CurrentDocument.Items.Clear();

            // Тестовые данные
            CurrentDocument.Items.Add(new DocumentItem
            {
                ItemId = 1,
                ProductName = "Товар 1 (редактируемый)",
                ProductId = 1,
                Quantity = 10,
                UnitOfMeasurementName = "шт."
            });

            CurrentDocument.Items.Add(new DocumentItem
            {
                ItemId = 2,
                ProductName = "Товар 2 (редактируемый)",
                ProductId = 2,
                Quantity = 5,
                UnitOfMeasurementName = "кг"
            });

            await Task.CompletedTask;
        }
    }
}