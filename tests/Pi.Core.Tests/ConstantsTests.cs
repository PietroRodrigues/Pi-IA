// Guardrail: literais de configuração/protocolo/mensagem vivem apenas na pasta
// Pi.Contracts/Constants (fachada PiConstants.cs + parciais por domínio).
// Os arquivos de protocolo v1 e os testes (asserções com texto esperado) são isentos.
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Pi.Core.Tests;

public class ConstantsTests
{
    private static readonly string[] ExemptFiles =
    [
        "Protocol/Commands.cs",
        "Protocol/Events.cs",
        "Protocol/Envelope.cs"
    ];

    private const string ConstantsDirectory = "src/Pi.Contracts/Constants/";

    private static readonly string[] RequiredConstantFiles =
    [
        "PiConstants.cs",
        "PiConstants.Configuration.cs",
        "PiConstants.Protocol.cs",
        "PiConstants.Execution.cs",
        "PiConstants.Text.cs"
    ];

    private static readonly (string Name, Regex Pattern)[] Rules =
    [
        ("URL ou endpoint", new Regex(@"https?://|/api/(tags|chat|generate)", RegexOptions.IgnoreCase)),
        ("variável de ambiente", new Regex(@"\b(PI|OLLAMA|DOTNET)_[A-Z0-9_]+")),
        ("modelo conhecido", new Regex(@"\bqwen[0-9.-]*\b|llama3(\.2)?|deepseek-r1|phi4:", RegexOptions.IgnoreCase)),
        ("número fixo", new Regex(@"\b(32768|65536|8000|1048576|4096|1000000|11434)\b")),
        ("chave JSON", new Regex(@"""(response|done|message|tool_calls|function|arguments|content|role|path|filePath|file|prompt_eval_count|eval_count)""")),
        ("papel", new Regex(@"""(system|user|assistant|tool)""")),
        ("tipo de protocolo", new Regex(@"""(prompt_submit|interrupt_turn|tool_decision|session_switch|session_clear|session_snapshot|stream_delta|tool_request|tool_result|context_metrics|error_event|user_cancel|invalid_message|unsupported_version|empty_prompt|unknown_command|tool_denied|tool_timeout|llm_failure|session_failure|internal)""")),
        ("processo ou arquivo", new Regex(@"""(ollama|bun|git|dotnet|serve|install|stdio|node_modules|global.json|package.json)""")),
        ("comando perigoso", new Regex(@"""(rm -rf|del /s|mkfs|diskpart|shutdown|taskkill /f|stop-process -force|reg delete)""")),
        ("texto em português", new Regex(@"""[^""]*[\u00C0-\u00FF][^""]*"""))
    ];

    [Fact]
    public void CodigoFonte_NaoContemLiteraisForaDePiConstants()
    {
        var root = FindRepoRoot();
        var sourceDirectory = Path.Combine(root, "src");
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            if (relative.StartsWith(ConstantsDirectory, StringComparison.Ordinal)) continue;
            if (ExemptFiles.Any(relative.EndsWith)) continue;

            var code = StripComments(File.ReadAllText(path));
            foreach (var (name, pattern) in Rules)
            {
                if (pattern.IsMatch(code))
                {
                    failures.Add($"{relative}: literal de {name} fora de PiConstants.cs");
                }
            }
        }

