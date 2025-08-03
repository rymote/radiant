using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Repository;

public interface ISmartRepository<TModel> where TModel : class, new()
{
    Task<TModel> InsertAsync(TModel model);
    Task<TModel> UpdateAsync(TModel model);
    Task<bool> DeleteAsync(TModel model);
    Task<bool> SoftDeleteAsync(TModel model);
    Task<bool> RestoreAsync(TModel model);
}