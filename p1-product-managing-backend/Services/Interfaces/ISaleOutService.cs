public interface ISaleOutService
{
    Task<IEnumerable<SaleOut>> GetAll();
}