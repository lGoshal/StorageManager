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
    /// Логика взаимодействия для PartnersViewModel.cs
    /// </summary>
    public class PartnersViewModel : BaseViewModel
    {
        /// <summary>
        /// Контекст/Коллекции/Свойства/Команды
        /// </summary>
        private readonly DatabaseService _dbService;

        private ObservableCollection<Partner> _partners;
        private ObservableCollection<Partner> _filteredPartners;

        private Partner _currentPartner;
        private Partner _selectedPartner;

        private string _searchText;
        private string _errorMessage;

        private bool _isEditing;
        private bool _hasErrors;

        public ObservableCollection<Partner> Partners
        {
            get => _partners;
            set => SetField(ref _partners, value);
        }
        public ObservableCollection<Partner> FilteredPartners
        {
            get => _filteredPartners;
            set => SetField(ref _filteredPartners, value);
        }
        public Partner CurrentPartner
        {
            get => _currentPartner;
            set => SetField(ref _currentPartner, value);
        }
        public Partner SelectedPartner
        {
            get => _selectedPartner;
            set
            {
                if (SetField(ref _selectedPartner, value) && value != null)
                {
                    EditPartner(value);
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

        public string FormTitle => IsEditing ? "Редактирование контрагента" : "Новый контрагент";
        public string SaveButtonText => IsEditing ? "Сохранить изменения" : "Создать контрагента";

        public ICommand LoadPartnersCommand { get; }
        public ICommand AddPartnerCommand { get; }
        public ICommand EditPartnerCommand { get; }
        public ICommand DeletePartnerCommand { get; }
        public ICommand SavePartnerCommand { get; }
        public ICommand CancelCommand { get; }

        public PartnersViewModel(string connectionString)
        {
            _dbService = new DatabaseService(connectionString);

            Partners = new ObservableCollection<Partner>();
            FilteredPartners = new ObservableCollection<Partner>();

            LoadPartnersCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddPartnerCommand = new RelayCommand(_ => AddNewPartner());
            EditPartnerCommand = new RelayCommand(EditPartner);
            DeletePartnerCommand = new RelayCommand(async p => await DeletePartnerAsync(p as Partner));
            SavePartnerCommand = new RelayCommand(async _ => await SavePartnerAsync());
            CancelCommand = new RelayCommand(_ => CancelEditing());

            CurrentPartner = new Partner();

            LoadPartnersCommand.Execute(null);
        }

        /// <summary>
        /// CRUD - операции
        /// </summary>
        private void AddNewPartner()
        {
            CurrentPartner = new Partner();
            IsEditing = true;
            HasErrors = false;
        }
        private void EditPartner(object parameter)
        {
            if (parameter is Partner partner)
            {
                CurrentPartner = new Partner
                {
                    PartnerId = partner.PartnerId,
                    PartnerName = partner.PartnerName,
                    FullName = partner.FullName,
                    INN = partner.INN,
                    KPP = partner.KPP,
                    Phone = partner.Phone,
                    Email = partner.Email,
                    AddressId = partner.AddressId,
                    Address = partner.Address
                };

                IsEditing = true;
                HasErrors = false;
            }
        }
        private async Task SavePartnerAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentPartner.PartnerName))
            {
                ErrorMessage = "Название контрагента обязательно для заполнения";
                HasErrors = true;
                return;
            }

            if (!string.IsNullOrEmpty(CurrentPartner.INN) && CurrentPartner.INN.Length != 10 && CurrentPartner.INN.Length != 12)
            {
                ErrorMessage = "ИНН должен содержать 10 или 12 цифр";
                HasErrors = true;
                return;
            }

            if (!string.IsNullOrEmpty(CurrentPartner.KPP) && CurrentPartner.KPP.Length != 9)
            {
                ErrorMessage = "КПП должен содержать 9 цифр";
                HasErrors = true;
                return;
            }

            if (!string.IsNullOrEmpty(CurrentPartner.Email) && !IsValidEmail(CurrentPartner.Email))
            {
                ErrorMessage = "Некорректный формат email";
                HasErrors = true;
                return;
            }

            try
            {
                if (IsEditing && CurrentPartner.PartnerId > 0)
                {
                    bool success = await _dbService.UpdatePartnerAsync(CurrentPartner);

                    if (success)
                    {
                        MessageBox.Show("Контрагент успешно обновлен", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка обновления контрагента";
                        HasErrors = true;
                        return;
                    }
                }
                else
                {
                    int newId = await _dbService.AddPartnerAsync(CurrentPartner);

                    if (newId > 0)
                    {
                        MessageBox.Show($"Контрагент успешно создан. ID: {newId}", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ErrorMessage = "Ошибка создания контрагента";
                        HasErrors = true;
                        return;
                    }
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
        private async Task DeletePartnerAsync(Partner partner)
        {
            if (partner == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить контрагента '{partner.PartnerName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _dbService.DeletePartnerAsync(partner.PartnerId);

                    if (success)
                    {
                        MessageBox.Show("Контрагент успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadDataAsync();
                    }
                    else
                    {
                        ErrorMessage = "Ошибка удаления контрагента";
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
        private void CancelEditing()
        {
            CurrentPartner = new Partner();
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
                var partners = await _dbService.GetSuppliersAsync();
                Partners.Clear();

                foreach (var partner in partners)
                {
                    Partners.Add(partner);
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
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private void ApplyFilters()
        {
            if (Partners == null || !Partners.Any())
            {
                FilteredPartners.Clear();
                return;
            }

            IEnumerable<Partner> filtered = Partners;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.PartnerName != null && p.PartnerName.ToLower().Contains(searchLower)) ||
                    (p.FullName != null && p.FullName.ToLower().Contains(searchLower)) ||
                    (p.INN != null && p.INN.Contains(searchLower)) ||
                    (p.Email != null && p.Email.ToLower().Contains(searchLower)));
            }
            FilteredPartners.Clear();
            foreach (var partner in filtered.ToList())
            {
                FilteredPartners.Add(partner);
            }
        }
    }
}