using System.Data;
using System.Data.SqlClient;

namespace zadanie6.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouse(CreateDataBaseRequest request);
}

public class WarehouseService(IConfiguration configuration) : IWarehouseService
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }


    public async Task<int> AddProductToWarehouse(CreateDataBaseRequest request)
    {
        await using var connection = await GetConnection();
        await using var transaction = await connection.BeginTransactionAsync();
        int orderId = 0;
        decimal price = 0;
        int insertedId = 0;
        
        try
        {
            //sprawdzenie Amount
            if (request.Amount <= 0)
            {
                throw new Exception("Amount nie może być 0 lub mniejsze");
            }

            //sprawdzenie IdProductu
            await using (var command1 = new SqlCommand(
                             "SELECT Price FROM PRODUCT WHERE IdProduct = @1",
                             connection, (SqlTransaction)transaction))
            {
                command1.Parameters.AddWithValue("@1", request.IdProduct);
                await using var reader = await command1.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    throw new Exception("Nie ma takiego produktu!");
                }

                
                while (await reader.ReadAsync())
                {
                    price = reader.GetDecimal(0);
                }

                Console.WriteLine(price);
            }

            //==================================================
            //sprawdzenie IdWarehousu
            await using (var command2 = new SqlCommand(
                             "SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @1", connection,
                             (SqlTransaction)transaction))
            {
                command2.Parameters.AddWithValue("@1", request.IdWarehouse);
                await using var reader = await command2.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    bool warehouseExists = reader.GetInt32(0) != 0;
                    if (!warehouseExists)
                    {
                        throw new Exception("Nie ma takiego magazynu!");
                    }
                }
            }

            //2 zadanie =========================================
            await using (var command3 = new SqlCommand(
                             "SELECT IdOrder FROM [Order] WHERE IdProduct = @1 AND Amount = @2 AND CreatedAt < @3",
                             connection,
                             (SqlTransaction)transaction))
            {
                command3.Parameters.AddWithValue("@1", request.IdProduct);
                command3.Parameters.AddWithValue("@2", request.Amount);
                command3.Parameters.AddWithValue("@3", request.CreatedAt);

                await using var reader = await command3.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    throw new Exception("Nie ma takiego zamówienia lub data utworzenia zamówienia nie jest wcześniejsza.");
                }


                if (await reader.ReadAsync())
                {
                    bool orderExists = reader.GetInt32(0) != 0;
                    if (orderExists)
                    {
                        orderId = reader.GetInt32(0);
                    }
                }
            }

            //================================
            //zadanie 3
            await using (var command4 = new SqlCommand(
                             "SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @1", connection,
                             (SqlTransaction)transaction))
            {
                command4.Parameters.AddWithValue("@1", orderId);

                await using var reader = await command4.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    bool orderCompleted = reader.GetInt32(0) == 1;
                    if (orderCompleted)
                    {
                        throw new Exception("Zamówienie zostało zrealizowane!");
                    }
                }
            }

            //================================
            //zadanie 4
            await using (var command5 = new SqlCommand(
                             "UPDATE [Order] SET FulfilledAt = SYSDATETIME() WHERE IdOrder = @1", connection,
                             (SqlTransaction)transaction))
            {
                command5.Parameters.AddWithValue("@1", orderId);

                await command5.ExecuteNonQueryAsync();
            }

            //================================
            //zadanie 5

            //INSERTING ====================
            var command6 = new SqlCommand(
                "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)" +
                " OUTPUT INSERTED.IdProductWarehouse " +
                "VALUES (@1, @2, @3, @4, @5, @6)"
                , connection,
                (SqlTransaction)transaction);
            
            command6.Parameters.AddWithValue("@1", request.IdWarehouse);
            command6.Parameters.AddWithValue("@2", request.IdProduct);
            command6.Parameters.AddWithValue("@3", orderId);
            command6.Parameters.AddWithValue("@4", request.Amount);
            command6.Parameters.AddWithValue("@5", request.Amount * price);
            command6.Parameters.AddWithValue("@6", DateTime.Now);

            insertedId = (int) (await command6.ExecuteScalarAsync())!;
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return insertedId;
    }
}