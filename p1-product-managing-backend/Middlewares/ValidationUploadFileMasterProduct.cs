using Dapper;
using OfficeOpenXml;

public class ValidationUploadFileMasterProduct
{
    private readonly DapperContext _context;
    public ValidationUploadFileMasterProduct(DapperContext context)
    {
        _context = context;
    }

    public void ValidateRequiredFields(MasterProduct product, int row)
    {
        string errorFields = "";
        int countErrors = 0;
        if (string.IsNullOrWhiteSpace(product.ProductCode))
        {
            errorFields += "- Mã sản phẩm không được để trống<br/>";
            countErrors++;
        }

        if (string.IsNullOrWhiteSpace(product.ProductName))
        {
            errorFields += "- Tên sản phẩm không được để trống<br/>";
            countErrors++;
        }

        if (string.IsNullOrWhiteSpace(product.Unit))
        {
            errorFields += "- Đơn vị tính không được để trống<br/>";
            countErrors++;
        }

        if (string.IsNullOrWhiteSpace(product.Specification))
        {
            errorFields += "- Quy cách không được để trống<br/>";
            countErrors++;
        }

        if (product.QuantityPerBox <= 0)
        {
            errorFields += "- Số lượng/thùng không được để trống và phải lớn hơn 0<br/>";
            countErrors++;
        }

        if (product.ProductWeight <= 0)
        {
            errorFields += "- Trọng lượng không được để trống và phải lớn hơn 0<br/>";
            countErrors++;
        }

        if (errorFields != "" && countErrors < 6 && countErrors > 0)
        {
            throw new Exception(errorFields);
        }
    }


    public async Task ValidateDuplicateCodeAsync(
        string productCode,
        HashSet<string> existedCodes,
        int row
    )
    {
        // Trùng trong file
        if (existedCodes.Contains(productCode))
            throw new Exception($"Mã sản phẩm '{productCode}' bị trùng trong file");

        // Trùng trong DB
        var sql = "SELECT COUNT(1) FROM MasterProduct WHERE ProductCode = @Code";
        using var conn = _context.CreateConnection();

        var count = await conn.ExecuteScalarAsync<int>(
            sql,
            new { Code = productCode }
        );

        if (count > 0)
            throw new Exception($"Mã sản phẩm '{productCode}' đã tồn tại trong hệ thống");
    }

    public void ValidateHeader(ExcelWorksheet ws)
    {
        string errorHeader = "";
        if (ws.Cells[1, 1].Text != "Mã sản phẩm")
            errorHeader += "- Sai template: thiếu cột Mã sản phẩm<br/>";
        if (ws.Cells[1, 2].Text != "Tên sản phẩm")
            errorHeader += "- Sai template: thiếu cột Tên sản phẩm<br/>";
        if (ws.Cells[1, 3].Text != "Đơn vị tính")
            errorHeader += "- Sai template: thiếu cột Đơn vị tính<br/>";
        if (ws.Cells[1, 4].Text != "Quy cách")
            errorHeader += "- Sai template: thiếu cột Quy cách<br/>";
        if (ws.Cells[1, 5].Text != "Số lượng/Thùng")
            errorHeader += "- Sai template: thiếu cột Số lượng/Thùng<br/>";
        if (ws.Cells[1, 6].Text != "Trọng lượng")
            errorHeader += "- Sai template: thiếu cột Trọng lượng<br/>";
        if (errorHeader != "")
        {
            throw new Exception(errorHeader);
        }
    }

}
