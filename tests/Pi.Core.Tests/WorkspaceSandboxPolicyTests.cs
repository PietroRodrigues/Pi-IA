// Sandbox: caminhos fora da raiz, comandos perigosos e decisões por risco.
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Xunit;

namespace Pi.Core.Tests;

public class WorkspaceSandboxPolicyTests
{
    [Fact]
    public void IsAllowed_AceitaCaminhoInternoERecusaTraversal()
    {
        var root = Directory.CreateTempSubdirectory("pi-sandbox-").FullName;

        try
        {
            var policy = new WorkspaceSandboxPolicy(root);

            Assert.True(policy.IsAllowed(Path.Combine(root, "arquivo.txt")));
            Assert.False(policy.IsAllowed(Path.Combine(root, "..", "fora.txt")));
            Assert.Throws<UnauthorizedAccessException>(
                () => policy.ResolvePath(Path.Combine(root, "..", "fora.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Decide_SegueRiscoDaFerramenta()
    {
        var root = Directory.CreateTempSubdirectory("pi-sandbox-").FullName;

        try
        {
            var policy = new WorkspaceSandboxPolicy(root, allowShell: false);

            Assert.Equal(
                PermissionDecision.Allow,
                policy.Decide(new FakeTool("read", ToolRisk.ReadOnly), "leia arquivo.txt"));
            Assert.Equal(
                PermissionDecision.Deny,
                policy.Decide(new FakeTool("shell", ToolRisk.Shell), "execute echo oi"));
            Assert.Equal(
                PermissionDecision.Ask,
                policy.Decide(new FakeTool("write", ToolRisk.Write), "edite arquivo.txt | a => b"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void IsDangerous_BloqueiaComandosDestrutivos()
    {
        Assert.True(WorkspaceSandboxPolicy.IsDangerous("rm -rf /"));
        Assert.True(WorkspaceSandboxPolicy.IsDangerous("Stop-Process -Force -Id 1"));
        Assert.False(WorkspaceSandboxPolicy.IsDangerous("git status"));
    }

    private sealed class FakeTool(string name, ToolRisk risk) : IAgentTool
    {
        public string Name { get; } = name;

        public ToolRisk Risk { get; } = risk;

        public bool CanHandle(string input) => true;

        public Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default) =>
            Task.FromResult("ok");
    }
}
