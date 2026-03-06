using SoftSign.Domain.Common;

namespace SoftSign.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    IRepository<T> GetRepository<T>() where T : class;
}
