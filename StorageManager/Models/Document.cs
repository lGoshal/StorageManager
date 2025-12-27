using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace StorageManager.Models
{
    /// <summary>
    /// Логика взаимодействия для Document.cs
    /// </summary>
    public class Document : INotifyPropertyChanged
    {
        private string _documentType;
        private DateTime _documentDate;
        private string _documentNumber;
        private int _responsibleId;
        private string _responsibleName;
        private int? _storageId;
        private string _storageName;
        private int? _supplierId;
        private string _supplierName;
        private int? _senderStorageId;
        private string _senderStorageName;
        private int? _recipientStorageId;
        private string _recipientStorageName;
        private string _status;
        private string _locationInfo;

        /// <summary>
        /// Свойства/События
        /// </summary>
        public int DocumentId { get; set; }

        public string DocumentType
        {
            get => _documentType;
            set
            {
                _documentType = value;
                OnPropertyChanged(nameof(DocumentType));
            }
        }
        public DateTime DocumentDate
        {
            get => _documentDate;
            set
            {
                _documentDate = value;
                OnPropertyChanged(nameof(DocumentDate));
            }
        }
        public string DocumentNumber
        {
            get => _documentNumber;
            set
            {
                _documentNumber = value;
                OnPropertyChanged(nameof(DocumentNumber));
            }
        }
        public int ResponsibleId
        {
            get => _responsibleId;
            set
            {
                _responsibleId = value;
                OnPropertyChanged(nameof(ResponsibleId));
            }
        }
        public string ResponsibleName
        {
            get => _responsibleName;
            set
            {
                _responsibleName = value;
                OnPropertyChanged(nameof(ResponsibleName));
            }
        }
        public int? StorageId
        {
            get => _storageId;
            set
            {
                _storageId = value;
                OnPropertyChanged(nameof(StorageId));
            }
        }
        public string StorageName
        {
            get => _storageName;
            set
            {
                _storageName = value;
                OnPropertyChanged(nameof(StorageName));
            }
        }
        public int? SupplierId
        {
            get => _supplierId;
            set
            {
                _supplierId = value;
                OnPropertyChanged(nameof(SupplierId));
            }
        }
        public string SupplierName
        {
            get => _supplierName;
            set
            {
                _supplierName = value;
                OnPropertyChanged(nameof(SupplierName));
            }
        }
        public int? SenderStorageId
        {
            get => _senderStorageId;
            set
            {
                _senderStorageId = value;
                OnPropertyChanged(nameof(SenderStorageId));
            }
        }
        public string SenderStorageName
        {
            get => _senderStorageName;
            set
            {
                _senderStorageName = value;
                OnPropertyChanged(nameof(SenderStorageName));
            }
        }
        public int? RecipientStorageId
        {
            get => _recipientStorageId;
            set
            {
                _recipientStorageId = value;
                OnPropertyChanged(nameof(RecipientStorageId));
            }
        }
        public string RecipientStorageName
        {
            get => _recipientStorageName;
            set
            {
                _recipientStorageName = value;
                OnPropertyChanged(nameof(RecipientStorageName));
            }
        }
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        public string LocationInfo
        {
            get
            {
                if (!string.IsNullOrEmpty(_locationInfo))
                    return _locationInfo;

                if (!string.IsNullOrEmpty(StorageName))
                    return StorageName;

                if (!string.IsNullOrEmpty(SenderStorageName) && !string.IsNullOrEmpty(RecipientStorageName))
                    return $"{SenderStorageName} → {RecipientStorageName}";

                if (!string.IsNullOrEmpty(SenderStorageName))
                    return SenderStorageName;

                if (!string.IsNullOrEmpty(RecipientStorageName))
                    return RecipientStorageName;

                return "Не указано";
            }
            set
            {
                _locationInfo = value;
                OnPropertyChanged(nameof(LocationInfo));
            }
        }

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

        public ObservableCollection<DocumentItem> Items { get; set; }
        public Document()
        {
            Items = new ObservableCollection<DocumentItem>();
            DocumentDate = DateTime.Now;
            Status = "Черновик";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}