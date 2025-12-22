using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using StorageManager.ViewModels;
using StorageManager.Views;

namespace StorageManager.Views
{
    /// <summary>
    /// Логика взаимодействия для EditDocumentWindow.xaml
    /// </summary>
    public partial class EditDocumentWindow : Window
    {
        public EditDocumentWindow(DocumentListItem documentItem, string connectionString)
        {
            InitializeComponent();

            // Создаем ViewModel для редактирования
            var viewModel = new DocumentEditViewModel(connectionString, documentItem);
            DocumentViewControl.DataContext = viewModel;

            // Подписываемся на событие закрытия
            viewModel.DocumentSaved += (s, e) => this.DialogResult = true;
            viewModel.DocumentCanceled += (s, e) => this.DialogResult = false;
        }
    }
}
