// Execução de processo sem shell implícito: timeout, kill tree e saída limitada.
using System.Diagnostics;
using Pi.Contracts.Constants;
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Tools;

public sealed class ExecuteShellTool(
    WorkspaceSandboxPolicy sandbox,
    ToolSettings settings) : IAgentTool
{
    public string Name => PiConstants.Tools.ExecuteShellName;

    public ToolRisk Risk => ToolRisk.Shell;

    public bool CanHandle(string input) =>
        PiConstants.Tools.ShellPrefixes.Any(prefix =>
            input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (WorkspaceSandboxPolicy.IsDangerous(input))
        {
            throw new InvalidOperationException(PiConstants.Messages.CommandBlocked);
        }

        var tokens = ToolInput.Tokens(ToolInput.Argument(input));
        if (tokens.Count == 0)
        {
            return PiConstants.Messages.MissingExecutable;
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
            ?? throw new InvalidOperationException(string.Format(
                PiConstants.Messages.CannotStartExecutableTemplate,
                tokens[0]));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(
            Math.Max(PiConstants.Timeouts.MinimumSeconds, settings.ShellTimeoutSeconds)));

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

            return PiConstants.Messages.CommandTimedOut;
        }

        var suffix = process.ExitCode == 0
            ? string.Empty
            : string.Format(PiConstants.Messages.CommandExitSuffixTemplate, process.ExitCode);
        return output.Length == 0
            ? string.Format(PiConstants.Messages.CommandCompletedTemplate, suffix)
            : output.ToString() + suffix;
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
