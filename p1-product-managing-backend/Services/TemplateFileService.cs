
using System.Drawing;
using OfficeOpenXml;

public class TemplateFileService : ITemplateFileService
{
    private readonly ValidationUploadFile _validationUploadFile;
    private readonly IMasterProductService _masterProductService;

    public TemplateFileService(ValidationUploadFile validationUploadFile, IMasterProductService masterProductService)
    {
        _validationUploadFile = validationUploadFile;
        _masterProductService = masterProductService;
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

            // dùng MemoryStream async
            using (var stream = new MemoryStream())
            {
                await package.SaveAsAsync(stream);
                return stream.ToArray();
            }
        }
    }

    public async Task<ImportResult> importExcelTemplate(IFormFile file)
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
            _validationUploadFile.ValidateHeader(ws);
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
                var product = ParseRow(ws, i);

                _validationUploadFile.ValidateRequiredFields(product, i);

                await _validationUploadFile.ValidateDuplicateCodeAsync(
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

    private MasterProduct ParseRow(ExcelWorksheet ws, int row)
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



}