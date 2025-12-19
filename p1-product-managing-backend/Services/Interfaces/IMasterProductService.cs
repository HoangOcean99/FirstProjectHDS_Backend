public interface IMasterProductService
{
    Task<IEnumerable<MasterProduct>> GetAll();
    Task<IEnumerable<MasterProduct>> addMasterProduct(MasterProduct masterProduct);
    Task<bool> deleteMasterProduct(Guid Id);
    Task<bool> editMasterProduct(MasterProduct masterProduct);

}