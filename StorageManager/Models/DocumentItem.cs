using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    public class DocumentItem : INotifyPropertyChanged
    {
        private int _productId;
        private string _productName;
        private decimal _quantity;
        private int? _unitOfMeasurementId;
        private string _unitOfMeasurementName;
        private int? _characteristicId;
        private string _characteristicName;

        public int ItemId { get; set; }

        public int ProductId
        {
            get => _productId;
            set
            {
                _productId = value;
                OnPropertyChanged(nameof(ProductId));
            }
        }

        public string ProductName
        {
            get => _productName;
            set
            {
                _productName = value;
                OnPropertyChanged(nameof(ProductName));
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public int? UnitOfMeasurementId
        {
            get => _unitOfMeasurementId;
            set
            {
                _unitOfMeasurementId = value;
                OnPropertyChanged(nameof(UnitOfMeasurementId));
            }
        }

        public string UnitOfMeasurementName
        {
            get => _unitOfMeasurementName;
            set
            {
                _unitOfMeasurementName = value;
                OnPropertyChanged(nameof(UnitOfMeasurementName));
            }
        }

        public int? CharacteristicId
        {
            get => _characteristicId;
            set
            {
                _characteristicId = value;
                OnPropertyChanged(nameof(CharacteristicId));
            }
        }

        public string CharacteristicName
        {
            get => _characteristicName;
            set
            {
                _characteristicName = value;
                OnPropertyChanged(nameof(CharacteristicName));
            }
        }

        public decimal? RemainingQuantity { get; set; }

        // Для ComboBox в DataGrid
        public Product SelectedProduct { get; set; }
        public UnitOfMeasurement SelectedUnit { get; set; }
        public Characteristic SelectedCharacteristic { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
