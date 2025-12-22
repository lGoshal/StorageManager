using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StorageManager.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Login { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; }
    }
}