using System;
using System.Collections.Generic;
using System.Linq;
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

namespace StorageManager.Views
{
    /// <summary>
    /// Логика взаимодействия для DocumentTypeSelectionWindow.xaml
    /// </summary>
    public partial class DocumentTypeSelectionWindow : Window
    {
        public string SelectedDocumentType { get; private set; }

        public DocumentTypeSelectionWindow()
        {
            InitializeComponent();
        }

        private void DocumentTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                SelectedDocumentType = button.Tag.ToString();
                this.DialogResult = true;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
