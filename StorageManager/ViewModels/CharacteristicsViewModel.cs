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
    /// Логика взаимодействия для CharacteristicsViewModel.cs
    /// </summary>
    public class CharacteristicsViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<Characteristic> _characteristics;
        private ObservableCollection<Characteristic> _filteredCharacteristics;

        private Characteristic _currentCharacteristic;
        private Characteristic _selectedCharacteristic;
        private string _searchText;
        private string _errorMessage;
        private bool _isEditing;
        private bool _hasErrors;
        public ObservableCollection<Characteristic> Characteristics
        {
            get => _characteristics;
            set => SetField(ref _characteristics, value);
        }
        public ObservableCollection<Characteristic> FilteredCharacteristics
        {
            get => _filteredCharacteristics;
            set => SetField(ref _filteredCharacteristics, value);
        }
        public Characteristic CurrentCharacteristic
        {
            get => _currentCharacteristic;
            set => SetField(ref _currentCharacteristic, value);
        }
        public Characteristic SelectedCharacteristic
        {
            get => _selectedCharacteristic;
            set
            {
                if (SetField(ref _selectedCharacteristic, value) && value != null)
                {
                    EditCharacteristic(value);
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

        public string FormTitle => IsEditing ? "Редактирование характеристики" : "Новая характеристика";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать характеристику";

        public ICommand LoadCharacteristicsCommand { get; }
        public ICommand AddCharacteristicCommand { get; }
        public ICommand EditCharacteristicCommand { get; }
        public ICommand DeleteCharacteristicCommand { get; }
        public ICommand SaveCharacteristicCommand { get; }
        public ICommand CancelCommand { get; }

        public CharacteristicsViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Characteristics = new ObservableCollection<Characteristic>();
            FilteredCharacteristics = new ObservableCollection<Characteristic>();

            LoadCharacteristicsCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddCharacteristicCommand = new RelayCommand(_ => AddNewCharacteristic());
            EditCharacteristicCommand = new RelayCommand(EditCharacteristic);
            DeleteCharacteristicCommand = new RelayCommand(async p => await DeleteCharacteristicAsync(p as Characteristic));
            SaveCharacteristicCommand = new RelayCommand(async _ => await SaveCharacteristicAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());

            CurrentCharacteristic = new Characteristic();

            LoadCharacteristicsCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewCharacteristic()
        {
            CurrentCharacteristic = new Characteristic();
            IsEditing = true;
            HasErrors = false;
        }
        private void EditCharacteristic(object parameter)
        {
            if (parameter is Characteristic characteristic)
            {
                CurrentCharacteristic = new Characteristic
                {
                    CharacteristicId = characteristic.CharacteristicId,
                    CharacteristicName = characteristic.CharacteristicName,
                    CharacteristicDescription = characteristic.CharacteristicDescription
                };

                IsEditing = true;
                HasErrors = false;
            }
        }
        private async Task DeleteCharacteristicAsync(Characteristic characteristic)
        {
            if (characteristic == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить характеристику '{characteristic.CharacteristicName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.DeleteCharacteristicAsync(characteristic.CharacteristicId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
        private async Task SaveCharacteristicAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentCharacteristic.CharacteristicName))
            {
                ErrorMessage = "Название характеристики обязательно для заполнения";
                HasErrors = true;
                return;
            }

            try
            {
                if (IsEditing && CurrentCharacteristic.CharacteristicId > 0)
                {
                    await _dbService.UpdateCharacteristicAsync(CurrentCharacteristic);
                }
                else
                {
                    await _dbService.AddCharacteristicAsync(CurrentCharacteristic);
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
            CurrentCharacteristic = new Characteristic();
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
                var characteristics = await _dbService.GetCharacteristicsAsync();
                Characteristics.Clear();

                foreach (var characteristic in characteristics)
                {
                    Characteristics.Add(characteristic);
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
            if (Characteristics == null || !Characteristics.Any())
            {
                FilteredCharacteristics.Clear();
                return;
            }

            IEnumerable<Characteristic> filtered = Characteristics;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    (c.CharacteristicName != null && c.CharacteristicName.ToLower().Contains(searchLower)) ||
                    (c.CharacteristicDescription != null && c.CharacteristicDescription.ToLower().Contains(searchLower)));
            }

            FilteredCharacteristics.Clear();
            foreach (var characteristic in filtered.ToList())
            {
                FilteredCharacteristics.Add(characteristic);
            }
        }
    }
}