// Persistência atômica de snapshots em JSON. Nunca escreve diretamente no arquivo
// final: grava temporário e troca com File.Move(overwrite: true).
using System.Text.Json;
using Pi.Contracts.Models;
using Pi.Core.Abstractions;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Persistence;

public sealed class JsonSessionRepository(SessionSettings settings) : ISessionRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string DirectoryPath => Path.GetFullPath(
        Path.IsPathRooted(settings.Directory)
            ? settings.Directory
            : Path.Combine(System.IO.Directory.GetCurrentDirectory(), settings.Directory));

    public async Task<SessionSnapshot?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var path = PathFor(id);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SessionSnapshot>(stream, Json, cancellationToken);
    }

    public async Task SaveAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var directory = DirectoryPath;
        System.IO.Directory.CreateDirectory(directory);

        var final = PathFor(snapshot.Id);
        var temporary = final + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            await using (var stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, Json, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporary, final, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!System.IO.Directory.Exists(DirectoryPath))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var ids = System.IO.Directory.GetFiles(DirectoryPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(ids);
    }

    private string PathFor(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id de sessão vazio.", nameof(id));
        }

        var safe = new string(id.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_').ToArray());
        return Path.Combine(DirectoryPath, safe + ".json");
    }
}
