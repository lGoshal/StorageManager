using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для DocumentData.cs
    /// </summary>
    public class DocumentData
    {
        public DateTime DocumentDate { get; set; }
        public int ResponsibleId { get; set; }
        public string ResponsibleName { get; set; } = string.Empty;
        public int? StorageId { get; set; }
        public int? SupplierId { get; set; }
        public int? SenderStorageId { get; set; }
        public int? RecipientStorageId { get; set; }
        public string LocationInfo { get; set; } = string.Empty;
    }
}
