using StorageManager.ViewModels;
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
    /// Логика взаимодействия для StorageView.xaml
    /// </summary>
    public partial class StorageView : UserControl
    {
        public StorageView()
        {
            InitializeComponent();
        }
        public StorageViewModel ViewModel
        {
            get => (StorageViewModel)DataContext;
            set => DataContext = value;
        }
    }
}
