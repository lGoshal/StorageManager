using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для ProductType.cs
    /// </summary>
    public class ProductType
    {
        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; }

        /// <summary>
        /// Свойство
        /// </summary>
        public override string ToString()
        {
            return ProductTypeName;
        }
    }
}
