

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
            order by s.CustomerPoNo, p.ProductCode
        """;
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<SaleOut>(sql);
    }
    
    public async Task<SaleOut> AddSaleOutAsync(SaleOut saleOut)
    {
        using var conn = _context.CreateConnection();

        var selectProductSql = """
            SELECT Id
            FROM MasterProduct
            WHERE ProductCode = @ProductCode
        """;

        Guid productId;
        try
        {
            productId = await conn.QuerySingleAsync<Guid>(
                selectProductSql,
                new { saleOut.ProductCode }
            );
        }
        catch
        {
            throw new Exception("Không tìm thấy sản phẩm");
        }

        var insertSql = """
        INSERT INTO SaleOut (
            CustomerPoNo, OrderDate, CustomerName,
            ProductId, Quantity, Price, QuantityPerBox, BoxQuantity
        )
        OUTPUT INSERTED.Id
        VALUES (
            @CustomerPoNo, @OrderDate, @CustomerName,
            @ProductId, @Quantity, @Price, @QuantityPerBox, @BoxQuantity
        )
    """;
        Guid idInsert;
        try
        {
            idInsert = await conn.QuerySingleAsync<Guid>(
                insertSql,
                new
                {
                    saleOut.CustomerPoNo,
                    saleOut.OrderDate,
                    saleOut.CustomerName,
                    ProductId = productId,
                    saleOut.Quantity,
                    saleOut.Price,
                    saleOut.QuantityPerBox,
                    saleOut.BoxQuantity
                }
            );
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            throw new DuplicateSQLException(
                $"Số PO khách hàng: {saleOut.CustomerPoNo}; Mã sản phẩm: {saleOut.ProductCode} đã có trên hệ thống",
                StatusCodes.Status409Conflict
            );

        }


        var selectSql = """
        SELECT *
        FROM SaleOut
        WHERE Id = @Id
    """;

        return await conn.QuerySingleAsync<SaleOut>(selectSql, new { Id = idInsert });
    }
    
    public async Task<bool> deleteSaleOut(Guid Id)
    {
        var sql = """
            DELETE FROM SaleOut
            WHERE Id = @Id
        """;
        using var conn = _context.CreateConnection();
        int affectedRows = await conn.ExecuteAsync(sql, new { Id = Id });
        return (affectedRows > 0);
    }

    public async Task<bool> editSaleOut(SaleOut saleOut)
    {
        using var conn = _context.CreateConnection();

        var selectProductSql = """
            SELECT Id
            FROM MasterProduct
            WHERE ProductCode = @ProductCode
        """;

        Guid productId;
        try
        {
            productId = await conn.QuerySingleAsync<Guid>(
                selectProductSql,
                new { saleOut.ProductCode }
            );
        }
        catch
        {
            throw new Exception("Không tìm thấy sản phẩm");
        }
        var editSql = """
                Update SaleOut 
                SET
                    ProductId = @ProductId,
                    Quantity = @Quantity,
                    Price = @Price,
                    QuantityPerBox = @QuantityPerBox,
                    BoxQuantity = @BoxQuantity
                WHERE Id = @Id
            """;

        try
        {
            int affectRows = await conn.ExecuteAsync(editSql, new
            {
                ProductId = productId,
                saleOut.Quantity,
                saleOut.Price,
                saleOut.Amount,
                saleOut.QuantityPerBox,
                saleOut.BoxQuantity,
                saleOut.Id
            });
            return (affectRows > 0);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            throw new DuplicateSQLException(
                $"Số PO khách hàng: {saleOut.CustomerPoNo}; Mã sản phẩm: {saleOut.ProductCode} đã có trên hệ thống",
                StatusCodes.Status409Conflict
            );
        }
    }
}