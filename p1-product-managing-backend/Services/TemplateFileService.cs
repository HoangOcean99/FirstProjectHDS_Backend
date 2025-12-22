
using System.Drawing;
using Dapper;
using OfficeOpenXml;

public class TemplateFileService : ITemplateFileService
{
    private readonly ValidationUploadFileMasterProduct _validationUploadFileMasterProduct;
    private readonly ValidationUploadFileSaleOut _validationUploadFileSaleOut;

    private readonly IMasterProductService _masterProductService;
    private readonly ISaleOutService _saleOutService;
    private readonly DapperContext _context;


    public TemplateFileService(
        DapperContext context,
        ValidationUploadFileMasterProduct validationUploadMasterProduct,
        ValidationUploadFileSaleOut validationUploadSaleOut,
        IMasterProductService masterProductService,
        ISaleOutService saleOutService)
    {
        _context = context;
        _validationUploadFileSaleOut = validationUploadSaleOut;
        _validationUploadFileMasterProduct = validationUploadMasterProduct;
        _masterProductService = masterProductService;
        _saleOutService = saleOutService;
    }

    public async Task<byte[]> DownloadSaleOutReport(
    int startDate,
    int endDate)
    {
        var data = await GetSaleOutReportAsync(startDate, endDate);

        var license = new EPPlusLicense();
        license.SetNonCommercialOrganization("Personal");

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("SaleOutReport");

        string[] columns =
        {
            "Mã sản phẩm",
            "Tên sản phẩm",
            "Số lượng",
            "Đơn giá",
            "Thành tiền"
        };

        for (int i = 0; i < columns.Length; i++)
            ws.Cells[1, i + 1].Value = columns[i];

        int row = 2;
        foreach (var item in data)
        {
            ws.Cells[row, 1].Value = item.ProductCode;
            ws.Cells[row, 2].Value = item.ProductName;
            ws.Cells[row, 3].Value = item.Quantity;
            ws.Cells[row, 4].Value = item.Price;
            ws.Cells[row, 5].Value = item.Amount;
            row++;
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        ws.Cells[1, 1, 1, 5].Style.Font.Bold = true;

        using (var stream = new MemoryStream())
        {
            await package.SaveAsAsync(stream);
            stream.Position = 0;
            return stream.ToArray();
        }
    }

    public async Task<List<SaleOutReport>> GetSaleOutReportAsync(
        int startDate,
        int endDate)
    {
        using var conn = _context.CreateConnection();

        var sql = """
            SELECT *
            FROM fnSaleOutReport(@StartDate, @EndDate)
            ORDER BY ProductCode ASC
        """;
        var data = await conn.QueryAsync<SaleOutReport>(
            sql,
            new { StartDate = startDate, EndDate = endDate }
        );
        return data.ToList();
    }

    public async Task<byte[]> GenerateExcelTemplateAsync(List<string> columns)
    {
        var license = new EPPlusLicense();
        license.SetNonCommercialOrganization("Personal");
        using (var package = new ExcelPackage())
        {
            var ws = package.Workbook.Worksheets.Add("Template");
            for (int i = 0; i < columns.Count; i++)
            {
                ws.Cells[1, i + 1].Value = columns[i];
            }
            using (var stream = new MemoryStream())
            {
                await package.SaveAsAsync(stream);
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }


    public async Task<ImportResult> importExcelTemplateMasterProduct(IFormFile file)
    {
        var result = new ImportResult();

        var license = new EPPlusLicense();
        license.SetNonCommercialOrganization("Personal");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets[0];
        try
        {
            _validationUploadFileMasterProduct.ValidateHeader(ws);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lỗi header: {ex.Message}");
            return result;
        }
        int rowCount = ws.Dimension.End.Row;

        HashSet<string> existedCodes = new HashSet<string>();
        List<MasterProduct> validProducts = new List<MasterProduct>();

        for (int i = 2; i <= rowCount; i++)
        {
            try
            {
                var product = ParseRowMasterProduct(ws, i);

                _validationUploadFileMasterProduct.ValidateRequiredFields(product, i);

                await _validationUploadFileMasterProduct.ValidateDuplicateCodeAsync(
                    product.ProductCode,
                    existedCodes,
                    i
                );

                existedCodes.Add(product.ProductCode);
                validProducts.Add(product);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Dòng {i}: {ex.Message}");
            }
        }

        if (result.Errors.Any())
            return result;

        foreach (var product in validProducts)
        {
            await _masterProductService.addMasterProduct(product);
            result.InsertedRow++;
        }

        return result;
    }

    public async Task<ImportResult> importExcelTemplateSaleOut(IFormFile file)
    {
        var result = new ImportResult();

        var license = new EPPlusLicense();
        license.SetNonCommercialOrganization("Personal");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        using var package = new ExcelPackage(stream);
        var ws = package.Workbook.Worksheets[0];
        try
        {
            _validationUploadFileSaleOut.ValidateHeader(ws);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lỗi header: {ex.Message}");
            return result;
        }
        int rowCount = ws.Dimension.End.Row;

        Dictionary<string, HashSet<string>> existedSaleOuts = new Dictionary<string, HashSet<string>>();
        List<SaleOut> validSaleOuts = new List<SaleOut>();

        for (int i = 2; i <= rowCount; i++)
        {
            try
            {
                var saleOut = ParseRowSaleOut(ws, i);

                _validationUploadFileSaleOut.ValidateRequiredFields(saleOut, i);

                await _validationUploadFileSaleOut.ValidateDuplicateProductAsync(
                    saleOut.ProductCode,
                    saleOut.CustomerPoNo,
                    existedSaleOuts,
                    i
                );

                validSaleOuts.Add(saleOut);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Dòng {i}: {ex.Message}");
            }
        }


        if (result.Errors.Any())
            return result;

        foreach (var saleOut in validSaleOuts)
        {
            await _saleOutService.AddSaleOutAsync(saleOut);
            result.InsertedRow++;
        }

        return result;
    }

    private MasterProduct ParseRowMasterProduct(ExcelWorksheet ws, int row)
    {
        if (!decimal.TryParse(ws.Cells[row, 5].Text, out decimal QuantityPerBox))
            QuantityPerBox = 0;
        if (!decimal.TryParse(ws.Cells[row, 6].Text, out decimal ProductWeight))
            ProductWeight = 0;

        return new MasterProduct
        {
            ProductCode = ws.Cells[row, 1].Text?.Trim(),
            ProductName = ws.Cells[row, 2].Text?.Trim(),
            Unit = ws.Cells[row, 3].Text?.Trim(),
            Specification = ws.Cells[row, 4].Text?.Trim(),
            QuantityPerBox = QuantityPerBox,
            ProductWeight = ProductWeight
        };
    }

    private SaleOut ParseRowSaleOut(ExcelWorksheet ws, int row)
    {
        if (!decimal.TryParse(ws.Cells[row, 5].Text, out var quantity))
            quantity = 0;

        if (!decimal.TryParse(ws.Cells[row, 6].Text, out var quantityPerBox))
            quantityPerBox = 0;

        if (!decimal.TryParse(ws.Cells[row, 7].Text, out var price))
            price = 0;

        int orderDate = 0;
        var cell = ws.Cells[row, 2];

        if (cell.Value is DateTime dt)
        {
            orderDate = int.Parse(dt.ToString("yyyyMMdd"));
        }
        else if (double.TryParse(cell.Value?.ToString(), out var oaDate))
        {
            var excelDate = DateTime.FromOADate(oaDate);
            orderDate = int.Parse(excelDate.ToString("yyyyMMdd"));
        }
        else if (DateTime.TryParse(cell.Text, out var textDate))
        {
            orderDate = int.Parse(textDate.ToString("yyyyMMdd"));
        }


        return new SaleOut
        {
            CustomerPoNo = ws.Cells[row, 1].Text?.Trim(),
            OrderDate = orderDate,
            CustomerName = ws.Cells[row, 3].Text?.Trim(),
            ProductCode = ws.Cells[row, 4].Text?.Trim(),
            Quantity = quantity,
            QuantityPerBox = quantityPerBox,
            BoxQuantity = quantityPerBox > 0 ? quantity / quantityPerBox : 0,
            Price = price,
            Amount = quantity * price
        };
    }


}