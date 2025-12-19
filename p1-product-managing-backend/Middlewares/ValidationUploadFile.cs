using Dapper;
using OfficeOpenXml;

public class ValidationUploadFile
{
    private readonly DapperContext _context;
    public ValidationUploadFile(DapperContext context)
    {
        _context = context;
    }
    public void ValidateRequiredFields(MasterProduct product, int row)
    {
        if (string.IsNullOrWhiteSpace(product.ProductCode))
            throw new Exception("Mã sản phẩm không được để trống");

        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new Exception("Tên sản phẩm không được để trống");

        if (string.IsNullOrWhiteSpace(product.Unit))
            throw new Exception("Đơn vị tính không được để trống");

        if (string.IsNullOrWhiteSpace(product.Specification))
            throw new Exception("Quy cách không được để trống");

        if (product.QuantityPerBox <= 0)
            throw new Exception("Số lượng/thùng không được để trống và phải lớn hơn 0");

        if (product.ProductWeight <= 0)
            throw new Exception("Trọng lượng không được để trống và phải lớn hơn 0");
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
        if (ws.Cells[1, 1].Text != "Mã sản phẩm")
            throw new Exception("Sai template: thiếu cột Mã sản phẩm");
        if (ws.Cells[1, 2].Text != "Tên sản phẩm")
            throw new Exception("Sai template: thiếu cột Tên sản phẩm");
        if (ws.Cells[1, 3].Text != "Đơn vị tính")
            throw new Exception("Sai template: thiếu cột Đơn vị tính");
        if (ws.Cells[1, 4].Text != "Quy cách")
            throw new Exception("Sai template: thiếu cột Quy cách");
        if (ws.Cells[1, 5].Text != "Số lượng/Thùng")
            throw new Exception("Sai template: thiếu cột Số lượng/Thùng");
        if (ws.Cells[1, 6].Text != "Trọng lượng")
            throw new Exception("Sai template: thiếu cột Trọng lượng");
    }

}
