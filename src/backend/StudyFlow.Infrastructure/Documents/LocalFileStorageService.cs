using Microsoft.Extensions.Options;
using StudyFlow.Application.Documents.Interfaces;

namespace StudyFlow.Infrastructure.Documents;

public sealed class FileStorageOptions { public const string SectionName = "FileStorage"; public string RootPath { get; init; } = "uploads"; }

internal sealed class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);
    public async Task SaveAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        var path = Resolve(key); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken);
    }
    public async Task SaveAsync(string key, Stream stream, CancellationToken cancellationToken)
    {
        var path = Resolve(key); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await stream.CopyToAsync(target, cancellationToken);
    }
    public Task<byte[]> ReadAsync(string key, CancellationToken cancellationToken) => File.ReadAllBytesAsync(Resolve(key), cancellationToken);
    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key); if (File.Exists(path)) File.Delete(path); return Task.CompletedTask;
    }
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(File.Exists(Resolve(key))); }
    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid storage key.");
        return path;
    }
}
