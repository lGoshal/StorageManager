using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using StorageManager.Helpers;
using StorageManager.Models;

namespace StorageManager.Services
{
    /// <summary>
    /// Логика взаимодействия для DatabaseService.cs
    /// </summary>
    public class DatabaseService
    {
        /// <summary>
        /// Свойства/Коллекции
        /// </summary>
        private readonly string _connectionString;
        public string ConnectionString => _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Методы для товаров
        /// </summary>
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

        /// <summary>
        /// Методы для типов товаров
        /// </summary>
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
        public async Task<int> AddProductTypeAsync(ProductType productType)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                INSERT INTO ProductType (ProductTypeName)
                VALUES (@Name);
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", productType.ProductTypeName);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления вида товара: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateProductTypeAsync(ProductType productType)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE ProductType 
                SET ProductTypeName = @Name
                WHERE ProductTypeID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productType.ProductTypeId);
                        cmd.Parameters.AddWithValue("@Name", productType.ProductTypeName);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления вида товара: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteProductTypeAsync(int productTypeId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM ProductType WHERE ProductTypeID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productTypeId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления вида товара: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Методы для единиц измерения
        /// </summary>
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
        public async Task<int> AddUnitOfMeasurementAsync(UnitOfMeasurement unit)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                INSERT INTO UnitOfMeasurement (UnitOfMeasurementName)
                VALUES (@Name);
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", unit.UnitOfMeasurementName);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления единицы измерения: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateUnitOfMeasurementAsync(UnitOfMeasurement unit)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE UnitOfMeasurement 
                SET UnitOfMeasurementName = @Name
                WHERE UnitOfMeasurementID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", unit.UnitOfMeasurementId);
                        cmd.Parameters.AddWithValue("@Name", unit.UnitOfMeasurementName);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления единицы измерения: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteUnitOfMeasurementAsync(int unitId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM UnitOfMeasurement WHERE UnitOfMeasurementID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", unitId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления единицы измерения: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Методы для характеристик
        /// </summary>
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
        public async Task<int> AddCharacteristicAsync(Characteristic characteristic)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                INSERT INTO Characteristic (CharacteristicName, CharacteristicDescription)
                VALUES (@Name, @Description);
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", characteristic.CharacteristicName);
                        cmd.Parameters.AddWithValue("@Description", characteristic.CharacteristicDescription ?? (object)DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления характеристики: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateCharacteristicAsync(Characteristic characteristic)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE Characteristic 
                SET CharacteristicName = @Name,
                    CharacteristicDescription = @Description
                WHERE CharacteristicID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", characteristic.CharacteristicId);
                        cmd.Parameters.AddWithValue("@Name", characteristic.CharacteristicName);
                        cmd.Parameters.AddWithValue("@Description", characteristic.CharacteristicDescription ?? (object)DBNull.Value);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления характеристики: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteCharacteristicAsync(int characteristicId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM Characteristic WHERE CharacteristicID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", characteristicId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления характеристики: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Методы для складов
        /// </summary>
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

        /// <summary>
        /// Методы для единиц измерения времени
        /// </summary>
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

        /// <summary>
        /// Методы для заполнения карточек статистики
        /// </summary>
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

        /// <summary>
        /// Методы для пользователей
        /// </summary>
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
        public async Task<int> AddUserAsync(User user, string password)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string salt = PasswordHasher.GenerateSalt();
                    string passwordHash = PasswordHasher.HashPassword(password, salt);

                    var personQuery = @"
                INSERT INTO Person (PersonName, PersonLastName, PersonPhone, PersonEmail)
                VALUES (@Name, @LastName, @Phone, @Email);
                SELECT SCOPE_IDENTITY();";

                    int personId;
                    using (var personCmd = new SqlCommand(personQuery, conn))
                    {
                        personCmd.Parameters.AddWithValue("@Name", user.UserName.Split(' ').FirstOrDefault() ?? user.UserName);
                        personCmd.Parameters.AddWithValue("@LastName", user.UserName.Split(' ').LastOrDefault() ?? "");
                        personCmd.Parameters.AddWithValue("@Phone", "");
                        personCmd.Parameters.AddWithValue("@Email", $"{user.Login}@company.local");

                        personId = Convert.ToInt32(await personCmd.ExecuteScalarAsync());
                    }

                    var userQuery = @"
                INSERT INTO Users (UsersName, UsersLogin, UsersPersonID, PasswordSalt, PasswordHash)
                VALUES (@Name, @Login, @PersonId, @Salt, @Hash);
                SELECT SCOPE_IDENTITY();";

                    using (var userCmd = new SqlCommand(userQuery, conn))
                    {
                        userCmd.Parameters.AddWithValue("@Name", user.UserName);
                        userCmd.Parameters.AddWithValue("@Login", user.Login);
                        userCmd.Parameters.AddWithValue("@PersonId", personId);
                        userCmd.Parameters.AddWithValue("@Salt", salt);
                        userCmd.Parameters.AddWithValue("@Hash", passwordHash);

                        var result = await userCmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления пользователя: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateUserAsync(User user, string newPassword = null)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        var updatePasswordQuery = @"
                    UPDATE Users 
                    SET PasswordSalt = @Salt,
                        PasswordHash = @Hash
                    WHERE UsersID = @Id";

                        string salt = PasswordHasher.GenerateSalt();
                        string passwordHash = PasswordHasher.HashPassword(newPassword, salt);

                        using (var cmd = new SqlCommand(updatePasswordQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", user.UserId);
                            cmd.Parameters.AddWithValue("@Salt", salt);
                            cmd.Parameters.AddWithValue("@Hash", passwordHash);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    var updateUserQuery = @"
                UPDATE Users 
                SET UsersName = @Name,
                    UsersLogin = @Login
                WHERE UsersID = @Id";

                    using (var cmd = new SqlCommand(updateUserQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", user.UserId);
                        cmd.Parameters.AddWithValue("@Name", user.UserName);
                        cmd.Parameters.AddWithValue("@Login", user.Login);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления пользователя: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var getPersonIdQuery = "SELECT UsersPersonID FROM Users WHERE UsersID = @Id";
                    int personId = 0;

                    using (var getCmd = new SqlCommand(getPersonIdQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@Id", userId);
                        using (var reader = await getCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                personId = reader.GetInt32(0);
                            }
                        }
                    }

                    var deleteUserQuery = "DELETE FROM Users WHERE UsersID = @Id";
                    using (var userCmd = new SqlCommand(deleteUserQuery, conn))
                    {
                        userCmd.Parameters.AddWithValue("@Id", userId);
                        await userCmd.ExecuteNonQueryAsync();
                    }

                    if (personId > 0)
                    {
                        var checkPersonQuery = "SELECT COUNT(*) FROM Users WHERE UsersPersonID = @PersonId";
                        using (var checkCmd = new SqlCommand(checkPersonQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@PersonId", personId);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                var deletePersonQuery = "DELETE FROM Person WHERE PersonID = @PersonId";
                                using (var personCmd = new SqlCommand(deletePersonQuery, conn))
                                {
                                    personCmd.Parameters.AddWithValue("@PersonId", personId);
                                    await personCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления пользователя: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Методы для контрагентов
        /// </summary>
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

        /// <summary>
        /// Методы для партнеров
        /// </summary>
        public async Task<int> AddPartnerAsync(Partner partner)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                INSERT INTO Partners (
                    PartnersName, 
                    PartnersFullName, 
                    PartnersINN, 
                    PartnersKPP, 
                    PartnersPhone, 
                    PartnersEmail, 
                    PartnersAdressID
                )
                VALUES (
                    @Name, 
                    @FullName, 
                    @INN, 
                    @KPP, 
                    @Phone, 
                    @Email, 
                    @AddressId
                );
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", partner.PartnerName);
                        cmd.Parameters.AddWithValue("@FullName", partner.FullName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@INN", partner.INN ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@KPP", partner.KPP ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", partner.Phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", partner.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AddressId", partner.AddressId ?? (object)DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления контрагента: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdatePartnerAsync(Partner partner)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE Partners 
                SET PartnersName = @Name,
                    PartnersFullName = @FullName,
                    PartnersINN = @INN,
                    PartnersKPP = @KPP,
                    PartnersPhone = @Phone,
                    PartnersEmail = @Email,
                    PartnersAdressID = @AddressId
                WHERE PartnersID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", partner.PartnerId);
                        cmd.Parameters.AddWithValue("@Name", partner.PartnerName);
                        cmd.Parameters.AddWithValue("@FullName", partner.FullName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@INN", partner.INN ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@KPP", partner.KPP ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", partner.Phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", partner.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AddressId", partner.AddressId ?? (object)DBNull.Value);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления контрагента: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeletePartnerAsync(int partnerId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM Partners WHERE PartnersID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", partnerId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления контрагента: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Методы для адреса
        /// </summary>
        public async Task<List<Country>> GetCountriesAsync()
        {
            var countries = new List<Country>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "SELECT CountryID, CountryName, CountryAbbreviation FROM Country ORDER BY CountryName";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            countries.Add(new Country
                            {
                                CountryId = reader.GetInt32(0),
                                CountryName = reader.GetString(1),
                                CountryAbbreviation = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки стран: {ex.Message}");
            }

            return countries;
        }
        public async Task<List<City>> GetCitiesAsync(int? countryId = null)
        {
            var cities = new List<City>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "SELECT CityID, CityName FROM City";

                    if (countryId.HasValue)
                    {

                    }

                    query += " ORDER BY CityName";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (countryId.HasValue)
                        {

                        }

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                cities.Add(new City
                                {
                                    CityId = reader.GetInt32(0),
                                    CityName = reader.GetString(1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки городов: {ex.Message}");
            }

            return cities;
        }
        public async Task<List<Address>> GetAddressesAsync()
        {
            var addresses = new List<Address>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    a.AddressID,
                    a.AddressView,
                    a.AdressCountryID,
                    a.AdressRegionID,
                    a.AdressCityID,
                    a.AdressLocalityID,
                    a.AdressStreetID,
                    a.AdressHouseNumber,
                    a.AdressEntranceNumber,
                    c.CountriesName,
                    ci.CitiesName,
                    s.StreetsName
                FROM Address a
                LEFT JOIN Countries c ON a.AdressCountryID = c.Countries
                LEFT JOIN Cities ci ON a.AdressCityID = ci.CitiesID
                LEFT JOIN Streets s ON a.AdressStreetID = s.StreetsID
                ORDER BY a.AddressID";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            addresses.Add(new Address
                            {
                                AddressId = reader.GetInt32(0),
                                AddressView = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                CountryId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                RegionId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                                CityId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                LocalityId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                StreetId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                HouseNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                EntranceNumber = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                CountryName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                CityName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                StreetName = reader.IsDBNull(11) ? "" : reader.GetString(11)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки адресов: {ex.Message}");
            }

            return addresses;
        }
        public async Task<int> AddAddressAsync(Address address)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string addressView = GenerateAddressView(address);

                    var query = @"
                INSERT INTO Address (
                    AddressView,
                    AdressCountryID,
                    AdressRegionID,
                    AdressCityID,
                    AdressLocalityID,
                    AdressStreetID,
                    AdressHouseNumber,
                    AdressEntranceNumber
                )
                VALUES (
                    @AddressView,
                    @CountryId,
                    @RegionId,
                    @CityId,
                    @LocalityId,
                    @StreetId,
                    @HouseNumber,
                    @EntranceNumber
                );
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@AddressView", addressView);
                        cmd.Parameters.AddWithValue("@CountryId", address.CountryId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RegionId", address.RegionId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CityId", address.CityId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LocalityId", address.LocalityId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@StreetId", address.StreetId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@HouseNumber", address.HouseNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntranceNumber", address.EntranceNumber ?? (object)DBNull.Value);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления адреса: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateAddressAsync(Address address)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string addressView = GenerateAddressView(address);

                    var query = @"
                UPDATE Address 
                SET AddressView = @AddressView,
                    AdressCountryID = @CountryId,
                    AdressRegionID = @RegionId,
                    AdressCityID = @CityId,
                    AdressLocalityID = @LocalityId,
                    AdressStreetID = @StreetId,
                    AdressHouseNumber = @HouseNumber,
                    AdressEntranceNumber = @EntranceNumber
                WHERE AddressID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", address.AddressId);
                        cmd.Parameters.AddWithValue("@AddressView", addressView);
                        cmd.Parameters.AddWithValue("@CountryId", address.CountryId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RegionId", address.RegionId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CityId", address.CityId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LocalityId", address.LocalityId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@StreetId", address.StreetId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@HouseNumber", address.HouseNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntranceNumber", address.EntranceNumber ?? (object)DBNull.Value);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления адреса: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM Address WHERE AddressID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", addressId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления адреса: {ex.Message}");
                throw;
            }
        }
        private string GenerateAddressView(Address address)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(address.CountryName))
                parts.Add(address.CountryName);

            if (!string.IsNullOrEmpty(address.CityName))
                parts.Add("г. " + address.CityName);

            if (!string.IsNullOrEmpty(address.StreetName))
                parts.Add("ул. " + address.StreetName);

            if (!string.IsNullOrEmpty(address.HouseNumber))
                parts.Add("д. " + address.HouseNumber);

            if (!string.IsNullOrEmpty(address.EntranceNumber))
                parts.Add("под. " + address.EntranceNumber);

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Методы для договоров
        /// </summary>
        public async Task<List<Contract>> GetContractsAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    c.ContractsID,
                    c.ContractsName,
                    c.ContractsObject,
                    c.ContractsCustomerID,
                    c.ContractsContractorID,
                    c.ContractsValue,
                    c.ContractsTimeOfAction,
                    c.ContractsExpirationDateUnitID,
                    customer.PersonName + ' ' + ISNULL(customer.PersonMiddleName, '') + ' ' + ISNULL(customer.PersonLastName, '') as CustomerName,
                    contractor.PersonName + ' ' + ISNULL(contractor.PersonMiddleName, '') + ' ' + ISNULL(contractor.PersonLastName, '') as ContractorName,
                    u.UnitOfMeasurementName as ExpirationDateUnitName
                FROM Contracts c
                LEFT JOIN Person customer ON c.ContractsCustomerID = customer.PersonID
                LEFT JOIN Person contractor ON c.ContractsContractorID = contractor.PersonID
                LEFT JOIN UnitOfMeasurement u ON c.ContractsExpirationDateUnitID = u.UnitOfMeasurementID
                ORDER BY c.ContractsName";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        var contracts = new List<Contract>();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var contract = new Contract
                                {
                                    ContractsID = reader.GetInt32(0),
                                    ContractsName = reader.GetString(1),
                                    ContractsObject = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    ContractsCustomerID = reader.GetInt32(3),
                                    ContractsContractorID = reader.GetInt32(4),
                                    ContractsValue = reader.GetDecimal(5),
                                    ContractsTimeOfAction = reader.GetDecimal(6),
                                    ContractsExpirationDateUnitID = reader.GetInt32(7),
                                    CustomerName = reader.IsDBNull(8) ? "Не указан" : reader.GetString(8),
                                    ContractorName = reader.IsDBNull(9) ? "Не указан" : reader.GetString(9),
                                    ExpirationDateUnitName = reader.IsDBNull(10) ? "Не указана" : reader.GetString(10)
                                };

                                contracts.Add(contract);
                            }
                        }

                        return contracts;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки договоров: {ex.Message}");
                throw;
            }
        }
        public async Task<List<dynamic>> GetPersonsForDropdownAsync()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                SELECT 
                    PersonID,
                    PersonName + ' ' + ISNULL(PersonMiddleName, '') + ' ' + ISNULL(PersonLastName, '') as FullName
                FROM Person
                ORDER BY FullName";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        var persons = new List<dynamic>();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                persons.Add(new
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1)
                                });
                            }
                        }

                        return persons;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки контрагентов: {ex.Message}");
                throw;
            }
        }
        public async Task<int> AddContractAsync(Contract contract)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                INSERT INTO Contracts (
                    ContractsName,
                    ContractsObject,
                    ContractsCustomerID,
                    ContractsContractorID,
                    ContractsValue,
                    ContractsTimeOfAction,
                    ContractsExpirationDateUnitID
                ) VALUES (
                    @Name,
                    @Object,
                    @CustomerID,
                    @ContractorID,
                    @Value,
                    @TimeOfAction,
                    @ExpirationDateUnitID
                );
                SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", contract.ContractsName);
                        cmd.Parameters.AddWithValue("@Object", (object)contract.ContractsObject ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerID", contract.ContractsCustomerID);
                        cmd.Parameters.AddWithValue("@ContractorID", contract.ContractsContractorID);
                        cmd.Parameters.AddWithValue("@Value", contract.ContractsValue);
                        cmd.Parameters.AddWithValue("@TimeOfAction", contract.ContractsTimeOfAction);
                        cmd.Parameters.AddWithValue("@ExpirationDateUnitID", contract.ContractsExpirationDateUnitID);

                        var result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления договора: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateContractAsync(Contract contract)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = @"
                UPDATE Contracts 
                SET 
                    ContractsName = @Name,
                    ContractsObject = @Object,
                    ContractsCustomerID = @CustomerID,
                    ContractsContractorID = @ContractorID,
                    ContractsValue = @Value,
                    ContractsTimeOfAction = @TimeOfAction,
                    ContractsExpirationDateUnitID = @ExpirationDateUnitID
                WHERE ContractsID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", contract.ContractsID);
                        cmd.Parameters.AddWithValue("@Name", contract.ContractsName);
                        cmd.Parameters.AddWithValue("@Object", (object)contract.ContractsObject ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CustomerID", contract.ContractsCustomerID);
                        cmd.Parameters.AddWithValue("@ContractorID", contract.ContractsContractorID);
                        cmd.Parameters.AddWithValue("@Value", contract.ContractsValue);
                        cmd.Parameters.AddWithValue("@TimeOfAction", contract.ContractsTimeOfAction);
                        cmd.Parameters.AddWithValue("@ExpirationDateUnitID", contract.ContractsExpirationDateUnitID);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления договора: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> DeleteContractAsync(int contractId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var query = "DELETE FROM Contracts WHERE ContractsID = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", contractId);
                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления договора: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Методы заглушки для документов
        /// </summary>
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
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", 0);
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@UnitId", item.UnitOfMeasurementId ?? (object)DBNull.Value);

                                    var tableProductId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                    tableProductIds.Add(tableProductId);
                                }
                            }

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
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", 1);
                                    cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                    cmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@UnitId", item.UnitOfMeasurementId ?? (object)DBNull.Value);

                                    var tableProductId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                    tableProductIds.Add(tableProductId);
                                }
                            }

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
        public async Task<int> SaveMovementDocumentAsync(Document document)
        {
            throw new NotImplementedException();
        }
        public async Task<int> SaveWriteOffDocumentAsync(Document document)
        {
            throw new NotImplementedException();
        }
        public async Task<int> SaveInventoryDocumentAsync(Document document)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Универсальные методы для документов
        /// </summary>
        public async Task<bool> UpdateDocumentAsync(Document document, string tableName)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = tableName switch
                    {
                        "SettingTheInitialBalances" => @"
                    UPDATE SettingTheInitialBalances 
                    SET SettingTheInitialBalancesDate = @Date,
                        SettingTheInitialBalancesResponsibleID = @ResponsibleId,
                        SettingTheInitialBalancesStorageID = @StorageId
                    WHERE SettingTheInitialBalancesID = @DocumentId",

                        "ProductReceipt" => @"
                    UPDATE ProductReceipt 
                    SET ProductReceiptDate = @Date,
                        ProductReceiptResponsibleID = @ResponsibleId,
                        ProductReceiptStorageID = @StorageId,
                        ProductReceiptSupplierID = @SupplierId
                    WHERE ProductReceiptID = @DocumentId",

                        "MovementOfGoods" => @"
                    UPDATE MovementOfGoods 
                    SET MovementOfGoodsDate = @Date,
                        MovementOfGoodsResponsibleID = @ResponsibleId,
                        MovementOfGoodsSenderStorageID = @SenderStorageId,
                        MovementOfGoodsResepientStorageID = @RecipientStorageId
                    WHERE MovementOfGoodsID = @DocumentId",

                        "WriteOffOfGoods" => @"
                    UPDATE WriteOffOfGoods 
                    SET WriteOffOfGoodsDate = @Date,
                        WriteOffOfGoodsResponsibleID = @ResponsibleId
                    WHERE WriteOffOfGoodsID = @DocumentId",

                        "Inventory" => @"
                    UPDATE Inventory 
                    SET InventoryDate = @Date,
                        InventoryResponsibleID = @ResponsibleId,
                        InventoryStorageID = @StorageId
                    WHERE InventoryID = @DocumentId",

                        _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
                    };

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocumentId", document.DocumentId);
                        cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                        cmd.Parameters.AddWithValue("@ResponsibleId", document.ResponsibleId);

                        switch (tableName)
                        {
                            case "SettingTheInitialBalances":
                                cmd.Parameters.AddWithValue("@StorageId", document.StorageId ?? (object)DBNull.Value);
                                break;
                            case "Inventory":
                                cmd.Parameters.AddWithValue("@StorageId", document.StorageId ?? (object)DBNull.Value);
                                break;

                            case "ProductReceipt":
                                cmd.Parameters.AddWithValue("@SupplierId", document.SupplierId ?? (object)DBNull.Value);
                                break;

                            case "MovementOfGoods":
                                cmd.Parameters.AddWithValue("@SenderStorageId", document.SenderStorageId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RecipientStorageId", document.RecipientStorageId ?? (object)DBNull.Value);
                                break;
                        }

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления документа в таблице {tableName}: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateDocumentItemsAsync(Document document, string tableName)
        {
            Console.WriteLine($"Обновление товаров документа ID: {document.DocumentId}");
            Console.WriteLine($"Тип документа: {tableName}");
            Console.WriteLine($"Количество товаров: {document.Items.Count}");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            await DeleteOldDocumentItemsAsync(document.DocumentId, tableName, connection, transaction);

                            foreach (var item in document.Items)
                            {
                                await AddNewDocumentItemAsync(document, item, tableName, connection, transaction);
                                Console.WriteLine($"  - {item.ProductName}: {item.Quantity}");
                            }

                            transaction.Commit();
                            Console.WriteLine("Товары документа успешно обновлены");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Ошибка при обновлении товаров: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка соединения с БД: {ex.Message}");
                throw;
            }
        }
        private async Task DeleteOldDocumentItemsAsync(int documentId, string tableName,
                                                SqlConnection connection, SqlTransaction transaction)
        {
            string getTableProductIdsQuery = tableName switch
            {
                "SettingTheInitialBalances" =>
                    "SELECT SettingTheInitialBalancesTableProductID FROM SettingTheInitialBalances WHERE SettingTheInitialBalancesID = @DocumentId",
                "ProductReceipt" =>
                    "SELECT ProductReceiptTableProductID FROM ProductReceipt WHERE ProductReceiptID = @DocumentId",
                "MovementOfGoods" =>
                    "SELECT MovementOfGoodsTableProductID FROM MovementOfGoods WHERE MovementOfGoodsID = @DocumentId",
                "WriteOffOfGoods" =>
                    "SELECT WriteOffOfGoodsTableProductID FROM WriteOffOfGoods WHERE WriteOffOfGoodsID = @DocumentId",
                "Inventory" =>
                    "SELECT InventoryTableProductID FROM Inventory WHERE InventoryID = @DocumentId",
                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            List<int> tableProductIds = new List<int>();

            using (var getIdsCmd = new SqlCommand(getTableProductIdsQuery, connection, transaction))
            {
                getIdsCmd.Parameters.AddWithValue("@DocumentId", documentId);

                using (var reader = await getIdsCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            tableProductIds.Add(reader.GetInt32(0));
                        }
                    }
                }
            }

            string deleteDocumentQuery = tableName switch
            {
                "SettingTheInitialBalances" =>
                    "DELETE FROM [SettingTheInitialBalances] WHERE SettingTheInitialBalancesID = @DocumentId",
                "ProductReceipt" =>
                    "DELETE FROM [ProductReceipt] WHERE ProductReceiptID = @DocumentId",
                "MovementOfGoods" =>
                    "DELETE FROM [MovementOfGoods] WHERE MovementOfGoodsID = @DocumentId",
                "WriteOffOfGoods" =>
                    "DELETE FROM [WriteOffOfGoods] WHERE WriteOffOfGoodsID = @DocumentId",
                "Inventory" =>
                    "DELETE FROM [Inventory] WHERE InventoryID = @DocumentId",
                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            using (var deleteDocCmd = new SqlCommand(deleteDocumentQuery, connection, transaction))
            {
                deleteDocCmd.Parameters.AddWithValue("@DocumentId", documentId);
                await deleteDocCmd.ExecuteNonQueryAsync();
            }

            if (tableProductIds.Any())
            {
                string deleteTableProductQuery = @"
            DELETE FROM [TableProduct] 
            WHERE TableProductID IN ({0})";

                var parameters = tableProductIds.Select((id, index) => $"@id{index}").ToArray();
                deleteTableProductQuery = string.Format(deleteTableProductQuery, string.Join(",", parameters));

                using (var deleteTpCmd = new SqlCommand(deleteTableProductQuery, connection, transaction))
                {
                    for (int i = 0; i < tableProductIds.Count; i++)
                    {
                        deleteTpCmd.Parameters.AddWithValue($"@id{i}", tableProductIds[i]);
                    }

                    await deleteTpCmd.ExecuteNonQueryAsync();
                }
            }

            Console.WriteLine($"Удалено {tableProductIds.Count} товаров для документа {documentId}");
        }
        private async Task AddNewDocumentItemAsync(Document document, DocumentItem item, string tableName,
                                                   SqlConnection connection, SqlTransaction transaction, bool isNew = false)
        {
            int documentTypeId = tableName switch
            {
                "SettingTheInitialBalances" => 0,
                "ProductReceipt" => 1,
                "MovementOfGoods" => 2,
                "WriteOffOfGoods" => 3,
                "Inventory" => 4,
                _ => 0
            };

            var insertTableProductQuery = @"
        INSERT INTO [TableProduct] 
        ([TableProductDocumentTypeID], [TableProductProductID], 
         [TableProductCharacteristicID], [TableProductCount], 
         [TableProductRemains], [TableProductUnitOfMeasurementID])
        VALUES 
        (@DocumentTypeId, @ProductId, @CharacteristicId, 
         @Count, @Remains, @UnitOfMeasurementId);
        SELECT SCOPE_IDENTITY();";

            int tableProductId;

            using (var insertCmd = new SqlCommand(insertTableProductQuery, connection, transaction))
            {
                insertCmd.Parameters.AddWithValue("@DocumentTypeId", documentTypeId);
                insertCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                insertCmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Count", item.Quantity);

                object remainsValue = documentTypeId switch
                {
                    0 or 1 => item.Quantity,
                    _ => (object)DBNull.Value
                };

                insertCmd.Parameters.AddWithValue("@Remains", remainsValue);
                insertCmd.Parameters.AddWithValue("@UnitOfMeasurementId", item.UnitOfMeasurementId ?? (object)DBNull.Value);

                tableProductId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
            }

            if (!isNew)
            {
                await LinkTableProductToDocumentAsync(document.DocumentId, tableProductId, tableName, connection, transaction);
            }
        }
        private async Task LinkTableProductToDocumentAsync(int documentId, int tableProductId, string tableName,
                                                           SqlConnection connection, SqlTransaction transaction)
        {
            string insertDocumentQuery = tableName switch
            {
                "SettingTheInitialBalances" => @"
            INSERT INTO [SettingTheInitialBalances] 
            ([SettingTheInitialBalancesDocumentTypeID], [SettingTheInitialBalancesDate],
             [SettingTheInitialBalancesResponsibleID], [SettingTheInitialBalancesStorageID],
             [SettingTheInitialBalancesTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @StorageId, @TableProductId)",

                "ProductReceipt" => @"
            INSERT INTO [ProductReceipt] 
            ([ProductReceiptDocumentTypeID], [ProductReceiptDate],
             [ProductReceiptSupplierID], [ProductReceiptResponsibleID],
             [ProductReceiptStorageID], [ProductReceiptTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @SupplierId, @ResponsibleId, 
             @StorageId, @TableProductId)",

                "MovementOfGoods" => @"
            INSERT INTO [MovementOfGoods] 
            ([MovementOfGoodsDocumentTypeID], [MovementOfGoodsDate],
             [MovementOfGoodsResponsibleID], [MovementOfGoodsSenderStorageID],
             [MovementOfGoodsResepientStorageID], [MovementOfGoodsTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @SenderStorageId,
             @RecipientStorageId, @TableProductId)",

                "WriteOffOfGoods" => @"
            INSERT INTO [WriteOffOfGoods] 
            ([WriteOffOfGoodsDocumentTypeID], [WriteOffOfGoodsDate],
             [WriteOffOfGoodsResponsibleID], [WriteOffOfGoodsTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @TableProductId)",

                "Inventory" => @"
            INSERT INTO [Inventory] 
            ([InventoryDocumentTypeID], [InventoryDate],
             [InventoryResponsibleID], [InventoryStorageID],
             [InventoryTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @StorageId, @TableProductId)",

                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            using (var docCmd = new SqlCommand(insertDocumentQuery, connection, transaction))
            {
                docCmd.Parameters.AddWithValue("@DocumentTypeId", GetDocumentTypeId(tableName));
                docCmd.Parameters.AddWithValue("@Date", DateTime.Now); // Можно добавить дату документа как параметр
                docCmd.Parameters.AddWithValue("@TableProductId", tableProductId);

                // Остальные параметры будут установлены позже или можно получить из кэша
                // Пока оставляем заглушки или нужно передать document
                await docCmd.ExecuteNonQueryAsync();
            }
        }     
        private int GetDocumentTypeId(string tableName)
        {
            return tableName switch
            {
                "SettingTheInitialBalances" => 0,
                "ProductReceipt" => 1,
                "MovementOfGoods" => 2,
                "WriteOffOfGoods" => 3,
                "Inventory" => 4,
                _ => 0
            };
        }
        public async Task<List<DocumentItem>> GetDocumentItemsAsync(int documentId, string tableName)
        {
            var items = new List<DocumentItem>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string itemsQuery = @"
                SELECT 
                    tp.TableProductID,
                    tp.TableProductProductID,
                    tp.TableProductCharacteristicID,
                    tp.TableProductCount,
                    tp.TableProductRemains,
                    tp.TableProductUnitOfMeasurementID,
                    p.ProductName,
                    c.CharacteristicName,
                    uom.UnitOfMeasurementName
                FROM TableProduct tp
                JOIN Product p ON tp.TableProductProductID = p.ProductID
                LEFT JOIN Characteristic c ON tp.TableProductCharacteristicID = c.CharacteristicID
                LEFT JOIN UnitOfMeasurement uom ON tp.TableProductUnitOfMeasurementID = uom.UnitOfMeasurementID
                WHERE tp.TableProductID IN (
                    SELECT DISTINCT TableProductID FROM (
                        SELECT SettingTheInitialBalancesTableProductID FROM SettingTheInitialBalances WHERE SettingTheInitialBalancesID = @DocumentId
                        UNION ALL
                        SELECT ProductReceiptTableProductID FROM ProductReceipt WHERE ProductReceiptID = @DocumentId
                        UNION ALL
                        SELECT MovementOfGoodsTableProductID FROM MovementOfGoods WHERE MovementOfGoodsID = @DocumentId
                        UNION ALL
                        SELECT WriteOffOfGoodsTableProductID FROM WriteOffOfGoods WHERE WriteOffOfGoodsID = @DocumentId
                        UNION ALL
                        SELECT InventoryTableProductID FROM Inventory WHERE InventoryID = @DocumentId
                    ) AS DocItems(TableProductID)
                )";

                    using (var cmd = new SqlCommand(itemsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocumentId", documentId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new DocumentItem
                                {
                                    ItemId = reader.GetInt32(0),
                                    ProductId = reader.GetInt32(1),
                                    CharacteristicId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                    Quantity = reader.GetDecimal(3),
                                    UnitOfMeasurementId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                    ProductName = reader.GetString(6),
                                    CharacteristicName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    UnitOfMeasurementName = reader.IsDBNull(8) ? "" : reader.GetString(8)
                                };

                                items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки товаров документа: {ex.Message}");
                throw;
            }

            return items;
        }
        public async Task<DocumentData> GetDocumentDataAsync(int documentId, string tableName)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = "";

                    switch (tableName)
                    {
                        case "SettingTheInitialBalances":
                            query = @"
                        SELECT 
                            stib.SettingTheInitialBalancesDate,
                            stib.SettingTheInitialBalancesResponsibleID,
                            stib.SettingTheInitialBalancesStorageID,
                            u.UsersName,
                            s.StorageName
                        FROM SettingTheInitialBalances stib
                        JOIN Users u ON stib.SettingTheInitialBalancesResponsibleID = u.UsersID
                        JOIN Storage s ON stib.SettingTheInitialBalancesStorageID = s.StorageID
                        WHERE stib.SettingTheInitialBalancesID = @DocumentId";
                            break;

                        case "ProductReceipt":
                            query = @"
                        SELECT 
                            pr.ProductReceiptDate,
                            pr.ProductReceiptResponsibleID,
                            pr.ProductReceiptSupplierID,
                            pr.ProductReceiptStorageID,
                            u.UsersName,
                            p.PartnersName,
                            s.StorageName
                        FROM ProductReceipt pr
                        JOIN Users u ON pr.ProductReceiptResponsibleID = u.UsersID
                        LEFT JOIN Partners p ON pr.ProductReceiptSupplierID = p.PartnersID
                        JOIN Storage s ON pr.ProductReceiptStorageID = s.StorageID
                        WHERE pr.ProductReceiptID = @DocumentId";
                            break;

                        case "MovementOfGoods":
                            query = @"
                        SELECT 
                            mg.MovementOfGoodsDate,
                            mg.MovementOfGoodsResponsibleID,
                            mg.MovementOfGoodsSenderStorageID,
                            mg.MovementOfGoodsResepientStorageID,
                            u.UsersName,
                            ss.StorageName as SenderStorageName,
                            rs.StorageName as RecipientStorageName
                        FROM MovementOfGoods mg
                        JOIN Users u ON mg.MovementOfGoodsResponsibleID = u.UsersID
                        JOIN Storage ss ON mg.MovementOfGoodsSenderStorageID = ss.StorageID
                        JOIN Storage rs ON mg.MovementOfGoodsResepientStorageID = rs.StorageID
                        WHERE mg.MovementOfGoodsID = @DocumentId";
                            break;

                        case "WriteOffOfGoods":
                            query = @"
                        SELECT 
                            wog.WriteOffOfGoodsDate,
                            wog.WriteOffOfGoodsResponsibleID,
                            u.UsersName
                        FROM WriteOffOfGoods wog
                        JOIN Users u ON wog.WriteOffOfGoodsResponsibleID = u.UsersID
                        WHERE wog.WriteOffOfGoodsID = @DocumentId";
                            break;

                        case "Inventory":
                            query = @"
                        SELECT 
                            inv.InventoryDate,
                            inv.InventoryResponsibleID,
                            inv.InventoryStorageID,
                            u.UsersName,
                            s.StorageName
                        FROM Inventory inv
                        JOIN Users u ON inv.InventoryResponsibleID = u.UsersID
                        JOIN Storage s ON inv.InventoryStorageID = s.StorageID
                        WHERE inv.InventoryID = @DocumentId";
                            break;

                        default:
                            throw new ArgumentException($"Неизвестная таблица: {tableName}");
                    }

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocumentId", documentId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var documentData = new DocumentData
                                {
                                    DocumentDate = reader.GetDateTime(0),
                                    ResponsibleId = reader.GetInt32(1),
                                    ResponsibleName = reader.GetString(reader.GetOrdinal("UsersName"))
                                };

                                switch (tableName)
                                {
                                    case "SettingTheInitialBalances":
                                    case "Inventory":
                                        documentData.StorageId = reader.GetInt32(2);
                                        documentData.LocationInfo = reader.GetString(reader.GetOrdinal("StorageName"));
                                        break;

                                    case "ProductReceipt":
                                        documentData.SupplierId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                                        documentData.StorageId = reader.GetInt32(3);
                                        documentData.LocationInfo = reader.GetString(reader.GetOrdinal("StorageName"));
                                        break;

                                    case "MovementOfGoods":
                                        documentData.SenderStorageId = reader.GetInt32(2);
                                        documentData.RecipientStorageId = reader.GetInt32(3);
                                        documentData.LocationInfo = reader.GetString(reader.GetOrdinal("SenderStorageName")) +
                                                                   " → " + reader.GetString(reader.GetOrdinal("RecipientStorageName"));
                                        break;
                                }

                                return documentData;
                            }
                            else
                            {
                                throw new Exception($"Документ с ID {documentId} не найден в таблице {tableName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных документа: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateDocumentStatusAsync(int documentId, string tableName, string status)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = tableName switch
                    {
                        "SettingTheInitialBalances" =>
                            "UPDATE SettingTheInitialBalances SET Status = @Status WHERE SettingTheInitialBalancesID = @DocumentId",
                        "ProductReceipt" =>
                            "UPDATE ProductReceipt SET Status = @Status WHERE ProductReceiptID = @DocumentId",
                        "MovementOfGoods" =>
                            "UPDATE MovementOfGoods SET Status = @Status WHERE MovementOfGoodsID = @DocumentId",
                        "WriteOffOfGoods" =>
                            "UPDATE WriteOffOfGoods SET Status = @Status WHERE WriteOffOfGoodsID = @DocumentId",
                        "Inventory" =>
                            "UPDATE Inventory SET Status = @Status WHERE InventoryID = @DocumentId",
                        _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
                    };

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocumentId", documentId);
                        cmd.Parameters.AddWithValue("@Status", status);

                        var rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статуса документа: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> CheckDocumentStatusAsync(int documentId, string tableName, string requiredStatus)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = tableName switch
                    {
                        "SettingTheInitialBalances" =>
                            "SELECT Status FROM SettingTheInitialBalances WHERE SettingTheInitialBalancesID = @DocumentId",
                        "ProductReceipt" =>
                            "SELECT Status FROM ProductReceipt WHERE ProductReceiptID = @DocumentId",
                        "MovementOfGoods" =>
                            "SELECT Status FROM MovementOfGoods WHERE MovementOfGoodsID = @DocumentId",
                        "WriteOffOfGoods" =>
                            "SELECT Status FROM WriteOffOfGoods WHERE WriteOffOfGoodsID = @DocumentId",
                        "Inventory" =>
                            "SELECT Status FROM Inventory WHERE InventoryID = @DocumentId",
                        _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
                    };

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocumentId", documentId);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            return result.ToString() == requiredStatus;
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки статуса документа: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> SaveDocumentAsync(Document document, string tableName, bool isPosting = false)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            bool documentUpdated = await UpdateDocumentAsync(document, tableName, connection, transaction);

                            if (!documentUpdated)
                            {
                                throw new Exception("Не удалось обновить документ");
                            }

                            bool itemsUpdated = await UpdateDocumentItemsAsync(document, tableName, connection, transaction);

                            if (!itemsUpdated)
                            {
                                throw new Exception("Не удалось обновить товары документа");
                            }

                            if (isPosting)
                            {
                                bool statusUpdated = await UpdateDocumentStatusAsync(document.DocumentId, tableName, "Проведен",
                                                                                     connection, transaction);

                                if (!statusUpdated)
                                {
                                    throw new Exception("Не удалось обновить статус документа");
                                }
                            }
                            else
                            {
                                bool statusUpdated = await UpdateDocumentStatusAsync(document.DocumentId, tableName, "Черновик",
                                                                                     connection, transaction);
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Ошибка при сохранении документа: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения документа: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Перегрузка универсальных методов
        /// </summary>
        private async Task<bool> UpdateDocumentStatusAsync(int documentId, string tableName, string status,
                                                           SqlConnection connection, SqlTransaction transaction)
        {
            string query = tableName switch
            {
                "SettingTheInitialBalances" =>
                    "UPDATE SettingTheInitialBalances SET Status = @Status WHERE SettingTheInitialBalancesID = @DocumentId",
                "ProductReceipt" =>
                    "UPDATE ProductReceipt SET Status = @Status WHERE ProductReceiptID = @DocumentId",
                "MovementOfGoods" =>
                    "UPDATE MovementOfGoods SET Status = @Status WHERE MovementOfGoodsID = @DocumentId",
                "WriteOffOfGoods" =>
                    "UPDATE WriteOffOfGoods SET Status = @Status WHERE WriteOffOfGoodsID = @DocumentId",
                "Inventory" =>
                    "UPDATE Inventory SET Status = @Status WHERE InventoryID = @DocumentId",
                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            using (var cmd = new SqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@DocumentId", documentId);
                cmd.Parameters.AddWithValue("@Status", status);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
        private async Task<bool> UpdateDocumentAsync(Document document, string tableName,
                                                     SqlConnection connection, SqlTransaction transaction)
        {
            string query = tableName switch
            {
                "SettingTheInitialBalances" => @"
            UPDATE SettingTheInitialBalances 
            SET SettingTheInitialBalancesDate = @Date,
                SettingTheInitialBalancesResponsibleID = @ResponsibleId,
                SettingTheInitialBalancesStorageID = @StorageId
            WHERE SettingTheInitialBalancesID = @DocumentId",

                "ProductReceipt" => @"
            UPDATE ProductReceipt 
            SET ProductReceiptDate = @Date,
                ProductReceiptResponsibleID = @ResponsibleId,
                ProductReceiptStorageID = @StorageId,
                ProductReceiptSupplierID = @SupplierId
            WHERE ProductReceiptID = @DocumentId",

                "MovementOfGoods" => @"
            UPDATE MovementOfGoods 
            SET MovementOfGoodsDate = @Date,
                MovementOfGoodsResponsibleID = @ResponsibleId,
                MovementOfGoodsSenderStorageID = @SenderStorageId,
                MovementOfGoodsResepientStorageID = @RecipientStorageId
            WHERE MovementOfGoodsID = @DocumentId",

                "WriteOffOfGoods" => @"
            UPDATE WriteOffOfGoods 
            SET WriteOffOfGoodsDate = @Date,
                WriteOffOfGoodsResponsibleID = @ResponsibleId
            WHERE WriteOffOfGoodsID = @DocumentId",

                "Inventory" => @"
            UPDATE Inventory 
            SET InventoryDate = @Date,
                InventoryResponsibleID = @ResponsibleId,
                InventoryStorageID = @StorageId
            WHERE InventoryID = @DocumentId",

                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            using (var cmd = new SqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@DocumentId", document.DocumentId);
                cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                cmd.Parameters.AddWithValue("@ResponsibleId", document.ResponsibleId);

                switch (tableName)
                {
                    case "SettingTheInitialBalances":
                    case "Inventory":
                        cmd.Parameters.AddWithValue("@StorageId", document.StorageId ?? (object)DBNull.Value);
                        break;
                    case "ProductReceipt":
                        cmd.Parameters.AddWithValue("@SupplierId", document.SupplierId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@StorageId", document.StorageId ?? (object)DBNull.Value);
                        break;
                    case "MovementOfGoods":
                        cmd.Parameters.AddWithValue("@SenderStorageId", document.SenderStorageId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RecipientStorageId", document.RecipientStorageId ?? (object)DBNull.Value);
                        break;
                }

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }
        private async Task<bool> UpdateDocumentItemsAsync(Document document, string tableName,
                                                          SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // 1. Удаляем старые товары документа
                await DeleteOldDocumentItemsAsync(document.DocumentId, tableName, connection, transaction);

                // 2. Добавляем новые товары
                foreach (var item in document.Items)
                {
                    await AddNewDocumentItemAsync(document, item, tableName, connection, transaction);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления товаров документа: {ex.Message}");
                throw;
            }
        }
        public async Task<int> SaveDocumentAsync(Document document, string tableName, bool isPosting = false, bool isNew = false)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int documentId;

                            if (isNew)
                            {
                                documentId = await CreateNewDocumentAsync(document, tableName, connection, transaction);
                                Console.WriteLine($"Создали документ с ID: {documentId}");

                                foreach (var item in document.Items)
                                {
                                    await AddNewDocumentItemForNewDocumentAsync(document, item, tableName, documentId,
                                                                               connection, transaction);
                                }
                            }
                            else
                            {
                                documentId = document.DocumentId;
                                Console.WriteLine($"Обновляем документ с ID: {documentId}");

                                bool documentUpdated = await UpdateDocumentAsync(document, tableName, connection, transaction);

                                if (!documentUpdated)
                                {
                                    throw new Exception("Не удалось обновить документ");
                                }

                                await DeleteOldDocumentItemsAsync(documentId, tableName, connection, transaction);

                                foreach (var item in document.Items)
                                {
                                    await AddNewDocumentItemForNewDocumentAsync(document, item, tableName, documentId,
                                                                               connection, transaction);
                                }
                            }

                            string status = isPosting ? "Проведен" : "Черновик";
                            //await UpdateDocumentStatusAsync(documentId, tableName, status, connection, transaction);
                            Console.WriteLine($"Статус документа {documentId} обновлен на: {status}");

                            transaction.Commit();
                            Console.WriteLine($"Транзакция завершена успешно. Документ ID: {documentId}");
                            return documentId;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Ошибка при сохранении документа: {ex.Message}");
                            Console.WriteLine($"StackTrace: {ex.StackTrace}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения документа: {ex.Message}");
                throw;
            }
        }
        private async Task<int> CreateNewDocumentAsync(Document document, string tableName,
                                                SqlConnection connection, SqlTransaction transaction)
        {
            string query = tableName switch
            {
                "SettingTheInitialBalances" => @"
            INSERT INTO SettingTheInitialBalances 
            (SettingTheInitialBalancesDocumentTypeID, SettingTheInitialBalancesDate,
             SettingTheInitialBalancesResponsibleID, SettingTheInitialBalancesStorageID)
            VALUES (0, @Date, @ResponsibleId, @StorageId);
            SELECT SCOPE_IDENTITY();",

                "ProductReceipt" => @"
            INSERT INTO ProductReceipt 
            (ProductReceiptDocumentTypeID, ProductReceiptDate,
             ProductReceiptSupplierID, ProductReceiptResponsibleID, ProductReceiptStorageID)
            VALUES (1, @Date, @SupplierId, @ResponsibleId, @StorageId);
            SELECT SCOPE_IDENTITY();",

                "MovementOfGoods" => @"
            INSERT INTO MovementOfGoods 
            (MovementOfGoodsDocumentTypeID, MovementOfGoodsDate,
             MovementOfGoodsResponsibleID, MovementOfGoodsSenderStorageID, 
             MovementOfGoodsResepientStorageID)
            VALUES (2, @Date, @ResponsibleId, @SenderStorageId, @RecipientStorageId);
            SELECT SCOPE_IDENTITY();",

                "WriteOffOfGoods" => @"
            INSERT INTO WriteOffOfGoods 
            (WriteOffOfGoodsDocumentTypeID, WriteOffOfGoodsDate,
             WriteOffOfGoodsResponsibleID)
            VALUES (3, @Date, @ResponsibleId);
            SELECT SCOPE_IDENTITY();",

                "Inventory" => @"
            INSERT INTO Inventory 
            (InventoryDocumentTypeID, InventoryDate,
             InventoryResponsibleID, InventoryStorageID)
            VALUES (4, @Date, @ResponsibleId, @StorageId);
            SELECT SCOPE_IDENTITY();",

                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            using (var cmd = new SqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                cmd.Parameters.AddWithValue("@ResponsibleId", document.ResponsibleId);

                switch (tableName)
                {
                    case "SettingTheInitialBalances":
                    case "Inventory":
                        cmd.Parameters.AddWithValue("@StorageId", document.StorageId ?? (object)DBNull.Value);
                        break;
                    case "ProductReceipt":
                        cmd.Parameters.AddWithValue("@SupplierId", document.SupplierId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@StorageId", document.StorageId ?? (object)DBNull.Value);
                        break;
                    case "MovementOfGoods":
                        cmd.Parameters.AddWithValue("@SenderStorageId", document.SenderStorageId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RecipientStorageId", document.RecipientStorageId ?? (object)DBNull.Value);
                        break;
                }

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }
        private async Task<int> AddNewDocumentItemForNewDocumentAsync(Document document, DocumentItem item,
                                                              string tableName, int documentId,
                                                              SqlConnection connection, SqlTransaction transaction)
        {
            int documentTypeId = GetDocumentTypeId(tableName);

            var insertTableProductQuery = @"
        INSERT INTO [TableProduct] 
        ([TableProductDocumentTypeID], [TableProductProductID], 
         [TableProductCharacteristicID], [TableProductCount], 
         [TableProductRemains], [TableProductUnitOfMeasurementID])
        VALUES 
        (@DocumentTypeId, @ProductId, @CharacteristicId, 
         @Count, @Remains, @UnitOfMeasurementId);
        SELECT SCOPE_IDENTITY();";

            int tableProductId;

            using (var insertCmd = new SqlCommand(insertTableProductQuery, connection, transaction))
            {
                insertCmd.Parameters.AddWithValue("@DocumentTypeId", documentTypeId);
                insertCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                insertCmd.Parameters.AddWithValue("@CharacteristicId", item.CharacteristicId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Count", item.Quantity);

                object remainsValue = documentTypeId switch
                {
                    0 or 1 => item.Quantity,
                    _ => (object)DBNull.Value
                };

                insertCmd.Parameters.AddWithValue("@Remains", remainsValue);
                insertCmd.Parameters.AddWithValue("@UnitOfMeasurementId", item.UnitOfMeasurementId ?? (object)DBNull.Value);

                tableProductId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                Console.WriteLine($"Создали TableProduct с ID: {tableProductId} для товара {item.ProductId}");
            }

            await LinkTableProductToDocumentAsync(document, documentId, tableProductId, tableName, connection, transaction);

            return tableProductId;
        }
        private async Task LinkTableProductToDocumentAsync(Document document, int documentId, int tableProductId,
                                                   string tableName, SqlConnection connection, SqlTransaction transaction)
        {
            string insertDocumentQuery = tableName switch
            {
                "SettingTheInitialBalances" => @"
            INSERT INTO [SettingTheInitialBalances] 
            ([SettingTheInitialBalancesDocumentTypeID], [SettingTheInitialBalancesDate],
             [SettingTheInitialBalancesResponsibleID], [SettingTheInitialBalancesStorageID],
             [SettingTheInitialBalancesTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @StorageId, @TableProductId)",

                "ProductReceipt" => @"
            INSERT INTO [ProductReceipt] 
            ([ProductReceiptDocumentTypeID], [ProductReceiptDate],
             [ProductReceiptSupplierID], [ProductReceiptResponsibleID],
             [ProductReceiptStorageID], [ProductReceiptTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @SupplierId, @ResponsibleId, 
             @StorageId, @TableProductId)",

                "MovementOfGoods" => @"
            INSERT INTO [MovementOfGoods] 
            ([MovementOfGoodsDocumentTypeID], [MovementOfGoodsDate],
             [MovementOfGoodsResponsibleID], [MovementOfGoodsSenderStorageID],
             [MovementOfGoodsResepientStorageID], [MovementOfGoodsTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @SenderStorageId,
             @RecipientStorageId, @TableProductId)",

                "WriteOffOfGoods" => @"
            INSERT INTO [WriteOffOfGoods] 
            ([WriteOffOfGoodsDocumentTypeID], [WriteOffOfGoodsDate],
             [WriteOffOfGoodsResponsibleID], [WriteOffOfGoodsTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @TableProductId)",

                "Inventory" => @"
            INSERT INTO [Inventory] 
            ([InventoryDocumentTypeID], [InventoryDate],
             [InventoryResponsibleID], [InventoryStorageID],
             [InventoryTableProductID])
            VALUES 
            (@DocumentTypeId, @Date, @ResponsibleId, @StorageId, @TableProductId)",

                _ => throw new ArgumentException($"Неизвестная таблица: {tableName}")
            };

            using (var docCmd = new SqlCommand(insertDocumentQuery, connection, transaction))
            {
                int documentTypeId = GetDocumentTypeId(tableName);

                docCmd.Parameters.AddWithValue("@DocumentTypeId", documentTypeId);
                docCmd.Parameters.AddWithValue("@Date", document.DocumentDate);
                docCmd.Parameters.AddWithValue("@ResponsibleId", document.ResponsibleId);
                docCmd.Parameters.AddWithValue("@TableProductId", tableProductId);

                switch (tableName)
                {
                    case "SettingTheInitialBalances":
                    case "Inventory":
                        if (document.StorageId.HasValue)
                            docCmd.Parameters.AddWithValue("@StorageId", document.StorageId.Value);
                        else
                            docCmd.Parameters.AddWithValue("@StorageId", DBNull.Value);
                        break;

                    case "ProductReceipt":
                        if (document.SupplierId.HasValue)
                            docCmd.Parameters.AddWithValue("@SupplierId", document.SupplierId.Value);
                        else
                            docCmd.Parameters.AddWithValue("@SupplierId", DBNull.Value);

                        if (document.StorageId.HasValue)
                            docCmd.Parameters.AddWithValue("@StorageId", document.StorageId.Value);
                        else
                            docCmd.Parameters.AddWithValue("@StorageId", DBNull.Value);
                        break;

                    case "MovementOfGoods":
                        if (document.SenderStorageId.HasValue)
                            docCmd.Parameters.AddWithValue("@SenderStorageId", document.SenderStorageId.Value);
                        else
                            docCmd.Parameters.AddWithValue("@SenderStorageId", DBNull.Value);

                        if (document.RecipientStorageId.HasValue)
                            docCmd.Parameters.AddWithValue("@RecipientStorageId", document.RecipientStorageId.Value);
                        else
                            docCmd.Parameters.AddWithValue("@RecipientStorageId", DBNull.Value);
                        break;
                }

                await docCmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Связали TableProduct {tableProductId} с документом {documentId} в таблице {tableName}");
            }
        }
    }
}