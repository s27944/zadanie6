using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace zadanie6.Controllers;

[ApiController]
[Route("magazyn")]
public class WarehouseController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WarehouseController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }


    [HttpPost]
    public async Task<int> nazwijToTak(CreateDataBaseRequest request)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand(
            @"IF EXISTS (SELECT 1 FROM PRODUCT WHERE IdProduct = @1)
                      IF EXISTS (SELECT 1 FROM WAREHOUSE WHERE IdWareHouse = @2)
                        IF @3 < 0 OR @3 = 0 RETURN", connection
        );

        command.Parameters.AddWithValue("@1", request.IdProduct);
        command.Parameters.AddWithValue("@2", request.IdWarehouse);
        command.Parameters.AddWithValue("@3", request.Amount);

        

        var reader = await command.ExecuteReaderAsync();

        // if (!reader.HasRows)
        // {
        //     return 0;
        // }


        return 2;
    }
}