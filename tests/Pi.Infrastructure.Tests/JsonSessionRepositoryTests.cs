// Persistência atômica, leitura e sanitização de id de sessão.
using Pi.Contracts.Models;
using Pi.Infrastructure.Options;
using Pi.Infrastructure.Persistence;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class JsonSessionRepositoryTests
{
    [Fact]
    public async Task SaveGet_List_ArquivoUnicoPorSessao()
    {
        var directory = Directory.CreateTempSubdirectory("pi-sessions-").FullName;

        try
        {
            var sut = new JsonSessionRepository(new SessionSettings { Directory = directory });
            var snapshot = SessionSnapshot.Create("s1", "modelo", "geral", "C:/temp", "main", 1024);
            snapshot.Messages.Add(SessionMessage.Create("user", "oi"));

            await sut.SaveAsync(snapshot);
            var loaded = await sut.GetAsync("s1");
            var ids = await sut.ListAsync();

            Assert.NotNull(loaded);
            Assert.Equal("oi", Assert.Single(loaded!.Messages).Text);
            Assert.Equal(["s1"], ids);
            Assert.Single(Directory.GetFiles(directory, "*.json"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_IdComTraversal_SanitizaNomeDoArquivo()
    {
        var directory = Directory.CreateTempSubdirectory("pi-sessions-").FullName;

        try
        {
            var sut = new JsonSessionRepository(new SessionSettings { Directory = directory });
            var snapshot = SessionSnapshot.Create("../../fora", "modelo", "geral", "C:/temp", "main");

            await sut.SaveAsync(snapshot);

            Assert.Single(Directory.GetFiles(directory, "*.json"));
            Assert.Null(await sut.GetAsync("inexistente"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_SubstituiSnapshotSemDeixarTemporario()
    {
        var directory = Directory.CreateTempSubdirectory("pi-sessions-").FullName;

        try
        {
            var sut = new JsonSessionRepository(new SessionSettings { Directory = directory });
            var first = SessionSnapshot.Create("s1", "modelo", "geral", "C:/temp", "main");
            var second = first with { Branch = "feature/novo" };

            await sut.SaveAsync(first);
            await sut.SaveAsync(second);

            var loaded = await sut.GetAsync("s1");
            Assert.Equal("feature/novo", loaded!.Branch);
            Assert.Empty(Directory.GetFiles(directory, "*.tmp-*"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
