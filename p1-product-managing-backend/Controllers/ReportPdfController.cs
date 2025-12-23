using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[Controller]")]
public class ReportPdfController : ControllerBase
{
    private readonly IReportPdfService _reportPdfService;
    public ReportPdfController(IReportPdfService reportPdfService)
    {
        _reportPdfService = reportPdfService;
    }


    [HttpPost("download-report-pdf")]
    public async Task<IActionResult> DownloadReportPdf([FromBody] string saleOutNo)
    {
        var fileBytes = await _reportPdfService.GenerateSaleOutPdfAsync(saleOutNo);

        return File(
            fileBytes,
            "application/pdf",
            $"Report_{saleOutNo}.pdf"
        );
    }
}