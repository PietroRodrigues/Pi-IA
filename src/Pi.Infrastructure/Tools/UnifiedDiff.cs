// Diff unificado mínimo: um hunk com contexto ao redor da alteração.
using Pi.Contracts.Constants;

namespace Pi.Infrastructure.Tools;

internal static class UnifiedDiff
{
    public static string Build(string relativePath, string original, string updated)
    {
        var before = Lines(original);
        var after = Lines(updated);

        var prefix = 0;
        while (prefix < before.Count && prefix < after.Count &&
               string.Equals(before[prefix], after[prefix], StringComparison.Ordinal))
        {
            prefix++;
        }

        var suffix = 0;
        while (suffix < before.Count - prefix && suffix < after.Count - prefix &&
               string.Equals(
                   before[before.Count - suffix - 1],
                   after[after.Count - suffix - 1],
                   StringComparison.Ordinal))
        {
            suffix++;
        }

        var context = PiConstants.Limits.DiffContextLines;
        var start = Math.Max(0, prefix - context);
        var beforeEnd = Math.Min(before.Count, before.Count - suffix + context);
        var afterEnd = Math.Min(after.Count, after.Count - suffix + context);

        var output = new List<string>
        {
            string.Format(PiConstants.Messages.DiffOldFileTemplate, relativePath),
            string.Format(PiConstants.Messages.DiffNewFileTemplate, relativePath),
            string.Format(
                PiConstants.Messages.DiffHunkTemplate,
                start + 1,
                beforeEnd - start,
                start + 1,
                afterEnd - start)
        };

        for (var index = start; index < prefix; index++)
        {
            output.Add(string.Format(PiConstants.Messages.DiffContextTemplate, before[index]));
        }

        for (var index = prefix; index < before.Count - suffix; index++)
        {
            output.Add(string.Format(PiConstants.Messages.DiffRemovedTemplate, before[index]));
        }

        for (var index = prefix; index < after.Count - suffix; index++)
        {
            output.Add(string.Format(PiConstants.Messages.DiffAddedTemplate, after[index]));
        }

        for (var index = before.Count - suffix; index < beforeEnd; index++)
        {
            output.Add(string.Format(PiConstants.Messages.DiffContextTemplate, before[index]));
        }

        return string.Join(Environment.NewLine, output);
    }

    private static List<string> Lines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        var crlf = string.Concat(PiConstants.Protocol.CarriageReturn, PiConstants.Protocol.LineFeed);
        var lineFeed = PiConstants.Protocol.LineFeed.ToString();
        return text.Replace(crlf, lineFeed)
            .TrimEnd(PiConstants.Protocol.LineFeed)
            .Split(PiConstants.Protocol.LineFeed)
            .ToList();
    }
}
