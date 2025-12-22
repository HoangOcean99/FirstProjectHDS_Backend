public interface ITemplateFileService
{
    Task<byte[]> GenerateExcelTemplateAsync(List<string> columns);
    Task<byte[]> DownloadSaleOutReport(int startDate, int endDate);
    Task<ImportResult> importExcelTemplateMasterProduct(IFormFile file);
    Task<ImportResult> importExcelTemplateSaleOut(IFormFile file);
}