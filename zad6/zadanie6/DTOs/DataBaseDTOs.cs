using System.ComponentModel.DataAnnotations;

namespace zadanie6;

public record CreateDataBaseRequest(
    [Required] int IdProduct,
    [Required] int IdWarehouse,
    [Required] int Amount,
    [Required] DateTime CreatedAt 
);

public record CreateDataBaseResponse(int IdProductWarehouse, int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt)
{
    public CreateDataBaseResponse(int IdProductWarehouse, CreateDataBaseRequest request) : this(IdProductWarehouse, request.IdProduct, request.IdWarehouse, request.Amount, request.CreatedAt) { }
}