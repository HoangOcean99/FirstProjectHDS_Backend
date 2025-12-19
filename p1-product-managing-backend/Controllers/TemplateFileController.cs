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


    [HttpPost("upload-template")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTemplate(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Vui lòng chọn file!");
        }
        var result = await _templateFileService.importExcelTemplate(file);
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