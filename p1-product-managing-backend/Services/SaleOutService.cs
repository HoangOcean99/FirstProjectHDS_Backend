

using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.FormulaExpressions.CompileResults;

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
    public async Task<PagedResult<SaleOut>> GetPagedAsync(
        int pageIndex,
        int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;

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
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM SaleOut;
        """;

        using var conn = _context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(sql, new
        {
            Offset = offset,
            PageSize = pageSize
        });

        return new PagedResult<SaleOut>
        {
            Items = await multi.ReadAsync<SaleOut>(),
            Total = await multi.ReadSingleAsync<int>()
        };
    }

    public async Task<SaleOut> AddSaleOutAsync(SaleOut saleOut, string saleOutNo = "")
    {
        using var conn = _context.CreateConnection();

        if (string.IsNullOrEmpty(saleOutNo))
            saleOutNo = await GenerateSaleOutNoAsync();

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
            ProductId, Quantity, Price, QuantityPerBox, BoxQuantity, SaleOutNo
        )
        OUTPUT INSERTED.Id
        VALUES (
            @CustomerPoNo, @OrderDate, @CustomerName,
            @ProductId, @Quantity, @Price, @QuantityPerBox, @BoxQuantity, @saleOutNo
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
                    saleOut.BoxQuantity,
                    saleOutNo
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

    public async Task<string> GenerateSaleOutNoAsync()
    {
        using var conn = _context.CreateConnection();
        var now = DateTime.Now;
        var prefix = $"STO{now:yyyyMM}";

        var selectSql = @"SELECT TOP 1 SaleOutNo 
          FROM SaleOut 
          WHERE SaleOutNo LIKE @Prefix + '%'
          ORDER BY SaleOutNo DESC";
        var lastNo = await conn.QueryFirstOrDefaultAsync<string>(
            selectSql,
            new { Prefix = prefix });

        int next = 1;

        if (!string.IsNullOrEmpty(lastNo))
        {
            var numberPart = lastNo.Substring(prefix.Length);
            next = int.Parse(numberPart) + 1;
        }

        return $"{prefix}{next:D4}";
    }

    public async Task<IEnumerable<string>> getAllSaleOutNo()
    {
        var sql = """
            SELECT DISTINCT SaleOutNo
            FROM SaleOut
            ORDER BY SaleOutNo DESC
        """;
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<string>(sql);
    }
    public async Task<List<SaleOutPdf>> getSaleOutByNo(string saleOutNo)
    {
        var sql = """
            SELECT
                p.ProductCode,
                p.ProductName,
                SUM(s.Quantity) AS Quantity,
                s.Price,
                SUM(s.Amount) AS Amount,
                s.CustomerName,
                s.OrderDate
            FROM MasterProduct p
            JOIN SaleOut s ON s.ProductId = p.Id
            WHERE s.SaleOutNo = @SaleOutNo
            GROUP BY
                p.ProductCode,
                p.ProductName,
                s.Price,
                s.CustomerName,
                s.OrderDate
        """;
        using var conn = _context.CreateConnection();
        return (await conn.QueryAsync<SaleOutPdf>(sql, new { SaleOutNo = saleOutNo })).ToList();
    }

}