using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для Contract.cs
    /// </summary>
    public class Contract
    {
        public int ContractsID { get; set; }
        public string ContractsName { get; set; }
        public string ContractsObject { get; set; }
        public int ContractsCustomerID { get; set; }
        public int ContractsContractorID { get; set; }
        public decimal ContractsValue { get; set; }
        public decimal ContractsTimeOfAction { get; set; }
        public int ContractsExpirationDateUnitID { get; set; }
        
        /// <summary>
        /// Свойства
        /// </summary>
        public string CustomerName { get; set; }
        public string ContractorName { get; set; }
        public string ExpirationDateUnitName { get; set; }

        public override string ToString()
        {
            return ContractsName;
        }
    }
}
