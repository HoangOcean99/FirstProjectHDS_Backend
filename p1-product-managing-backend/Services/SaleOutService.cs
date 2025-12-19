

using Dapper;
using Microsoft.Data.SqlClient;

public class SaleOutService : ISaleOutService
{
    private readonly DapperContext _context;
    public SaleOutService(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SaleOut>> GetAll()
    {
        var sql = """
            select 
                s.Id,
                s.CustomerPoNo,
                s.OrderDate,
                s.CustomerName,
                p.ProductCode,
                p.ProductName,
                p.Unit,
                s.Quantity,
                s.Price,
                s.Amount,
                s.QuantityPerBox,
                s.BoxQuantity
            from SaleOut s
            join MasterProduct p on s.ProductId = p.Id
            order by s.CustomerPoNo
        """;
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<SaleOut>(sql);
    }
    public async Task<IEnumerable<SaleOut>> addMasterProduct(SaleOut saleOut)
    {
        var insertSql = """
            INSERT INTO MasterProduct (CustomerPoNo, OrderDate, CustomerName,
            ProductId, Quantity, Price, Amount, QuantityPerBox, BoxQuantity)
            OUTPUT INSERTED.id
            VALUES
                (@CustomerPoNo, @OrderDate, @CustomerName,
                @ProductId, @Quantity, @Price, @Amount, @QuantityPerBox, @BoxQuantity)
        """;

        using var conn = _context.CreateConnection();
        Guid idInsert;
        try
        {
            idInsert = await conn.QuerySingleAsync<Guid>(insertSql, saleOut);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            throw new DuplicateWaitObjectException("Số PO khách hàng: " + saleOut.CustomerPoNo +, ex);
        }

        var selectSql = """
            SELECT Id, ProductCode, ProductName, Unit, 
                Specification, QuantityPerBox, ProductWeight
            FROM MasterProduct
            WHERE Id = @Id 
        """;

        var productInserted = await conn.QueryAsync<MasterProduct>(selectSql, new { Id = idInsert });
        if (productInserted == null)
        {
            throw new Exception("Không thể truy xuất sản phẩm vừa được tạo");
        }
        return productInserted;
    }