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
    /// Логика взаимодействия для AddressesView.xaml
    /// </summary>
    public partial class AddressesView : UserControl
    {
        public AddressesView()
        {
            InitializeComponent();
        }
        private async void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is AddressesViewModel viewModel)
            {
                await viewModel.LoadCitiesForSelectedCountry();
            }
        }
    }
}
