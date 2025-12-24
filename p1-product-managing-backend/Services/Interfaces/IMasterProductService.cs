public interface IMasterProductService
{
    Task<IEnumerable<MasterProduct>> GetAll();
    Task<PagedResult<MasterProduct>> GetPagedAsync(int pageIndex, int pageSize);

    Task<IEnumerable<MasterProduct>> addMasterProduct(MasterProduct masterProduct);
    Task<bool> deleteMasterProduct(Guid Id);
    Task<bool> editMasterProduct(MasterProduct masterProduct);

}