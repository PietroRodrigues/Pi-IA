// Aplicação segura de patch texto: backup temporário, validação e troca atômica.
using Pi.Contracts.Constants;
using Pi.Core.Abstractions;
using Pi.Core.Policies;

namespace Pi.Infrastructure.Tools;

public sealed class EditFileTool(WorkspaceSandboxPolicy sandbox) : IAgentTool
{
    public string Name => PiConstants.Tools.EditFileName;

    public ToolRisk Risk => ToolRisk.Write;

    public bool CanHandle(string input) =>
        PiConstants.Tools.EditPrefixes.Any(prefix =>
            input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var parts = ToolInput.Argument(input)
            .Split(PiConstants.Protocol.PipeSeparator, 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !parts[1].Contains(PiConstants.Protocol.EditArrow, StringComparison.Ordinal))
        {
            return PiConstants.Messages.EditFormat;
        }

        var path = sandbox.ResolvePath(parts[0]);
        if (!File.Exists(path))
        {
            return string.Format(PiConstants.Messages.FileNotFoundTemplate, parts[0]);
        }

        var separator = parts[1].IndexOf(PiConstants.Protocol.EditArrow, StringComparison.Ordinal);
        var oldText = parts[1][..separator].Trim();
        var newText = parts[2].Trim();

        if (oldText.Length == 0)
        {
            return PiConstants.Messages.EmptyOldText;
        }

        var original = await File.ReadAllTextAsync(path, cancellationToken);
        var occurrences = CountOccurrences(original, oldText);
        if (occurrences != 1)
        {
            return string.Format(
                PiConstants.Messages.OldTextOccurrencesTemplate,
                occurrences,
                PiConstants.Limits.ExpectedSingleOccurrence);
        }

        var token = Guid.NewGuid().ToString(PiConstants.Formats.GuidN);
        var temporary = path + PiConstants.Paths.TemporarySuffix + token;
        var backup = path + PiConstants.Paths.BackupSuffix + token;

        try
        {
            await File.WriteAllTextAsync(backup, original, cancellationToken);
            var updated = original.Replace(oldText, newText, StringComparison.Ordinal);
            await File.WriteAllTextAsync(temporary, updated, cancellationToken);
            File.Move(temporary, path, overwrite: true);
            File.Delete(backup);
            return string.Format(PiConstants.Messages.FileUpdatedTemplate, Path.GetFileName(path));
        }
        catch
        {
            if (File.Exists(backup))
            {
                File.Copy(backup, path, overwrite: true);
            }

            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }

            throw;
        }
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
