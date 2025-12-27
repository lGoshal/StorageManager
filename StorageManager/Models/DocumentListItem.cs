using System;
using System.Windows.Media;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для DocumentListItem.cs
    /// </summary>
    public class DocumentListItem
    {
        public string DocumentType { get; set; }
        public DateTime DocumentDate { get; set; }
        public string DocumentNumber { get; set; }
        public string ResponsibleName { get; set; }
        public string LocationInfo { get; set; }
        public string Status { get; set; }
        public int DocumentId { get; set; }
        public string TableName { get; set; }
        public int ItemsCount { get; set; }

        /// <summary>
        /// Свойства
        /// </summary>
        public Brush StatusBackground
        {
            get
            {
                return Status switch
                {
                    "Проведен" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    "Черновик" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    "Отменен" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    _ => new SolidColorBrush(Color.FromRgb(255, 193, 7))
                };
            }
        }
        public Brush StatusForeground => Brushes.White;
    }
}