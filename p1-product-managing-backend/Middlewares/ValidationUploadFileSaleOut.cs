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
        string errorFields = "";
        int countErrors = 0;
        if (string.IsNullOrWhiteSpace(saleOut.CustomerPoNo))
        {
            errorFields += "- Customer PO No không được để trống<br/>";
            countErrors++;
        }

        if (saleOut.OrderDate <= 0)
        {
            errorFields += "- Ngày đơn hàng không hợp lệ<br/>";
            countErrors++;
        }

        if (string.IsNullOrWhiteSpace(saleOut.CustomerName))
        {
            errorFields += "- Tên khách hàng không được để trống<br/>";
            countErrors++;
        }

        if (string.IsNullOrWhiteSpace(saleOut.ProductCode))
        {
            errorFields += "- Mã sản phẩm không được để trống<br/>";
            countErrors++;
        }

        if (saleOut.Quantity <= 0)
        {
            errorFields += "- Số lượng phải lớn hơn 0<br/>";
            countErrors++;
        }

        if (saleOut.Price <= 0)
        {
            errorFields += "- Đơn giá không được âm<br/>";
            countErrors++;
        }

        if (saleOut.QuantityPerBox <= 0)
        {
            errorFields += "- Số lượng / thùng phải lớn hơn 0<br/>";
            countErrors++;
        }

        if (errorFields != "" && countErrors < 7 && countErrors > 0)
        {
            throw new Exception(errorFields);
        }
    }

    public async Task ValidateDuplicateProductAsync(
        string productCode,
        string customerPoNo,
        Dictionary<string, HashSet<string>> existedSaleOuts,
        int row
    )
    {
        if (string.IsNullOrWhiteSpace(productCode) && string.IsNullOrWhiteSpace(customerPoNo))
            return;
        if (!existedSaleOuts.ContainsKey(customerPoNo))
            existedSaleOuts[customerPoNo] = new HashSet<string>();

        if (!existedSaleOuts[customerPoNo].Add(productCode))
        {
            throw new Exception(
                $"Số PO {customerPoNo}, Mã sản phẩm {productCode} bị trùng trong file"
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
                $"Mã sản phẩm {productCode} không tồn tại trong hệ thống"
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
                $"Số PO {customerPoNo}, Mã sản phẩm {productCode} đã tồn tại trên hệ thống"
            );
        }
    }

    public void ValidateHeader(ExcelWorksheet ws)
    {
        string errorHeader = "";
        if (ws.Cells[1, 1].Text != "Số PO khách hàng")
            errorHeader += "- Sai template: thiếu cột Sô PO khách hàng<br/>";
        if (ws.Cells[1, 2].Text != "Ngày đặt hàng (yyyy/MM/dd)")
            errorHeader += "- Sai template: thiếu cột Ngày đặt hàng<br/>";
        if (ws.Cells[1, 3].Text != "Khách hàng")
            errorHeader += "- Sai template: thiếu cột Tên khách hàng<br/>";
        if (ws.Cells[1, 4].Text != "Mã sản phẩm")
            errorHeader += "- Sai template: thiếu cột Mã sản phẩm<br/>";
        if (ws.Cells[1, 5].Text != "Số lượng")
            errorHeader += "- Sai template: thiếu cột Số lượng<br/>";
        if (ws.Cells[1, 6].Text != "Số lượng/thùng")
            errorHeader += "- Sai template: thiếu cột Số lượng/Thùng<br/>";
        if (ws.Cells[1, 7].Text != "Đơn giá")
            errorHeader += "- Sai template: thiếu cột Đơn giá<br/>";
        if (errorHeader != "")
        {
            throw new Exception(errorHeader);
        }
    }
}
