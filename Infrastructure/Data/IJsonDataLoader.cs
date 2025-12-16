namespace BackendApi.Infrastructure.Data;

public interface IJsonDataLoader<T> where T : class
{
    Task<T?> LoadAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<T>> LoadListAsync(string filePath, CancellationToken cancellationToken = default);
}
