using System.Windows;
using System.Windows.Controls;
using StorageManager.ViewModels;
using StorageManager.Views;

namespace StorageManager
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private string connectionString = @"Server=GOSHA\SQLEXPRESS;Database=StorageManagement;Integrated Security=True;TrustServerCertificate=True;";

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация ViewModel
            _viewModel = new MainViewModel(connectionString);
            DataContext = _viewModel;

            // Загружаем панель управления по умолчанию
            ShowDashboard();

            // Показываем окно "О программе" после загрузки
            this.ContentRendered += MainWindow_ContentRendered;
        }

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

        // Навигация
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string pageTitleText = button.Content.ToString().Substring(2);
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
                case "btnCounterparties":
                    ShowCounterparties();
                    break;
                case "btnContracts":
                    ShowContracts();
                    break;
                case "btnEmployees":
                    ShowEmployees();
                    break;
            }
        }

        private void ShowDashboard()
        {
            // Показываем дашборд
            var dashboardView = new DashboardView();
            dashboardView.DataContext = new DashboardViewModel(connectionString);
            MainFrame.Content = dashboardView;

            // Показываем быстрые действия
            QuickActionsPanel.Visibility = Visibility.Visible;
            UpdatePageState("Dashboard");
        }

        private void ShowProducts()
        {
            // Показываем форму товаров
            var productsView = new ProductsView();
            productsView.DataContext = new ProductsViewModel(connectionString);
            MainFrame.Content = productsView;

            // Скрываем быстрые действия
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowDocuments()
        {
            var documentsView = new DocumentsView();
            documentsView.DataContext = new DocumentsViewModel(connectionString, this); // Передаем this как owner
            MainFrame.Content = documentsView;
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Documents");
            pageTitle.Text = "Все документы";
        }

        private void ShowCounterparties()
        {
            // TODO: Реализовать позже
            MainFrame.Content = new TextBlock
            {
                Text = "Форма контрагентов будет реализована позже",
                Style = (Style)FindResource("HeaderStyle"),
                Margin = new Thickness(20)
            };
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowContracts()
        {
            // TODO: Реализовать позже
            MainFrame.Content = new TextBlock
            {
                Text = "Форма договоров будет реализована позже",
                Style = (Style)FindResource("HeaderStyle"),
                Margin = new Thickness(20)
            };
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowEmployees()
        {
            // TODO: Реализовать позже
            MainFrame.Content = new TextBlock
            {
                Text = "Форма сотрудников будет реализована позже",
                Style = (Style)FindResource("HeaderStyle"),
                Margin = new Thickness(20)
            };
            QuickActionsPanel.Visibility = Visibility.Collapsed;
        }

        // Быстрые действия
        private void BtnNewProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowProducts(); // Переходим к товарам
        }

        private void BtnNewDocument_Click(object sender, RoutedEventArgs e)
        {
            // Показываем окно выбора типа документа
            var selectionWindow = new DocumentTypeSelectionWindow();
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // Получаем выбранный тип документа
                string documentType = selectionWindow.SelectedDocumentType;

                // Создаем ViewModel и View для документа
                var documentViewModel = new DocumentViewModel(connectionString, documentType);
                var documentView = new DocumentView
                {
                    DataContext = documentViewModel
                };

                // Отображаем форму документа
                MainFrame.Content = documentView;
                QuickActionsPanel.Visibility = Visibility.Collapsed;
                UpdatePageState("Document");
                pageTitle.Text = "Создание документа";
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Обновляем текущую страницу
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
            // TODO: Реализовать форму видов товаров
            MainFrame.Content = new TextBlock
            {
                Text = "Форма видов товаров будет реализована позже",
                Style = (Style)FindResource("HeaderStyle"),
                Margin = new Thickness(20)
            };
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("ProductTypes");
        }

        private void ShowUsers()
        {
            // TODO: Реализовать форму пользователей
            MainFrame.Content = new TextBlock
            {
                Text = "Форма пользователей будет реализована позже",
                Style = (Style)FindResource("HeaderStyle"),
                Margin = new Thickness(20)
            };
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Users");
        }

        private void ShowAddresses()
        {
            // TODO: Реализовать форму адресов
            MainFrame.Content = new TextBlock
            {
                Text = "Форма адресов будет реализована позже",
                Style = (Style)FindResource("HeaderStyle"),
                Margin = new Thickness(20)
            };
            QuickActionsPanel.Visibility = Visibility.Collapsed;
            UpdatePageState("Addresses");
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