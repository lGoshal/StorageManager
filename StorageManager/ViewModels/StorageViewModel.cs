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
    public class StorageViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;

        // Коллекции
        private ObservableCollection<Storage> _storages;
        private ObservableCollection<Storage> _filteredStorages;

        // Текущие объекты
        private Storage _currentStorage;
        private Storage _selectedStorage;

        // Поиск и фильтры
        private string _searchText;
        private string _errorMessage;

        // Флаги состояния
        private bool _isEditing;
        private bool _hasErrors;

        // Свойства
        public ObservableCollection<Storage> Storages
        {
            get => _storages;
            set => SetField(ref _storages, value);
        }

        public ObservableCollection<Storage> FilteredStorages
        {
            get => _filteredStorages;
            set => SetField(ref _filteredStorages, value);
        }

        public Storage CurrentStorage
        {
            get => _currentStorage;
            set => SetField(ref _currentStorage, value);
        }

        public Storage SelectedStorage
        {
            get => _selectedStorage;
            set
            {
                if (SetField(ref _selectedStorage, value) && value != null)
                {
                    EditStorage(value);
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

        // Вычисляемые свойства
        public string FormTitle => IsEditing ? "Редактирование склада" : "Новый склад";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать склад";

        // Команды
        public ICommand LoadStoragesCommand { get; }
        public ICommand AddStorageCommand { get; }
        public ICommand EditStorageCommand { get; }
        public ICommand DeleteStorageCommand { get; }
        public ICommand SaveStorageCommand { get; }
        public ICommand CancelCommand { get; }

        // Конструктор
        public StorageViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Storages = new ObservableCollection<Storage>();
            FilteredStorages = new ObservableCollection<Storage>();

            // Инициализация команд
            LoadStoragesCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddStorageCommand = new RelayCommand(_ => AddNewStorage());
            EditStorageCommand = new RelayCommand(EditStorage);
            DeleteStorageCommand = new RelayCommand(async p => await DeleteStorageAsync(p as Storage));
            SaveStorageCommand = new RelayCommand(async _ => await SaveStorageAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());

            // Создаем новый склад по умолчанию
            CurrentStorage = new Storage();

            // Загружаем данные
            LoadStoragesCommand.Execute(null);
        }

        // Метод загрузки данных
        private async Task LoadDataAsync()
        {
            try
            {
                // Загружаем склады
                var storages = await _dbService.GetStoragesAsync();
                Storages.Clear();

                foreach (var storage in storages)
                {
                    Storages.Add(storage);
                }

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

        // Добавление нового склада
        private void AddNewStorage()
        {
            CurrentStorage = new Storage();
            IsEditing = true;
            HasErrors = false;
        }

        // Редактирование склада
        private void EditStorage(object parameter)
        {
            if (parameter is Storage storage)
            {
                CurrentStorage = new Storage
                {
                    StorageId = storage.StorageId,
                    StorageName = storage.StorageName,
                    StorageAddressId = storage.StorageAddressId,
                    FullAddress = storage.FullAddress,
                    TempAddress = storage.FullAddress // Для редактирования
                };

                IsEditing = true;
                HasErrors = false;
            }
        }

        // Удаление склада
        private async Task DeleteStorageAsync(Storage storage)
        {
            if (storage == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить склад '{storage.StorageName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.DeleteStorageAsync(storage.StorageId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }

        // Сохранение склада
        private async Task SaveStorageAsync()
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(CurrentStorage.StorageName))
            {
                ErrorMessage = "Название склада обязательно для заполнения";
                HasErrors = true;
                return;
            }

            try
            {
                // TODO: Реализовать сохранение адреса
                // Пока просто сохраняем название склада

                if (IsEditing && CurrentStorage.StorageId > 0)
                {
                    // Обновление существующего склада
                    await _dbService.UpdateStorageAsync(CurrentStorage);
                }
                else
                {
                    // Добавление нового склада
                    await _dbService.AddStorageAsync(CurrentStorage);
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
            CurrentStorage = new Storage();
            IsEditing = false;
            HasErrors = false;
        }

        // Применение фильтров
        private void ApplyFilters()
        {
            if (Storages == null || !Storages.Any())
            {
                FilteredStorages.Clear();
                return;
            }

            IEnumerable<Storage> filtered = Storages;

            // Фильтр по тексту поиска
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(s =>
                    (s.StorageName != null && s.StorageName.ToLower().Contains(searchLower)) ||
                    (s.FullAddress != null && s.FullAddress.ToLower().Contains(searchLower)));
            }

            // Обновляем отфильтрованную коллекцию
            FilteredStorages.Clear();
            foreach (var storage in filtered.ToList())
            {
                FilteredStorages.Add(storage);
            }
        }
    }
}