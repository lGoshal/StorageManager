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
    /// Логика взаимодействия для ContractsView.xaml
    /// </summary>
    public partial class ContractsView : UserControl
    {
        public ContractsView()
        {
            InitializeComponent();
        }
    /// <summary>
    /// Контроль за введенной строкой
    /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}
