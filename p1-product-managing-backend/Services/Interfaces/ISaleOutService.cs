public interface ISaleOutService
{
    Task<IEnumerable<SaleOut>> GetAll();
    Task<SaleOut> AddSaleOutAsync(SaleOut saleOut);
    Task<bool> deleteSaleOut(Guid Id);
}