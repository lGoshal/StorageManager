using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using StorageManager.Models;

namespace StorageManager.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        public string ConnectionString => _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Метод для получения товаров
        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    p.ProductID,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductQuantity,
                    p.ProductTypeID,
                    p.ProductUnitOfMeasurementID,
                    p.ProductCharacteristicID,
                    p.ProductExpirationDate,
                    p.ProductExpirationDateUnitID,
                    pt.ProductTypeName,
                    uom.UnitOfMeasurementName,
                    c.CharacteristicName,
                    edu.ExpirationDateUnitName
                FROM Product p
                LEFT JOIN ProductType pt ON p.ProductTypeID = pt.ProductTypeID
                LEFT JOIN UnitOfMeasurement uom ON p.ProductUnitOfMeasurementID = uom.UnitOfMeasurementID
                LEFT JOIN Characteristic c ON p.ProductCharacteristicID = c.CharacteristicID
                LEFT JOIN ExpirationDateUnit edu ON p.ProductExpirationDateUnitID = edu.ExpirationDateUnitID
                ORDER BY p.ProductName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var product = new Product
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                ProductDescription = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                ProductQuantity = reader.GetDecimal(3),
                                ProductTypeId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                ProductUnitOfMeasurementId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                ProductCharacteristicId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                ProductExpirationDate = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7),
                                ProductExpirationDateUnitId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                ProductTypeName = reader.IsDBNull(9) ? "Не указан" : reader.GetString(9),
                                UnitOfMeasurementName = reader.IsDBNull(10) ? "Не указан" : reader.GetString(10),
                                CharacteristicName = reader.IsDBNull(11) ? "Не указан" : reader.GetString(11),
                                ExpirationDateUnitName = reader.IsDBNull(12) ? "Не указан" : reader.GetString(12)
                            };

                            products.Add(product);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки товаров: {ex.Message}");
                throw;
            }

            return products;
        }

        public async Task<int> AddProductAsync(Product product)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                INSERT INTO Product (
                    ProductName, 
                    ProductDescription, 
                    ProductQuantity, 
                    ProductTypeID,
                    ProductUnitOfMeasurementID,
                    ProductCharacteristicID,
                    ProductExpirationDate,
                    ProductExpirationDateUnitID
                )
                VALUES (
                    @Name, 
                    @Description, 
                    @Quantity, 
                    @TypeId,
                    @UnitId,
                    @CharacteristicId,
                    @ExpirationDate,
                    @ExpirationDateUnitId
                );
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", product.ProductName);
                        cmd.Parameters.AddWithValue("@Description", product.ProductDescription ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Quantity", product.ProductQuantity);
                        cmd.Parameters.AddWithValue("@TypeId", product.ProductTypeId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UnitId", product.ProductUnitOfMeasurementId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CharacteristicId", product.ProductCharacteristicId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ExpirationDate", product.ProductExpirationDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ExpirationDateUnitId", product.ProductExpirationDateUnitId ?? (object)DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления товара: {ex.Message}");
                throw;
            }
        }

        // Метод для обновления товара
        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE Product 
                SET ProductName = @Name,
                    ProductDescription = @Description,
                    ProductQuantity = @Quantity,
                    ProductTypeID = @TypeId
                WHERE ProductID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", product.ProductId);
                        cmd.Parameters.AddWithValue("@Name", product.ProductName);
                        cmd.Parameters.AddWithValue("@Description", product.ProductDescription ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Quantity", product.ProductQuantity);
                        cmd.Parameters.AddWithValue("@TypeId", product.ProductTypeId);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления товара: {ex.Message}");
                throw;
            }
        }

        // Метод для удаления товара
        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM Product WHERE ProductID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления товара: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ProductType>> GetProductTypesAsync()
        {
            var types = new List<ProductType>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "SELECT ProductTypeID, ProductTypeName FROM ProductType ORDER BY ProductTypeName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            types.Add(new ProductType
                            {
                                ProductTypeId = reader.GetInt32(0),
                                ProductTypeName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки типов товаров: {ex.Message}");
                throw;
            }

            return types;
        }

        public async Task<List<UnitOfMeasurement>> GetUnitsOfMeasurementAsync()
        {
            var units = new List<UnitOfMeasurement>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "SELECT UnitOfMeasurementID, UnitOfMeasurementName FROM UnitOfMeasurement ORDER BY UnitOfMeasurementName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            units.Add(new UnitOfMeasurement
                            {
                                UnitOfMeasurementId = reader.GetInt32(0),
                                UnitOfMeasurementName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки единиц измерения: {ex.Message}");
                throw;
            }

            return units;
        }

        public async Task<List<Characteristic>> GetCharacteristicsAsync()
        {
            var characteristics = new List<Characteristic>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "SELECT CharacteristicID, CharacteristicName, CharacteristicDescription FROM Characteristic ORDER BY CharacteristicName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            characteristics.Add(new Characteristic
                            {
                                CharacteristicId = reader.GetInt32(0),
                                CharacteristicName = reader.GetString(1),
                                CharacteristicDescription = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки характеристик: {ex.Message}");
                throw;
            }

            return characteristics;
        }

        public async Task<List<Storage>> GetStoragesAsync()
        {
            var storages = new List<Storage>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                            SELECT 
                                s.StorageID,
                                s.StorageName,
                                s.StorageAdressID,
                                a.AdressView
                            FROM Storage s
                            LEFT JOIN Adress a ON s.StorageAdressID = a.AdressID
                            ORDER BY s.StorageName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            storages.Add(new Storage
                            {
                                StorageId = reader.GetInt32(0),
                                StorageName = reader.GetString(1),
                                StorageAddressId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                FullAddress = reader.IsDBNull(3) ? "Адрес не указан" : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки складов: {ex.Message}");
                throw;
            }

            return storages;
        }

        public async Task<int> AddStorageAsync(Storage storage)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Пока просто добавляем склад без адреса
                    var query = @"
                INSERT INTO Storage (StorageName, StorageAdressID)
                VALUES (@Name, NULL);
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", storage.StorageName);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления склада: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateStorageAsync(Storage storage)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE Storage 
                SET StorageName = @Name
                WHERE StorageID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", storage.StorageId);
                        cmd.Parameters.AddWithValue("@Name", storage.StorageName);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления склада: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteStorageAsync(int storageId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM Storage WHERE StorageID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", storageId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления склада: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ExpirationDateUnit>> GetExpirationDateUnitsAsync()
        {
            var units = new List<ExpirationDateUnit>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "SELECT ExpirationDateUnitID, ExpirationDateUnitName FROM ExpirationDateUnit ORDER BY ExpirationDateUnitName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            units.Add(new ExpirationDateUnit
                            {
                                ExpirationDateUnitId = reader.GetInt32(0),
                                ExpirationDateUnitName = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки единиц измерения времени: {ex.Message}");
                throw;
            }

            return units;
        }

        // DatabaseService.cs - добавляем новые методы
        public async Task<int> GetProductsCountLastWeekAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var query = @"
                SELECT COUNT(*) 
                FROM Product 
                WHERE EXISTS (
                    SELECT 1 
                    FROM ProductReceipt pr
                    JOIN TableProduct tp ON pr.ProductReceiptTableProductID = tp.TableProductID
                    WHERE tp.TableProductProductID = Product.ProductID
                    AND pr.ProductReceiptDate >= DATEADD(day, -7, GETDATE())
                )";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetWarehousesCountLastMonthAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var query = @"
                SELECT COUNT(DISTINCT StorageID) 
                FROM (
                    SELECT StorageID FROM SettingTheInitialBalances 
                    WHERE SettingTheInitialBalancesDate >= DATEADD(month, -1, GETDATE())
                    UNION
                    SELECT ProductReceiptStorageID FROM ProductReceipt 
                    WHERE ProductReceiptDate >= DATEADD(month, -1, GETDATE())
                    UNION
                    SELECT MovementOfGoodsSenderStorageID FROM MovementOfGoods 
                    WHERE MovementOfGoodsDate >= DATEADD(month, -1, GETDATE())
                    UNION
                    SELECT MovementOfGoodsResepientStorageID FROM MovementOfGoods 
                    WHERE MovementOfGoodsDate >= DATEADD(month, -1, GETDATE())
                ) AS RecentWarehouses";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetPartnersCountLastMonthAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var query = @"
                SELECT COUNT(DISTINCT p.PartnersID) 
                FROM Partners p
                WHERE EXISTS (
                    SELECT 1 
                    FROM ProductReceipt pr
                    WHERE pr.ProductReceiptSupplierID = p.PartnersID
                    AND pr.ProductReceiptDate >= DATEADD(month, -1, GETDATE())
                )";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetDocumentsCountLastWeekAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var query = @"
                SELECT COUNT(*) FROM (
                    SELECT SettingTheInitialBalancesID FROM SettingTheInitialBalances
                    WHERE SettingTheInitialBalancesDate >= DATEADD(day, -7, GETDATE())
                    UNION ALL
                    SELECT ProductReceiptID FROM ProductReceipt
                    WHERE ProductReceiptDate >= DATEADD(day, -7, GETDATE())
                    UNION ALL
                    SELECT MovementOfGoodsID FROM MovementOfGoods
                    WHERE MovementOfGoodsDate >= DATEADD(day, -7, GETDATE())
                    UNION ALL
                    SELECT WriteOffOfGoodsID FROM WriteOffOfGoods
                    WHERE WriteOffOfGoodsDate >= DATEADD(day, -7, GETDATE())
                    UNION ALL
                    SELECT InventoryID FROM Inventory
                    WHERE InventoryDate >= DATEADD(day, -7, GETDATE())
                ) AS RecentDocuments";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        // Методы для работы с пользователями
        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    u.UsersID,
                    u.UsersName,
                    u.UsersLogin,
                    u.UsersPersonID,
                    p.PersonName + ' ' + p.PersonLastName as PersonName
                FROM Users u
                JOIN Person p ON u.UsersPersonID = p.PersonID
                ORDER BY u.UsersName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                UserId = reader.GetInt32(0),
                                UserName = reader.GetString(1),
                                Login = reader.GetString(2),
                                PersonId = reader.GetInt32(3),
                                PersonName = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки пользователей: {ex.Message}");
                throw;
            }

            return users;
        }

        // Методы для работы с контрагентами
        public async Task<List<Partner>> GetSuppliersAsync()
        {
            var partners = new List<Partner>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    PartnersID,
                    PartnersName,
                    PartnersFullName,
                    PartnersINN,
                    PartnersKPP,
                    PartnersPhone,
                    PartnersEmail,
                    PartnersAdressID
                FROM Partners
                ORDER BY PartnersName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            partners.Add(new Partner
                            {
                                PartnerId = reader.GetInt32(0),
                                PartnerName = reader.GetString(1),
                                FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                INN = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                KPP = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Phone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Email = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                AddressId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки контрагентов: {ex.Message}");
                throw;
            }

            return partners;
        }

        // Метод для сохранения документа "Установка начальных остатков"
        public async Task<int> SaveInitialBalanceDocumentAsync(Document document)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Сохраняем табличную часть (товары)
                            var tableProductIds = new List<int>();

                            foreach (var item in document.Items)
                            {
                                var tableProductQuery = @"
                            INSERT INTO TableProduct (
                                TableProductDocumentTypeID,
                                TableProductProductID,
                                TableProductCharacteristicID,
                                TableProductCount,
                                TableProductRemains,
                                TableProductUnitOfMeasurementID
                            )
                            VALUES (
                                @DocumentTypeId,
                                @ProductId,
                                @CharacteristicId,
                                @Quantity,
                                @Quantity, -- Для начальных остатков Remains = Quantity
                                @UnitId
                            );
                            SELECT SCOPE_IDENTITY();";

                                using (var cmd = new SqlCommand(tableProductQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", 0); // ID для "Установка начальных остатков"
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@UnitId", item.UnitOfMeasurementId ?? (object)DBNull.Value);

                                    var tableProductId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                    tableProductIds.Add(tableProductId);
                                }
                            }

                            // 2. Сохраняем основной документ
                            var documentQuery = @"
                        INSERT INTO SettingTheInitialBalances (
                            SettingTheInitialBalancesDocumentTypeID,
                            SettingTheInitialBalancesDate,
                            SettingTheInitialBalancesResponsibleID,
                            SettingTheInitialBalancesStorageID,
                            SettingTheInitialBalancesTableProductID
                        )
                        VALUES (
                            @DocumentTypeId,
                            @Date,
                            @ResponsibleId,
                            @StorageId,
                            @TableProductId
                        );
                        SELECT SCOPE_IDENTITY();";

                            int documentId = 0;

                            // Для каждого товара создаем запись в документе
                            foreach (var tableProductId in tableProductIds)
                            {
                                using (var cmd = new SqlCommand(documentQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", 0);
                                    cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                                    cmd.Parameters.AddWithValue("@ResponsibleId", document.ResponsibleId);
                                    cmd.Parameters.AddWithValue("@StorageId", document.StorageId);
                                    cmd.Parameters.AddWithValue("@TableProductId", tableProductId);

                                    documentId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                }
                            }

                            // 3. Обновляем регистр остатков
                            foreach (var item in document.Items)
                            {
                                var registerQuery = @"
                            INSERT INTO ProductLeftiovers (
                                ProductLeftioversDate,
                                ProductLeftioversOperationID,
                                ProductLeftioversDocumentRegisterID,
                                ProductLeftioversDocumentTypeID,
                                ProductLeftioversProductID,
                                ProductLeftioversProductCharacteristicID,
                                ProductLeftioversQuantity,
                                ProductLeftioversProductUnitOfMeasurementID,
                                ProductLeftioversSenderStorageID,
                                ProductLeftioversResepientStorageID
                            )
                            VALUES (
                                @Date,
                                0, -- Приход
                                @DocumentId,
                                0, -- Установка остатков
                                @ProductId,
                                @CharacteristicId,
                                @Quantity,
                                @UnitId,
                                NULL,
                                @StorageId
                            )";

                                using (var cmd = new SqlCommand(registerQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                                    cmd.Parameters.AddWithValue("@DocumentId", documentId);
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@UnitId", item.UnitOfMeasurementId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@StorageId", document.StorageId);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return documentId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения документа установки остатков: {ex.Message}");
                throw;
            }
        }

        // Метод для сохранения документа "Поступление товаров"
        public async Task<int> SaveProductReceiptDocumentAsync(Document document)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            var tableProductIds = new List<int>();

                            // 1. Сохраняем табличную часть
                            foreach (var item in document.Items)
                            {
                                var tableProductQuery = @"
                            INSERT INTO TableProduct (
                                TableProductDocumentTypeID,
                                TableProductProductID,
                                TableProductCharacteristicID,
                                TableProductCount,
                                TableProductRemains,
                                TableProductUnitOfMeasurementID
                            )
                            VALUES (
                                @DocumentTypeId,
                                @ProductId,
                                @CharacteristicId,
                                @Quantity,
                                @Quantity, -- Для поступления Remains = Quantity
                                @UnitId
                            );
                            SELECT SCOPE_IDENTITY();";

                                using (var cmd = new SqlCommand(tableProductQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", 1); // ID для "Поступление товаров"
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@UnitId", item.UnitOfMeasurementId ?? (object)DBNull.Value);

                                    var tableProductId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                    tableProductIds.Add(tableProductId);
                                }
                            }

                            // 2. Сохраняем основной документ
                            int documentId = 0;

                            foreach (var tableProductId in tableProductIds)
                            {
                                var documentQuery = @"
                            INSERT INTO ProductReceipt (
                                ProductReceiptDocumentTypeID,
                                ProductReceiptDate,
                                ProductReceiptSupplierID,
                                ProductReceiptResponsibleID,
                                ProductReceiptStorageID,
                                ProductReceiptTableProductID
                            )
                            VALUES (
                                @DocumentTypeId,
                                @Date,
                                @SupplierId,
                                @ResponsibleId,
                                @StorageId,
                                @TableProductId
                            );
                            SELECT SCOPE_IDENTITY();";

                                using (var cmd = new SqlCommand(documentQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", 1);
                                    cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                                    cmd.Parameters.AddWithValue("@SupplierId", document.SupplierId);
                                    cmd.Parameters.AddWithValue("@ResponsibleId", document.ResponsibleId);
                                    cmd.Parameters.AddWithValue("@StorageId", document.StorageId);
                                    cmd.Parameters.AddWithValue("@TableProductId", tableProductId);

                                    documentId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                }
                            }

                            // 3. Обновляем регистр остатков
                            foreach (var item in document.Items)
                            {
                                var registerQuery = @"
                            INSERT INTO ProductLeftiovers (
                                ProductLeftioversDate,
                                ProductLeftioversOperationID,
                                ProductLeftioversDocumentRegisterID,
                                ProductLeftioversDocumentTypeID,
                                ProductLeftioversProductID,
                                ProductLeftioversProductCharacteristicID,
                                ProductLeftioversQuantity,
                                ProductLeftioversProductUnitOfMeasurementID,
                                ProductLeftioversSenderStorageID,
                                ProductLeftioversResepientStorageID
                            )
                            VALUES (
                                @Date,
                                0, -- Приход
                                @DocumentId,
                                1, -- Поступление
                                @ProductId,
                                @CharacteristicId,
                                @Quantity,
                                @UnitId,
                                NULL,
                                @StorageId
                            )";

                                using (var cmd = new SqlCommand(registerQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                                    cmd.Parameters.AddWithValue("@DocumentId", documentId);
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@UnitId", item.UnitOfMeasurementId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@StorageId", document.StorageId);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return documentId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения документа поступления: {ex.Message}");
                throw;
            }
        }

        // Аналогичные методы для других типов документов (можно добавить позже)
        public async Task<int> SaveMovementDocumentAsync(Document document)
        {
            // TODO: Реализовать для "Перемещение товаров"
            throw new NotImplementedException();
        }

        public async Task<int> SaveWriteOffDocumentAsync(Document document)
        {
            // TODO: Реализовать для "Списание товаров"
            throw new NotImplementedException();
        }

        public async Task<int> SaveInventoryDocumentAsync(Document document)
        {
            // TODO: Реализовать для "Инвентаризация"
            throw new NotImplementedException();
        }
    }
}

public class ProductType
{
    public int ProductTypeId { get; set; }
    public string ProductTypeName { get; set; }
}

