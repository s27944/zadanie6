using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using zadanie6.Services;

namespace zadanie6.Controllers;

[ApiController]
[Route("magazyn")]
public class WarehouseController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private WarehouseService ws;

    public WarehouseController(IConfiguration configuration)
    {
        _configuration = configuration;
        this.ws = new WarehouseService(_configuration);
    }


    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse(CreateDataBaseRequest request)
    {
        int id = await ws.AddProductToWarehouse(request);
        if (id == 0)
        {
            return NotFound();  
        }

        return Created("/api/warehouse/" + id, null);
    }
}