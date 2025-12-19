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

    [HttpDelete]
    public async Task<bool> Delete([FromBody] Guid id)
    {

    }
}