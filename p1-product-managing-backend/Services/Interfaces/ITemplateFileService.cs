public interface ITemplateFileService
{
    Task<byte[]> GenerateExcelTemplateAsync(List<string> columns);
    Task<ImportResult> importExcelTemplate(IFormFile file);
}