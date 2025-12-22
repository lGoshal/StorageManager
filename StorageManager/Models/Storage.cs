using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    public class Storage
    {
        public int StorageId { get; set; }
        public string StorageName { get; set; }
        public int? StorageAddressId { get; set; }

        // Для отображения
        public string FullAddress { get; set; }

        // Для редактирования
        public string TempAddress { get; set; }
    }
}
