using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    public class Partner
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; }
        public string FullName { get; set; }
        public string INN { get; set; }
        public string KPP { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int? AddressId { get; set; }
        public string Address { get; set; }
    }
}