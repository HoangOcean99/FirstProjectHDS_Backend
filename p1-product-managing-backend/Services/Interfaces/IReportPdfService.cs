public interface IReportPdfService
{
    Task<byte[]> GenerateSaleOutPdfAsync(string saleOutNo);
}