using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[Controller]")]
public class TemplateFileController : ControllerBase
{
    private readonly ITemplateFileService _templateFileService;
    public TemplateFileController(ITemplateFileService templateFileService)
    {
        _templateFileService = templateFileService;
    }


    [HttpPost("download-report")]
    public async Task<IActionResult> DownloadReport([FromBody] ReportRequest reportRequest)
    {
        var fileBytes = await _templateFileService.DownloadSaleOutReport(reportRequest.startDate, reportRequest.toDate);

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "SaleOutReport.xlsx" 
        );
    }


    [HttpPost("download-template")]
    public async Task<IActionResult> DownloadTemplate([FromBody] List<string> columns)
    {
        var fileBytes = await _templateFileService.GenerateExcelTemplateAsync(columns);

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "TemplateMasterProduct.xlsx"
        );
    }


    [HttpPost("upload-templateProduct")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTemplateProduct(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Vui lòng chọn file!");
        }
        var result = await _templateFileService.importExcelTemplateMasterProduct(file);
        if (result.Errors.Any())
        {
            return BadRequest(new
            {
                message = "Import thất bại",
                errors = result.Errors
            });
        }
        return Ok(new
        {
            message = "Import thành công",
            inserted = result.InsertedRow
        });
    }


    [HttpPost("upload-templateSaleOut")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTemplateSaleOut(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Vui lòng chọn file!");
        }
        var result = await _templateFileService.importExcelTemplateSaleOut(file);
        if (result.Errors.Any())
        {
            return BadRequest(new
            {
                message = "Import thất bại",
                errors = result.Errors
            });
        }
        return Ok(new
        {
            message = "Import thành công",
            inserted = result.InsertedRow
        });
    }
}