using System.Windows;
using System.Windows.Controls;
using StorageManager.ViewModels;
using StorageManager.Views;

namespace StorageManager.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private string connectionString = @"Server=GOSHA\SQLEXPRESS;Database=StorageManagement;Integrated Security=True;TrustServerCertificate=True;";

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel(connectionString);
            DataContext = _viewModel;

            ShowDashboard();

            this.ContentRendered += MainWindow_ContentRendered;
        }
        /// <summary>
        /// Обновление основного окна и вывод окна "О программе"
        /// </summary>
        private void MainWindow_ContentRendered(object sender, System.EventArgs e)
        {
            this.ContentRendered -= MainWindow_ContentRendered;
            ShowAboutDialog();
        }
        private void ShowAboutDialog()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// Реализация навигации, обработка нажатий на ссылки
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string buttonContent = button.Content.ToString();
            string pageTitleText = buttonContent.Length > 2 ? buttonContent.Substring(2).Trim() : buttonContent;
            pageTitle.Text = pageTitleText;

            switch (button.Name)
            {
                case "btnDashboard":
                    ShowDashboard();
                    break;
                case "btnProducts":
                    ShowProducts();
                    break;
                case "btnWarehouses":
                    ShowWarehouses();
                    break;
                case "btnDocuments":
                    ShowDocuments();
                    break;
                case "btnProductTypes":
                    ShowProductTypes();
                    break;
                case "btnCounterparties":
                    ShowPartners();
                    break;
                case "btnContracts":
                    ShowContracts();
                    break;
                case "btnUsers":
                    ShowUsers();
                    break;
                case "btnAddresses":
                    ShowAddresses();
                    break;
                case "btnUnits":
                    ShowUnits();
                    break;
                case "btnCharacteristics":
                    ShowCharacteristics();
                    break;
                default:
                    MessageBox.Show($"Раздел '{buttonContent}' находится в разработке",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }
        
        /// <summary>
        /// Обработчики быстрых действий, кнопки "О программе"
        /// </summary>
        private void BtnNewProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowProducts();
        }
        private void BtnNewDocument_Click(object sender, RoutedEventArgs e)
        {
            var selectionWindow = new DocumentTypeSelectionWindow();
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                string documentType = selectionWindow.SelectedDocumentType;

                var documentViewModel = new DocumentViewModel(connectionString, documentType);
                var documentView = new DocumentView
                {
                    DataContext = documentViewModel
                };

                MainFrame.Content = documentView;
                QuickActionsPanel.Visibility = Visibility.Collapsed;
                UpdatePageState("Document");
                pageTitle.Text = "Создание документа";
            }
        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content is DashboardView dashboardView)
            {
                if (dashboardView.DataContext is DashboardViewModel viewModel)
                {
                    viewModel.LoadDashboardDataAsync();
                }
            }
        }
        private void BtnNotifications_Click(object sender, RoutedEventArgs e)
        {
            ShowAboutDialog();
        }

        /// <summary>
        /// Методы открытия форм из навигации
        /// </summary>
        private void ShowDashboard()
        {
            var dashboardView = new DashboardView();
            dashboardView.DataContext = new DashboardViewModel(connectionString);
            MainFrame.Content = dashboardView;

            QuickActionsPanel.Visibility = Visibility.Visible;
            UpdatePageState("Dashboard");
        }
        private void ShowProducts()
        {
            var productsView = new ProductsView();
            productsView.DataContext = new ProductsViewModel(connectionString);
            MainFrame.Content = productsView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }
        private void ShowDocuments()
        {
            var documentsView = new DocumentsView();
            documentsView.DataContext = new DocumentsViewModel(connectionString, this);
            MainFrame.Content = documentsView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Documents");
            pageTitle.Text = "Все документы";
        }
        private void ShowContracts()
        {
            pageTitle.Text = "Договоры";
            var contractsView = new ContractsView();
            contractsView.DataContext = new ContractsViewModel(connectionString);
            MainFrame.Content = contractsView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Contracts");
        }
        private void ShowWarehouses()
        {
            var storageView = new StorageView();
            storageView.DataContext = new StorageViewModel(connectionString);
            MainFrame.Content = storageView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Warehouses");
        }
        private void ShowProductTypes()
        {
            pageTitle.Text = "Виды товаров";
            var productTypesView = new ProductTypesView();
            productTypesView.DataContext = new ProductTypesViewModel(connectionString);
            MainFrame.Content = productTypesView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("ProductTypes");
        }
        private void ShowUnits()
        {
            pageTitle.Text = "Единицы измерения";
            var unitsView = new UnitsView();
            unitsView.DataContext = new UnitsViewModel(connectionString);
            MainFrame.Content = unitsView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Units");
        }
        private void ShowCharacteristics()
        {
            pageTitle.Text = "Характеристики";
            var characteristicsView = new CharacteristicsView();
            characteristicsView.DataContext = new CharacteristicsViewModel(connectionString);
            MainFrame.Content = characteristicsView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }
        private void ShowUsers()
        {
            pageTitle.Text = "Пользователи";
            var usersView = new UsersView();
            usersView.DataContext = new UsersViewModel(connectionString);
            MainFrame.Content = usersView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }
        private void ShowAddresses()
        {
            pageTitle.Text = "Адреса";
            var addressesView = new AddressesView();
            addressesView.DataContext = new AddressesViewModel(connectionString);
            MainFrame.Content = addressesView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }
        private void ShowPartners()
        {
            pageTitle.Text = "Контрагенты";
            var partnersView = new PartnersView();
            partnersView.DataContext = new PartnersViewModel(connectionString);
            MainFrame.Content = partnersView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdatePageState(string pageName)
        {
            if (_viewModel != null)
            {
                _viewModel.IsDashboard = (pageName == "Dashboard");
            }
        }
    }
}