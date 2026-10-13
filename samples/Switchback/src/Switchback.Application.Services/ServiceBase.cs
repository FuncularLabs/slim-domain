using Funcular.Data.Orm;
using Switchback.Infrastructure.Persistence.Entities;

namespace Switchback.Application.Services;

/// <summary>
/// The persistence gate for the FunkyORM road (Slim Domain §2.3 and §4.5). The provider is private: a
/// service queries and writes through these members and never touches the library directly.
/// Episode 1 is read-only, so this base is deliberately small. Later episodes add audit stamping,
/// soft-delete intent and the version predicate here, in one place.
/// </summary>
public abstract class ServiceBase(IOrmDataProvider provider)
{
    private readonly IOrmDataProvider _provider = provider;

    protected IQueryable<T> Query<T>() where T : class, new() => _provider.Query<T>();

    protected Task<TKey> InsertAsync<T, TKey>(T entity) where T : class, new()
    {
        RefuseReadModel(entity);
        return _provider.InsertAsync<T, TKey>(entity);
    }

    protected Task<T> UpdateAsync<T>(T entity) where T : class, new()
    {
        RefuseReadModel(entity);
        return _provider.UpdateAsync(entity);
    }

    /// <summary>A read model is a projection; writing one is always a defect.</summary>
    private static void RefuseReadModel(object entity)
    {
        if (entity is IReadModel)
            throw new InvalidOperationException($"{entity.GetType().Name} is a read model and is never written.");
    }
}
