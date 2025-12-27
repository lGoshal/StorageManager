using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageManager
{
    /// <summary>
    /// Логика взаимодействия для RecentDocument.cs
    /// </summary>
    public class RecentDocument
    {
        public string DocumentType { get; set; }
        public DateTime DocumentDate { get; set; }
        public string DocumentNumber { get; set; }
        public string Responsible { get; set; }
        public string Location { get; set; }
        public string Counterparty { get; set; }
        public string Status { get; set; }
        public int DocumentID { get; set; }
        
        /// <summary>
        /// Служебные
        /// </summary>
        public System.Windows.Media.Brush StatusColor
        {
            get
            {
                return Status switch
                {
                    "Завершен" or "Принят" or "Выполнено" or "Проведена"
                        => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),
                    "Подтверждено"
                        => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 162, 184)),
                    "В работе" or "Ожидание"
                        => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)),
                    "Отменен" or "Ошибка"
                        => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 117, 125))
                };
            }
        }
        public string FormattedDate => DocumentDate.ToString("dd.MM.yyyy HH:mm");
    }
}
