using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[Controller]")]
public class MasterProductController : ControllerBase
{
    private readonly IMasterProductService _productService;
    private readonly ILogger<MasterProductController> _logger;

    public MasterProductController(IMasterProductService productService, ILogger<MasterProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _productService.GetAll();
        return Ok(data);
    }


    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] MasterProduct masterProduct)
    {
        var data = await _productService.addMasterProduct(masterProduct);
        return Ok(data);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var isDeleted = await _productService.deleteMasterProduct(id);
        if (!isDeleted)
            return NotFound(new { message = "Không tìm thấy sản phẩm để xóa." });

        return Ok(new { message = "Xóa sản phẩm thành công." });
    }


    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] MasterProduct masterProduct)
    {
        var data = await _productService.editMasterProduct(masterProduct);
        return Ok(data);
    }
}