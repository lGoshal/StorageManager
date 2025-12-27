using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using StorageManager.Models;
using StorageManager.Services;
using System.Windows.Controls;

namespace StorageManager.ViewModels
{
    /// <summary>
    /// Логика взаимодействия для AddressesViewModel.cs
    /// </summary>
    public class AddressesViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<Address> _addresses;
        private ObservableCollection<Address> _filteredAddresses;
        private ObservableCollection<Country> _countries;
        private ObservableCollection<City> _cities;

        private Address _currentAddress;
        private Address _selectedAddress;
        private string _searchText;
        private string _errorMessage;
        private bool _isEditing;
        private bool _hasErrors;

        public ObservableCollection<Address> Addresses
        {
            get => _addresses;
            set => SetField(ref _addresses, value);
        }
        public ObservableCollection<Address> FilteredAddresses
        {
            get => _filteredAddresses;
            set => SetField(ref _filteredAddresses, value);
        }
        public ObservableCollection<Country> Countries
        {
            get => _countries;
            set => SetField(ref _countries, value);
        }
        public ObservableCollection<City> Cities
        {
            get => _cities;
            set => SetField(ref _cities, value);
        }
        public Address CurrentAddress
        {
            get => _currentAddress;
            set => SetField(ref _currentAddress, value);
        }
        public Address SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                if (SetField(ref _selectedAddress, value) && value != null)
                {
                    EditAddress(value);
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

        public string FormTitle => IsEditing ? "Редактирование адреса" : "Новый адрес";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать адрес";

        public ICommand LoadAddressesCommand { get; }
        public ICommand LoadReferenceDataCommand { get; }
        public ICommand AddAddressCommand { get; }
        public ICommand EditAddressCommand { get; }
        public ICommand DeleteAddressCommand { get; }
        public ICommand SaveAddressCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CountryChangedCommand { get; }

        public AddressesViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Addresses = new ObservableCollection<Address>();
            FilteredAddresses = new ObservableCollection<Address>();
            Countries = new ObservableCollection<Country>();
            Cities = new ObservableCollection<City>();

            LoadAddressesCommand = new RelayCommand(async _ => await LoadAddressesAsync());
            LoadReferenceDataCommand = new RelayCommand(async _ => await LoadReferenceDataAsync());
            AddAddressCommand = new RelayCommand(_ => AddNewAddress());
            EditAddressCommand = new RelayCommand(EditAddress);
            DeleteAddressCommand = new RelayCommand(async p => await DeleteAddressAsync(p as Address));
            SaveAddressCommand = new RelayCommand(async _ => await SaveAddressAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());
            CountryChangedCommand = new RelayCommand(async _ => await LoadCitiesForSelectedCountry());

            CurrentAddress = new Address();

            LoadAddressesCommand.Execute(null);
            LoadReferenceDataCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewAddress()
        {
            CurrentAddress = new Address();
            IsEditing = true;
            HasErrors = false;
        }
        private void EditAddress(object parameter)
        {
            if (parameter is Address address)
            {
                CurrentAddress = new Address
                {
                    AddressId = address.AddressId,
                    AddressView = address.AddressView,
                    CountryId = address.CountryId,
                    RegionId = address.RegionId,
                    CityId = address.CityId,
                    LocalityId = address.LocalityId,
                    StreetId = address.StreetId,
                    HouseNumber = address.HouseNumber,
                    EntranceNumber = address.EntranceNumber,
                    CountryName = address.CountryName,
                    CityName = address.CityName,
                    StreetName = address.StreetName
                };

                IsEditing = true;
                HasErrors = false;

                if (CurrentAddress.CountryId.HasValue)
                {
                    LoadCitiesForSelectedCountry();
                }
            }
        }
        private async Task DeleteAddressAsync(Address address)
        {
            if (address == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить адрес '{address.AddressView}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _dbService.DeleteAddressAsync(address.AddressId);

                    if (success)
                    {
                        MessageBox.Show("Адрес успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadAddressesAsync();
                    }
                    else
                    {
                        ErrorMessage = "Ошибка удаления адреса";
                        HasErrors = true;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка удаления: {ex.Message}";
                    HasErrors = true;
                }
            }
        }
        private async Task SaveAddressAsync()
        {
            if (CurrentAddress.CountryId == null)
            {
                ErrorMessage = "Выберите страну";
                HasErrors = true;
                return;
            }

            if (CurrentAddress.CityId == null)
            {
                ErrorMessage = "Выберите город";
                HasErrors = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentAddress.HouseNumber))
            {
                ErrorMessage = "Номер дома обязателен для заполнения";
                HasErrors = true;
                return;
            }

            try
            {
                var selectedCountry = Countries.FirstOrDefault(c => c.CountryId == CurrentAddress.CountryId);
                var selectedCity = Cities.FirstOrDefault(c => c.CityId == CurrentAddress.CityId);

                if (selectedCountry != null)
                    CurrentAddress.CountryName = selectedCountry.CountryName;
                if (selectedCity != null)
                    CurrentAddress.CityName = selectedCity.CityName;

                if (IsEditing && CurrentAddress.AddressId > 0)
                {
                    bool success = await _dbService.UpdateAddressAsync(CurrentAddress);

                    if (success)
                    {
                        MessageBox.Show("Адрес успешно обновлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка обновления адреса";
                        HasErrors = true;
                        return;
                    }
                }
                else
                {
                    int newId = await _dbService.AddAddressAsync(CurrentAddress);

                    if (newId > 0)
                    {
                        MessageBox.Show($"Адрес успешно создан. ID: {newId}", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка создания адреса";
                        HasErrors = true;
                        return;
                    }
                }

                await LoadAddressesAsync();

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
            CurrentAddress = new Address();
            IsEditing = false;
            HasErrors = false;
        }
       
        /// <summary>
        /// Служебные методы
        /// </summary>
        private async Task LoadAddressesAsync()
        {
            try
            {
                var addresses = await _dbService.GetAddressesAsync();
                Addresses.Clear();

                foreach (var address in addresses)
                {
                    Addresses.Add(address);
                }

                ApplyFilters();

                CancelEditing();

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки адресов: {ex.Message}";
                HasErrors = true;
            }
        }
        private async Task LoadReferenceDataAsync()
        {
            try
            {
                var countries = await _dbService.GetCountriesAsync();
                Countries.Clear();

                foreach (var country in countries)
                {
                    Countries.Add(country);
                }

                var cities = await _dbService.GetCitiesAsync();
                Cities.Clear();

                foreach (var city in cities)
                {
                    Cities.Add(city);
                }

                HasErrors = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка загрузки справочников: {ex.Message}";
                HasErrors = true;
            }
        }
        public async Task LoadCitiesForSelectedCountry()
        {
            try
            {
                if (CurrentAddress.CountryId.HasValue)
                {
                    var cities = await _dbService.GetCitiesAsync(CurrentAddress.CountryId.Value);
                    Cities.Clear();

                    foreach (var city in cities)
                    {
                        Cities.Add(city);
                    }

                    CurrentAddress.CityId = null;
                }
                else
                {
                    var cities = await _dbService.GetCitiesAsync();
                    Cities.Clear();

                    foreach (var city in cities)
                    {
                        Cities.Add(city);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки городов: {ex.Message}");
            }
        }
        private void ApplyFilters()
        {
            if (Addresses == null || !Addresses.Any())
            {
                FilteredAddresses.Clear();
                return;
            }

            IEnumerable<Address> filtered = Addresses;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(a =>
                    (a.AddressView != null && a.AddressView.ToLower().Contains(searchLower)) ||
                    (a.CountryName != null && a.CountryName.ToLower().Contains(searchLower)) ||
                    (a.CityName != null && a.CityName.ToLower().Contains(searchLower)) ||
                    (a.StreetName != null && a.StreetName.ToLower().Contains(searchLower)) ||
                    (a.HouseNumber != null && a.HouseNumber.ToLower().Contains(searchLower)));
            }

            FilteredAddresses.Clear();
            foreach (var address in filtered.ToList())
            {
                FilteredAddresses.Add(address);
            }
        }
    }
}