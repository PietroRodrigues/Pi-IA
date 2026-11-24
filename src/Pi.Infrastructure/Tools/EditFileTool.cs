// Aplicação segura de patch texto: backup temporário, validação e troca atômica.
using Pi.Core.Abstractions;
using Pi.Core.Policies;

namespace Pi.Infrastructure.Tools;

public sealed class EditFileTool(WorkspaceSandboxPolicy sandbox) : IAgentTool
{
    public string Name => "edit_file";

    public ToolRisk Risk => ToolRisk.Write;

    public bool CanHandle(string input) =>
        input.StartsWith("edite ", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("edit ", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var parts = ToolInput.Argument(input).Split('|', 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !parts[1].Contains("=>", StringComparison.Ordinal))
        {
            return "Formato: edite <arquivo> | <texto antigo> => <texto novo>";
        }

        var path = sandbox.ResolvePath(parts[0]);
        if (!File.Exists(path))
        {
            return $"Arquivo não encontrado: {parts[0]}";
        }

        var separator = parts[1].IndexOf("=>", StringComparison.Ordinal);
        var oldText = parts[1][..separator].Trim();
        var newText = parts[2].Trim();

        if (oldText.Length == 0)
        {
            return "O texto antigo não pode ser vazio.";
        }

        var original = await File.ReadAllTextAsync(path, cancellationToken);
        var occurrences = CountOccurrences(original, oldText);
        if (occurrences != 1)
        {
            return $"O texto antigo aparece {occurrences} vezes; esperado exatamente 1.";
        }

        var token = Guid.NewGuid().ToString("N");
        var temporary = path + ".tmp-" + token;
        var backup = path + ".bak-" + token;

        try
        {
            await File.WriteAllTextAsync(backup, original, cancellationToken);
            var updated = original.Replace(oldText, newText, StringComparison.Ordinal);
            await File.WriteAllTextAsync(temporary, updated, cancellationToken);
            File.Move(temporary, path, overwrite: true);
            File.Delete(backup);
            return $"Arquivo atualizado: {Path.GetFileName(path)}";
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
