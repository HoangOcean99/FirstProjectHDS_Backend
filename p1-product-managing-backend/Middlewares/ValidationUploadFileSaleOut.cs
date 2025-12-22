using Dapper;
using OfficeOpenXml;

public class ValidationUploadFileSaleOut
{
    private readonly DapperContext _context;

    public ValidationUploadFileSaleOut(DapperContext context)
    {
        _context = context;
    }

    public void ValidateRequiredFields(SaleOut saleOut, int row)
    {
        if (string.IsNullOrWhiteSpace(saleOut.CustomerPoNo))
            throw new Exception($"Dòng {row}: Customer PO No không được để trống");

        if (saleOut.OrderDate <= 0)
            throw new Exception($"Dòng {row}: Ngày đơn hàng không hợp lệ");

        if (string.IsNullOrWhiteSpace(saleOut.CustomerName))
            throw new Exception($"Dòng {row}: Tên khách hàng không được để trống");

        if (string.IsNullOrWhiteSpace(saleOut.ProductCode))
            throw new Exception($"Dòng {row}: Mã sản phẩm không được để trống");

        if (saleOut.Quantity <= 0)
            throw new Exception($"Dòng {row}: Số lượng phải lớn hơn 0");

        if (saleOut.Price < 0)
            throw new Exception($"Dòng {row}: Đơn giá không được âm");

        if (saleOut.QuantityPerBox <= 0)
            throw new Exception($"Dòng {row}: Số lượng / thùng phải lớn hơn 0");

    }

    public async Task ValidateDuplicateProductAsync(
    string productCode,
    string customerPoNo,
    Dictionary<string, HashSet<string>> existedSaleOuts,
    int row
)
    {
        // ===== Check trùng trong file =====
        if (!existedSaleOuts.ContainsKey(customerPoNo))
            existedSaleOuts[customerPoNo] = new HashSet<string>();

        if (!existedSaleOuts[customerPoNo].Add(productCode))
        {
            throw new Exception(
                $"Dòng {row}: Số PO {customerPoNo}, Mã sản phẩm {productCode} bị trùng trong file"
            );
        }

        using var conn = _context.CreateConnection();

        // ===== Lấy ProductId =====
        var selectSql = """
        SELECT Id
        FROM MasterProduct
        WHERE ProductCode = @productCode
    """;

        var productId = await conn.QuerySingleOrDefaultAsync<Guid?>(
            selectSql,
            new { productCode }
        );

        if (productId == null)
        {
            throw new Exception(
                $"Dòng {row}: Mã sản phẩm {productCode} không tồn tại trong hệ thống"
            );
        }

        // ===== Check trùng DB =====
        var checkSql = """
        SELECT COUNT(1)
        FROM SaleOut
        WHERE CustomerPoNo = @customerPoNo
          AND ProductId = @productId
    """;

        var count = await conn.ExecuteScalarAsync<int>(
            checkSql,
            new { customerPoNo, productId }
        );

        if (count > 0)
        {
            throw new Exception(
                $"Dòng {row}: Số PO {customerPoNo}, Mã sản phẩm {productCode} đã tồn tại trên hệ thống"
            );
        }
    }



    public void ValidateHeader(ExcelWorksheet ws)
    {
        if (ws.Cells[1, 1].Text != "Số PO khách hàng")
            throw new Exception("Sai template: thiếu cột Sô PO khách hàng");
        if (ws.Cells[1, 2].Text != "Ngày đặt hàng")
            throw new Exception("Sai template: thiếu cột Ngày đặt hàng");
        if (ws.Cells[1, 3].Text != "Khách hàng")
            throw new Exception("Sai template: thiếu cột Tên khách hàng");
        if (ws.Cells[1, 4].Text != "Mã sản phẩm")
            throw new Exception("Sai template: thiếu cột Mã sản phẩm");
        if (ws.Cells[1, 5].Text != "Số lượng")
            throw new Exception("Sai template: thiếu cột Số lượng");
        if (ws.Cells[1, 6].Text != "Số lượng/thùng")
            throw new Exception("Sai template: thiếu cột Số lượng/Thùng");
        if (ws.Cells[1, 7].Text != "Đơn giá")
            throw new Exception("Sai template: thiếu cột Đơn giá");
    }
}
