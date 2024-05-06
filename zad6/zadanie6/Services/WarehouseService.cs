using System.Data;
using System.Data.SqlClient;

namespace zadanie6.Services;

public interface IWarehouseService
{
    Task<int?> AddProductToWarehouse(CreateDataBaseRequest request);
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


    public async Task<int?> AddProductToWarehouse(CreateDataBaseRequest request)
    {
        await using var connection = await GetConnection();

        var transaction = await connection.BeginTransactionAsync();
        try
        {
            //sprawdzenie Amount
            if (request.Amount <= 0)
            {
                throw new Exception("Amount nie może być 0 lub mniejsze");
            }

            //sprawdzenie IdProductu
            var command1 = new SqlCommand(
                "SELECT Price FROM PRODUCT WHERE IdProduct = @1",
                connection, (SqlTransaction)transaction);

            command1.Parameters.AddWithValue("@1", request.IdProduct);
            var reader = await command1.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new Exception(
                    "Nie ma takiego produktu!");
            }

            double price = 0.0;
            await reader.ReadAsync();
            do
            {
                price = reader.GetInt32(0);
                reader.Close();
            } while (await reader.ReadAsync());

            reader.Close();
            Console.WriteLine(price);
            //==================================================
            //sprawdzenie IdWarehousu
            var command2 = new SqlCommand(
                "SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @1", connection, (SqlTransaction)transaction);

            command2.Parameters.AddWithValue("@1", request.IdWarehouse);

            reader = await command2.ExecuteReaderAsync();

            await reader.ReadAsync();
            do
            {
                bool warehouseExists = reader.GetInt32(0) != 0;
                if (!warehouseExists)
                {
                    reader.Close();
                    throw new Exception(
                        "Nie ma takiego magazynu!");
                }
            } while (await reader.ReadAsync());

            reader.Close();

            //2 zadanie =========================================

            var command3 = new SqlCommand(
                "SELECT IdOrder FROM [Order] WHERE IdProduct = @1 AND Amount = @2 AND CreatedAt < @3", connection,
                (SqlTransaction)transaction);

            command3.Parameters.AddWithValue("@1", request.IdProduct);
            command3.Parameters.AddWithValue("@2", request.Amount);
            command3.Parameters.AddWithValue("@3", request.CreatedAt);


            reader = await command3.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new Exception("Nie ma takiego zamówienia lub data utworzenia zamowienia nie jest wczeniejsza.");
            }

            int orderId = 0;
            if (await reader.ReadAsync())
            {
                bool orderExists = reader.GetInt32(0) != 0;
                if (orderExists)
                {
                    orderId = reader.GetInt32(0);
                }
            }

            reader.Close();

            //================================
            //zadanie 3

            var command4 = new SqlCommand(
                "SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder = @1", connection,
                (SqlTransaction)transaction);

            command4.Parameters.AddWithValue("@1", orderId);


            reader = await command4.ExecuteReaderAsync();


            if (await reader.ReadAsync())
            {
                bool orderCompleted = reader.GetInt32(0) == 1;
                if (orderCompleted)
                {
                    reader.Close();
                    throw new Exception(
                        "Zamówienie zostało zrealizowane!");
                }
            }

            reader.Close();

            //================================
            //zadanie 4

            var command5 = new SqlCommand(
                "UPDATE [Order] SET FulfilledAt = SYSDATETIME() WHERE IdOrder = @1", connection,
                (SqlTransaction)transaction);

            command5.Parameters.AddWithValue("@1", orderId);
            
            
            reader = await command5.ExecuteReaderAsync();

            reader.Close();


            //================================
            //zadanie 5
            

            //INSERTING ====================
            // var command7 = new SqlCommand(
            //     "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES"
            //     , connection,
            //     (SqlTransaction)transaction);
            //
            // command7.Parameters.AddWithValue("@1", orderId);

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }


        return request.IdProduct;
    }
}