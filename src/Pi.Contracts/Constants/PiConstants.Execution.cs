// Parte da fonte única de literais compartilhados por Pi.Contracts, Pi.Core, Pi.Infrastructure e Pi.Host.
namespace Pi.Contracts.Constants;

public static partial class PiConstants
{

    /// <summary>Timeouts, delays e intervalos de sondagem (segundos ou milissegundos).</summary>
    // Origem: configuração de execução e sondagem
    // Finalidade: tempos limite, delays e intervalos.
    public static class Timeouts
    {
        public const int RequestSeconds = 60;
        public const int StartupSeconds = 30;
        public const int DecisionSeconds = 30;
        public const int ExecutionSeconds = 30;
        public const int SearchSeconds = 10;
        public const int ShellSeconds = 15;
        public const int MinimumSeconds = 1;
        public const int ProbeSeconds = 2;
        public const int PollMilliseconds = 250;
        public const int KillWaitMilliseconds = 2000;
        public const int GitWaitMilliseconds = 1000;
        public const int DispatcherShutdownSeconds = 5;
    }

    /// <summary>Limites, tamanhos aproximados e números mágicos.</summary>
    // Origem: limites de contexto, busca e leitura
    // Finalidade: tetos numéricos usados pelo Core e pela Infrastructure.
    public static class Limits
    {
        public const int ContextWindowTokens = 32768;
        public const int HistoryMessages = 20;
        public const int MaxResults = 20;
        public const int MaxReadBytes = 65536;
        public const int MaxOutputCharacters = 8000;
        public const int MaxToolIterations = 4;
        public const int CharsPerToken = 4;
        public const int MinimumTokenEstimate = 1;
        public const int MinimumContextLimit = 1;
        public const int MinimumHistoryMessages = 2;
        public const double MaxPercent = 100.0;
        public const int ReadFileMaxLines = 500;
        public const int ReadFileDefaultLines = 200;
        public const int ReadFileMinLines = 1;
        public const int ReadFileMinBytes = 256;
        public const int MinSearchTermLength = 2;
        public const int MaxSearchFileBytes = 1_000_000;
        public const int NdjsonChunkSize = 4096;
        public const int MaxNdjsonLineLength = 1_048_576;
        public const int ExpectedSingleOccurrence = 1;
    }

    /// <summary>Formatos usados por ToString/string.Format.</summary>
    // Origem: formatação de datas e identificadores
    // Finalidade: formatos passados a ToString/string.Format.
    public static class Formats
    {
        public const string TimestampRoundtrip = "O";
        public const string GuidN = "N";
    }

    /// <summary>Famílias de modelos com suporte a ferramentas.</summary>
    // Origem: famílias de modelos suportadas
    // Finalidade: nomes usados na detecção de tool calling.
    public static class Models
    {
        public const string Qwen3 = "qwen3";
        public const string Llama3 = "llama3";
        public static readonly string[] ToolCapableFamilies = [Qwen3, Llama3];
    }

    /// <summary>Nomes, prefixos e listas usados pelas ferramentas.</summary>
    // Origem: ferramentas do agente
    // Finalidade: nomes, prefixos e diretórios ignorados.
    public static class Tools
    {
        public const string ReadFileName = "read_file";
        public const string EditFileName = "edit_file";
        public const string SearchCodeName = "search_code";
        public const string ExecuteShellName = "execute_shell";
        public static readonly string[] ReadPrefixes = ["leia ", "read "];
        public static readonly string[] EditPrefixes = ["edite ", "edit "];
        public static readonly string[] SearchPrefixes = ["procure ", "busque "];
        public static readonly string[] ShellPrefixes = ["execute ", "shell "];
        public static readonly string[] IgnoredDirectories =
            [".git", "node_modules", "artifacts", "bin", "obj", "dist", ".vs", "coverage"];
    }

    /// <summary>Padrões de segurança da política de workspace.</summary>
    // Origem: política de workspace
    // Finalidade: comandos considerados perigosos.
    public static class Security
    {
        public static readonly string[] DangerousCommands =
        [
            "rm -rf", "del /s", "format", "mkfs", "diskpart", "shutdown",
            "taskkill /f", "stop-process -force", "reg delete", "curl ", "wget "
        ];
    }

    /// <summary>Padrões regex compartilhados (argumentos de atributo exigem const).</summary>
    // Origem: parsing de argumentos de ferramentas
    // Finalidade: padrões regex compartilhados.
    public static class Patterns
    {
        public const string ToolTokens = "\"([^\"]+)\"|(\\S+)";
    }
}
