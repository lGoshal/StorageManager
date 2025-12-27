using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для Product.cs
    /// </summary>
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public int? ProductCharacteristicId { get; set; }
        public int? ProductUnitOfMeasurementId { get; set; }
        public decimal ProductQuantity { get; set; }
        public decimal? ProductExpirationDate { get; set; }
        public int? ProductExpirationDateUnitId { get; set; }
        public int? ProductTypeId { get; set; }

        /// <summary>
        /// Свойства
        /// </summary>
        public string ProductTypeName { get; set; }
        public string UnitOfMeasurementName { get; set; }
        public string CharacteristicName { get; set; }
        public string ExpirationDateUnitName { get; set; }

        public ProductType SelectedProductType { get; set; }
        public UnitOfMeasurement SelectedUnit { get; set; }
        public Characteristic SelectedCharacteristic { get; set; }
    }
}