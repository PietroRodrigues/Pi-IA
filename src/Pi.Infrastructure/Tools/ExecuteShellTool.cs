// Execução de processo sem shell implícito: timeout, kill tree e saída limitada.
using System.Diagnostics;
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Tools;

public sealed class ExecuteShellTool(
    WorkspaceSandboxPolicy sandbox,
    ToolSettings settings) : IAgentTool
{
    public string Name => "execute_shell";

    public ToolRisk Risk => ToolRisk.Shell;

    public bool CanHandle(string input) =>
        input.StartsWith("execute ", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("shell ", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (WorkspaceSandboxPolicy.IsDangerous(input))
        {
            throw new InvalidOperationException("Comando bloqueado pela política de segurança.");
        }

        var tokens = ToolInput.Tokens(ToolInput.Argument(input));
        if (tokens.Count == 0)
        {
            return "Informe o executável após 'execute'.";
        }

        var startInfo = new ProcessStartInfo(tokens[0])
        {
            WorkingDirectory = sandbox.Root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in tokens.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Não foi possível iniciar '{tokens[0]}'.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.ShellTimeoutSeconds)));

        var output = new System.Text.StringBuilder();
        process.OutputDataReceived += (_, args) => Append(output, args.Data);
        process.ErrorDataReceived += (_, args) => Append(output, args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            return "O comando excedeu o tempo limite e foi encerrado.";
        }

        var suffix = process.ExitCode == 0 ? string.Empty : $" (exit {process.ExitCode})";
        return output.Length == 0 ? $"Comando concluído{suffix}." : output.ToString() + suffix;
    }

    private void Append(System.Text.StringBuilder output, string? data)
    {
        if (data is null || output.Length >= settings.MaxOutputCharacters)
        {
            return;
        }

        var remaining = settings.MaxOutputCharacters - output.Length;
        output.AppendLine(data.Length > remaining ? data[..remaining] : data);
    }
}
