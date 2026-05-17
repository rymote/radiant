using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Repository;

public interface ISmartRepository<TModel> where TModel : class, new()
{
    Task<TModel> InsertAsync(TModel model);
    Task<TModel> InsertAsync(TModel model, CancellationToken cancellationToken);
    Task<IReadOnlyList<TModel>> InsertManyAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default);

    Task<TModel> UpdateAsync(TModel model);
    Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken);

    Task<TModel> UpsertAsync(TModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(TModel model);
    Task<bool> DeleteAsync(TModel model, CancellationToken cancellationToken);

    Task<bool> ForceDeleteAsync(TModel model, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(TModel model);
    Task<bool> SoftDeleteAsync(TModel model, CancellationToken cancellationToken);

    Task<bool> RestoreAsync(TModel model);
    Task<bool> RestoreAsync(TModel model, CancellationToken cancellationToken);
}
