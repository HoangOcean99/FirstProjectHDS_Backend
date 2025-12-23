public interface ISaleOutService
{
    Task<IEnumerable<SaleOut>> GetAll();
    Task<SaleOut> AddSaleOutAsync(SaleOut saleOut, string saleOutNo);
    Task<bool> deleteSaleOut(Guid Id);
    Task<bool> editSaleOut(SaleOut saleOut);
    Task<string> GenerateSaleOutNoAsync();
    Task<IEnumerable<string>> getAllSaleOutNo();
    Task<List<SaleOutPdf>> getSaleOutByNo(string saleOutNo);

}