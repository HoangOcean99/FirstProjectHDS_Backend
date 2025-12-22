

using System.Net.Http.Headers;
using Dapper;
using Microsoft.Data.SqlClient;

public class MasterProductService : IMasterProductService
{
    private readonly DapperContext _context;
    
    public MasterProductService(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MasterProduct>> GetAll()
    {
        var sql = """
            SELECT Id, ProductCode, ProductName, Unit, 
                Specification, QuantityPerBox, ProductWeight
            FROM MasterProduct
            ORDER BY ProductCode
        """;
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<MasterProduct>(sql);
    }
    
    public async Task<IEnumerable<MasterProduct>> addMasterProduct(MasterProduct masterProduct)
    {
        var insertSql = """
            INSERT INTO MasterProduct (ProductCode, ProductName, Unit, 
                Specification, QuantityPerBox, ProductWeight)
            OUTPUT INSERTED.id
            VALUES
                (@ProductCode, @ProductName, @Unit, 
                @Specification, @QuantityPerBox, @ProductWeight)
        """;

        using var conn = _context.CreateConnection();
        Guid idInsert;
        try
        {
            idInsert = await conn.QuerySingleAsync<Guid>(insertSql, masterProduct);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            throw new DuplicateWaitObjectException("Mã " + masterProduct.ProductCode + " đã tồn tại", ex);
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

    public async Task<bool> deleteMasterProduct(Guid Id)
    {
        var sql = """
            DELETE FROM MasterProduct
            WHERE Id = @Id
        """;
        using var conn = _context.CreateConnection();
        int affectedRows = await conn.ExecuteAsync(sql, new { Id = Id });
        return (affectedRows > 0);
    }

    public async Task<bool> editMasterProduct(MasterProduct masterProduct)
    {
        var editSql = """
                Update MasterProduct 
                SET
                    ProductCode = @ProductCode,
                    ProductName = @ProductName,
                    Unit = @Unit,
                    Specification = @Specification,
                    QuantityPerBox = @QuantityPerBox,
                    ProductWeight = @ProductWeight
                WHERE Id = @Id
            """;

        using var conn = _context.CreateConnection();
        try
        {
            int affectRows = await conn.ExecuteAsync(editSql, masterProduct);
            return (affectRows > 0);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            throw new DuplicateWaitObjectException("Mã sản phẩm đã tồn tại", ex);
        }
    }
}