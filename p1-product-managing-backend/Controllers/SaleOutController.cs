using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[Controller]")]
public class SaleOutController : ControllerBase
{
    private readonly ISaleOutService _saleOutService;
    public SaleOutController(ISaleOutService saleOutService)
    {
        _saleOutService = saleOutService;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _saleOutService.GetAll();
        return Ok(data);
    }


    [HttpPost]
    public async Task<IActionResult> Insert([FromBody] SaleOut saleOut)
    {
        var data = await _saleOutService.AddSaleOutAsync(saleOut);
        return Ok(data);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var isDeleted = await _saleOutService.deleteSaleOut(id);
        if (!isDeleted)
            return NotFound(new { message = "Không tìm thấy sản phẩm để xóa." });

        return Ok(new { message = "Xóa sản phẩm thành công." });
    }
    
    
    [HttpPut]
    public async Task<IActionResult> Edit([FromBody] SaleOut saleOut)
    {
        var data = await _saleOutService.editSaleOut(saleOut);
        return Ok(data);
    }
}