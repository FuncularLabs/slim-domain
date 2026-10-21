namespace Switchback.Infrastructure.Persistence.Entities;

/// <summary>
/// Marks a read model: a query shape that must never reach a write path.
/// FunkyORM has no built-in read-only marker, so the solution declares one, and
/// <c>ServiceBase</c> refuses to insert or update anything that implements it.
/// </summary>
public interface IReadModel;
