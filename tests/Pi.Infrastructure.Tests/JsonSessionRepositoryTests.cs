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

    [Fact]
    public async Task SaveGet_PreservaToolCallsEToolCallId()
    {
        var directory = Directory.CreateTempSubdirectory("pi-sessions-").FullName;

        try
        {
            var sut = new JsonSessionRepository(new SessionSettings { Directory = directory });
            var snapshot = SessionSnapshot.Create("s1", "modelo", "geral", "C:/temp", "main", 1024);
            snapshot.Messages.Add(SessionMessage.Create("user", "leia a.txt"));
            snapshot.Messages.Add(SessionMessage.Create("assistant", string.Empty, new[]
            {
                new SessionToolCall("call-1", "read_file", "{\"path\":\"a.txt\"}")
            }));
            snapshot.Messages.Add(SessionMessage.Create("tool", "conteudo", toolCallId: "call-1"));

            await sut.SaveAsync(snapshot);
            var loaded = await sut.GetAsync("s1");

            Assert.NotNull(loaded);
            var assistant = Assert.Single(loaded!.Messages, message => message.Role == "assistant");
            var call = Assert.Single(assistant.ToolCalls!);
            Assert.Equal("call-1", call.Id);
            Assert.Equal("read_file", call.Name);
            Assert.Equal("{\"path\":\"a.txt\"}", call.ArgumentsJson);

            var tool = Assert.Single(loaded.Messages, message => message.Role == "tool");
            Assert.Equal("call-1", tool.ToolCallId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Get_SnapshotAntigoSemToolCalls_UsaValoresPadrao()
    {
        const string legacy = """
            {
              "id": "antiga",
              "model": "modelo",
              "agent": "geral",
              "cwd": "C:/temp",
              "branch": "main",
              "messages": [
                { "role": "user", "text": "oi", "timestamp": "2026-01-01T00:00:00Z" },
                { "role": "tool", "text": "conteudo", "timestamp": "2026-01-01T00:00:00Z" }
              ],
              "context": { "used": 0, "limit": 1024, "percent": 0 },
              "updatedAt": "2026-01-01T00:00:00Z"
            }
            """;
        var directory = Directory.CreateTempSubdirectory("pi-sessions-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "antiga.json"), legacy);
            var sut = new JsonSessionRepository(new SessionSettings { Directory = directory });

            var loaded = await sut.GetAsync("antiga");

            Assert.NotNull(loaded);
            Assert.Equal(2, loaded!.Messages.Count);
            Assert.All(loaded.Messages, message =>
            {
                Assert.Null(message.ToolCalls);
                Assert.Null(message.ToolCallId);
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
