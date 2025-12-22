using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public string AddressView { get; set; }
        public int? CountryId { get; set; }
        public int? RegionId { get; set; }
        public int? CityId { get; set; }
        public int? LocalityId { get; set; }
        public int? StreetId { get; set; }
        public string HouseNumber { get; set; }
        public string EntranceNumber { get; set; }

        // Навигационные свойства
        public string CountryName { get; set; }
        public string CityName { get; set; }
        public string StreetName { get; set; }
    }
}
