using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class ReportPdfService : IReportPdfService
{
    private readonly ISaleOutService _saleOutService;
    public ReportPdfService(ISaleOutService saleOutService)
    {
        _saleOutService = saleOutService;
    }
    public async Task<byte[]> GenerateSaleOutPdfAsync(string saleOutNo)
    {
        var saleOuts = await _saleOutService.getSaleOutByNo(saleOutNo);

        var doc = new SaleOutPdfDocument(saleOuts, saleOutNo);
        return doc.GeneratePdf();
    }

}
