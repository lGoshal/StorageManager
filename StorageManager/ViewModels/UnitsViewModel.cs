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
    /// Логика взаимодействия для UnitsViewModel.cs
    /// </summary>
    public class UnitsViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Свойства/Коллекции/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<UnitOfMeasurement> _units;
        private ObservableCollection<UnitOfMeasurement> _filteredUnits;

        private UnitOfMeasurement _currentUnit;
        private UnitOfMeasurement _selectedUnit;

        private string _searchText;
        private string _errorMessage;

        private bool _isEditing;
        private bool _hasErrors;

        public ObservableCollection<UnitOfMeasurement> Units
        {
            get => _units;
            set => SetField(ref _units, value);
        }
        public ObservableCollection<UnitOfMeasurement> FilteredUnits
        {
            get => _filteredUnits;
            set => SetField(ref _filteredUnits, value);
        }
        public UnitOfMeasurement CurrentUnit
        {
            get => _currentUnit;
            set => SetField(ref _currentUnit, value);
        }
        public UnitOfMeasurement SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (SetField(ref _selectedUnit, value) && value != null)
                {
                    EditUnit(value);
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

        public string FormTitle => IsEditing ? "Редактирование единицы измерения" : "Новая единица измерения";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать единицу";

        public ICommand LoadUnitsCommand { get; }
        public ICommand AddUnitCommand { get; }
        public ICommand EditUnitCommand { get; }
        public ICommand DeleteUnitCommand { get; }
        public ICommand SaveUnitCommand { get; }
        public ICommand CancelCommand { get; }

        public UnitsViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Units = new ObservableCollection<UnitOfMeasurement>();
            FilteredUnits = new ObservableCollection<UnitOfMeasurement>();

            LoadUnitsCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddUnitCommand = new RelayCommand(_ => AddNewUnit());
            EditUnitCommand = new RelayCommand(EditUnit);
            DeleteUnitCommand = new RelayCommand(async p => await DeleteUnitAsync(p as UnitOfMeasurement));
            SaveUnitCommand = new RelayCommand(async _ => await SaveUnitAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());

            CurrentUnit = new UnitOfMeasurement();

            LoadUnitsCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewUnit()
        {
            CurrentUnit = new UnitOfMeasurement();
            IsEditing = true;
            HasErrors = false;
        }
        private void EditUnit(object parameter)
        {
            if (parameter is UnitOfMeasurement unit)
            {
                CurrentUnit = new UnitOfMeasurement
                {
                    UnitOfMeasurementId = unit.UnitOfMeasurementId,
                    UnitOfMeasurementName = unit.UnitOfMeasurementName
                };

                IsEditing = true;
                HasErrors = false;
            }
        }
        private async Task DeleteUnitAsync(UnitOfMeasurement unit)
        {
            if (unit == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить единицу измерения '{unit.UnitOfMeasurementName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.DeleteUnitOfMeasurementAsync(unit.UnitOfMeasurementId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
        private async Task SaveUnitAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentUnit.UnitOfMeasurementName))
            {
                ErrorMessage = "Название единицы измерения обязательно для заполнения";
                HasErrors = true;
                return;
            }

            try
            {
                if (IsEditing && CurrentUnit.UnitOfMeasurementId > 0)
                {
                    await _dbService.UpdateUnitOfMeasurementAsync(CurrentUnit);
                }
                else
                {
                    await _dbService.AddUnitOfMeasurementAsync(CurrentUnit);
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
            CurrentUnit = new UnitOfMeasurement();
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
                var units = await _dbService.GetUnitsOfMeasurementAsync();
                Units.Clear();

                foreach (var unit in units)
                {
                    Units.Add(unit);
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
            if (Units == null || !Units.Any())
            {
                FilteredUnits.Clear();
                return;
            }

            IEnumerable<UnitOfMeasurement> filtered = Units;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(u =>
                    u.UnitOfMeasurementName != null &&
                    u.UnitOfMeasurementName.ToLower().Contains(searchLower));
            }

            FilteredUnits.Clear();
            foreach (var unit in filtered.ToList())
            {
                FilteredUnits.Add(unit);
            }
        }
    }
}