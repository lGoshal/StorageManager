using System.Collections.ObjectModel;
using System.Windows.Input;
using StorageManager.Models;
using System.Linq;
using System.Windows;
using System;
using StorageManager.Services;

namespace StorageManager.ViewModels
{
    /// <summary>
    /// Логика взаимодействия для ContractsViewModel.cs
    /// </summary>
    public class ContractsViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Методы
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<Contract> _contracts;
        private ObservableCollection<Contract> _filteredContracts;
        private ObservableCollection<dynamic> _customers;
        private ObservableCollection<dynamic> _contractors;
        private ObservableCollection<UnitOfMeasurement> _units;

        private Contract _currentContract;
        private Contract _selectedContract;
        private string _searchText;
        private string _errorMessage;
        private bool _isEditing;
        private bool _hasErrors;

        public ObservableCollection<Contract> Contracts
        {
            get => _contracts;
            set => SetField(ref _contracts, value);
        }
        public ObservableCollection<Contract> FilteredContracts
        {
            get => _filteredContracts;
            set => SetField(ref _filteredContracts, value);
        }
        public ObservableCollection<dynamic> Customers
        {
            get => _customers;
            set => SetField(ref _customers, value);
        }
        public ObservableCollection<dynamic> Contractors
        {
            get => _contractors;
            set => SetField(ref _contractors, value);
        }
        public ObservableCollection<UnitOfMeasurement> Units
        {
            get => _units;
            set => SetField(ref _units, value);
        }
        public Contract CurrentContract
        {
            get => _currentContract;
            set => SetField(ref _currentContract, value);
        }
        public Contract SelectedContract
        {
            get => _selectedContract;
            set
            {
                if (SetField(ref _selectedContract, value) && value != null)
                {
                    EditContract(value);
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

        public string FormTitle => IsEditing ? "Редактирование договора" : "Новый договор";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать договор";

        public ICommand LoadContractsCommand { get; }
        public ICommand AddContractCommand { get; }
        public ICommand EditContractCommand { get; }
        public ICommand DeleteContractCommand { get; }
        public ICommand SaveContractCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand LoadDropdownDataCommand { get; }

        public ContractsViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Contracts = new ObservableCollection<Contract>();
            FilteredContracts = new ObservableCollection<Contract>();
            Customers = new ObservableCollection<dynamic>();
            Contractors = new ObservableCollection<dynamic>();
            Units = new ObservableCollection<UnitOfMeasurement>();

            LoadContractsCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddContractCommand = new RelayCommand(_ => AddNewContract());
            EditContractCommand = new RelayCommand(EditContract);
            DeleteContractCommand = new RelayCommand(async p => await DeleteContractAsync(p as Contract));
            SaveContractCommand = new RelayCommand(async _ => await SaveContractAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());
            LoadDropdownDataCommand = new RelayCommand(async _ => await LoadDropdownDataAsync());

            CurrentContract = new Contract();

            LoadContractsCommand.Execute(null);
            LoadDropdownDataCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewContract()
        {
            CurrentContract = new Contract();
            IsEditing = true;
            HasErrors = false;
        }
        private void EditContract(object parameter)
        {
            if (parameter is Contract contract)
            {
                CurrentContract = new Contract
                {
                    ContractsID = contract.ContractsID,
                    ContractsName = contract.ContractsName,
                    ContractsObject = contract.ContractsObject,
                    ContractsCustomerID = contract.ContractsCustomerID,
                    ContractsContractorID = contract.ContractsContractorID,
                    ContractsValue = contract.ContractsValue,
                    ContractsTimeOfAction = contract.ContractsTimeOfAction,
                    ContractsExpirationDateUnitID = contract.ContractsExpirationDateUnitID
                };

                IsEditing = true;
                HasErrors = false;
            }
        }
        private async Task DeleteContractAsync(Contract contract)
        {
            if (contract == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить договор '{contract.ContractsName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _dbService.DeleteContractAsync(contract.ContractsID);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
        private async Task SaveContractAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentContract.ContractsName))
            {
                ErrorMessage = "Название договора обязательно для заполнения";
                HasErrors = true;
                return;
            }

            if (CurrentContract.ContractsCustomerID <= 0)
            {
                ErrorMessage = "Необходимо выбрать заказчика";
                HasErrors = true;
                return;
            }

            if (CurrentContract.ContractsContractorID <= 0)
            {
                ErrorMessage = "Необходимо выбрать подрядчика";
                HasErrors = true;
                return;
            }

            if (CurrentContract.ContractsValue < 0)
            {
                ErrorMessage = "Стоимость договора не может быть отрицательной";
                HasErrors = true;
                return;
            }

            if (CurrentContract.ContractsTimeOfAction < 0)
            {
                ErrorMessage = "Срок действия не может быть отрицательным";
                HasErrors = true;
                return;
            }

            if (CurrentContract.ContractsExpirationDateUnitID <= 0)
            {
                ErrorMessage = "Необходимо выбрать единицу измерения срока";
                HasErrors = true;
                return;
            }

            try
            {
                if (IsEditing && CurrentContract.ContractsID > 0)
                {
                    await _dbService.UpdateContractAsync(CurrentContract);
                }
                else
                {
                    await _dbService.AddContractAsync(CurrentContract);
                }

                await LoadDataAsync();
                await LoadDropdownDataAsync();

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
            CurrentContract = new Contract();
            IsEditing = false;
            HasErrors = false;
        }

        /// <summary>
        /// Служебные методы
        /// </summary>
        private async Task LoadDropdownDataAsync()
        {
            try
            {
                var persons = await _dbService.GetPersonsForDropdownAsync();

                Customers.Clear();
                Contractors.Clear();

                foreach (var person in persons)
                {
                    Customers.Add(person);
                    Contractors.Add(person);
                }

                var units = await _dbService.GetUnitsOfMeasurementAsync();
                Units.Clear();
                foreach (var unit in units)
                {
                    Units.Add(unit);
                }

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки справочников: {ex.Message}";
                HasErrors = true;
            }
        }
        private async Task LoadDataAsync()
        {
            try
            {
                var contracts = await _dbService.GetContractsAsync();
                Contracts.Clear();

                foreach (var contract in contracts)
                {
                    Contracts.Add(contract);
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
            if (Contracts == null || !Contracts.Any())
            {
                FilteredContracts.Clear();
                return;
            }

            IEnumerable<Contract> filtered = Contracts;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    (c.ContractsName != null && c.ContractsName.ToLower().Contains(searchLower)) ||
                    (c.CustomerName != null && c.CustomerName.ToLower().Contains(searchLower)) ||
                    (c.ContractorName != null && c.ContractorName.ToLower().Contains(searchLower)));
            }

            FilteredContracts.Clear();
            foreach (var contract in filtered.ToList())
            {
                FilteredContracts.Add(contract);
            }
        }
    }
}