// Parser pequeno e previsível para comandos de texto das ferramentas.
using System.Text.RegularExpressions;
using Pi.Contracts.Constants;

namespace Pi.Infrastructure.Tools;

public static partial class ToolInput
{
    public static string Argument(string input)
    {
        var parts = input.Split(
            PiConstants.Protocol.Space,
            2,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    public static IReadOnlyList<string> Tokens(string value) =>
        TokenRegex().Matches(value)
            .Select(match => string.IsNullOrEmpty(match.Groups[1].Value)
                ? match.Groups[2].Value
                : match.Groups[1].Value)
            .ToList();

    [GeneratedRegex(PiConstants.Patterns.ToolTokens)]
    private static partial Regex TokenRegex();
}