        Assert.True(
            failures.Count == 0,
            "Literais encontrados fora do arquivo de constantes:\n" + string.Join("\n", failures));
    }

    [Fact]
    public void Constantes_EstaoDivididasPorDominio()
    {
        var directory = Path.Combine(FindRepoRoot(), "src", "Pi.Contracts", "Constants");

        foreach (var file in RequiredConstantFiles)
        {
            Assert.True(File.Exists(Path.Combine(directory, file)), $"Arquivo ausente: {file}");
        }

        // A fachada não deve conter grupos: apenas a classe parcial vazia.
        var facade = File.ReadAllText(Path.Combine(directory, "PiConstants.cs"));
        Assert.DoesNotContain("public static class Defaults", facade);
    }

    [Fact]
    public void Regras_DetectamLiteralDeExemplo()
    {
        Assert.Contains(Rules, rule =>
            rule.Name == "variável de ambiente" &&
            rule.Pattern.IsMatch("Environment.GetEnvironmentVariable(\"PI_MODEL\")"));
        Assert.Contains(Rules, rule =>
            rule.Name == "texto em português" &&
            rule.Pattern.IsMatch("\"Arquivo não encontrado\""));
    }

    [Fact]
    public void PiConstants_ContemGruposEssenciais()
    {
        Assert.Equal("http://127.0.0.1:11434", Pi.Contracts.Constants.PiConstants.Defaults.BaseUrl);
        Assert.Equal(32768, Pi.Contracts.Constants.PiConstants.Limits.ContextWindowTokens);
        Assert.Equal("/api/tags", Pi.Contracts.Constants.PiConstants.Endpoints.Tags);
        Assert.Equal("OLLAMA_STOP_ON_EXIT", Pi.Contracts.Constants.PiConstants.Env.OllamaStopOnExit);
    }

    private static string FindRepoRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "global.json")) &&
                    File.Exists(Path.Combine(directory.FullName, "src", "Pi.Tui", "package.json")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }

    // Remove comentários para que documentação em português não dispare as regras.
    private static string StripComments(string code)
    {
        var result = new StringBuilder();
        var index = 0;

        while (index < code.Length)
        {
            var c = code[index];
            var next = index + 1 < code.Length ? code[index + 1] : '\0';

            if (c == '/' && next == '/')
            {
                while (index < code.Length && code[index] != '\n')
                {
                    result.Append(' ');
                    index += 1;
                }
                continue;
            }

            if (c == '/' && next == '*')
            {
                result.Append("  ");
                index += 2;
                while (index < code.Length &&
                       !(code[index] == '*' && index + 1 < code.Length && code[index + 1] == '/'))
                {
                    result.Append(code[index] == '\n' ? '\n' : ' ');
                    index += 1;
                }
                result.Append("  ");
                index += 2;
                continue;
            }

            if (c == '@' && next == '"')
            {
                result.Append("  ");
                index += 2;
                while (index < code.Length)
                {
                    if (code[index] == '"')
                    {
                        if (index + 1 < code.Length && code[index + 1] == '"')
                        {
                            result.Append("  ");
                            index += 2;
                            continue;
                        }
                        result.Append('"');
                        index += 1;
                        break;
                    }
                    result.Append(code[index]);
                    index += 1;
                }
                continue;
            }

            if (c == '"' && index + 2 < code.Length &&
                code[index + 1] == '"' && code[index + 2] == '"')
            {
                index += 3;
                while (index < code.Length &&
                       !(code[index] == '"' && index + 2 < code.Length &&
                         code[index + 1] == '"' && code[index + 2] == '"'))
                {
                    result.Append(code[index] == '\n' ? '\n' : ' ');
                    index += 1;
                }
                result.Append("   ");
                index += 3;
                continue;
            }

            if (c == '"')
            {
                result.Append(c);
                index += 1;
                while (index < code.Length)
                {
                    result.Append(code[index]);
                    if (code[index] == '\\' && index + 1 < code.Length)
                    {
                        result.Append(code[index + 1]);
                        index += 2;
                        continue;
                    }
                    var closed = code[index] == '"';
                    index += 1;
                    if (closed) break;
                }
                continue;
            }

            if (c == '\'')
            {
                result.Append(c);
                index += 1;
                while (index < code.Length)
                {
                    result.Append(code[index]);
                    if (code[index] == '\\' && index + 1 < code.Length)
                    {
                        result.Append(code[index + 1]);
                        index += 2;
                        continue;
                    }
                    var closed = code[index] == '\'';
                    index += 1;
                    if (closed) break;
                }
                continue;
            }

            result.Append(c);
            index += 1;
        }

        return result.ToString();
    }
}
