using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для Storage.cs
    /// </summary>
    public class Storage
    {
        public int StorageId { get; set; }
        public string StorageName { get; set; }
        public int? StorageAddressId { get; set; }

        /// <summary>
        /// Свойства
        /// </summary>
        public string FullAddress { get; set; }
        public string TempAddress { get; set; }
    }
}
