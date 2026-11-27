// Parte da fonte única de literais compartilhados por Pi.Contracts, Pi.Core, Pi.Infrastructure e Pi.Host.
namespace Pi.Contracts.Constants;

public static partial class PiConstants
{

    /// <summary>Valores padrão das classes de configuração (equivalentes ao appsettings/.env).</summary>
    // Origem: appsettings.json e .env
    // Finalidade: valores padrão usados quando a configuração não informa outro valor.
    public static class Defaults
    {
        public const string BaseUrl = "http://127.0.0.1:11434";
        public const string Model = "qwen3.8-2b-local";
        public const int TimeoutSeconds = Timeouts.RequestSeconds;
        public const bool AutoStart = true;
        public const int StartupTimeoutSeconds = Timeouts.StartupSeconds;
        public const bool StopOnExit = false;
        public const string SystemPrompt =
            "Você é Pi, um assistente local. Fale português do Brasil, seja claro e não invente informações.";
        public const int ContextWindowTokens = Limits.ContextWindowTokens;
        public const int MaxHistoryMessages = Limits.HistoryMessages;
        public const string AgentName = "geral";
        public const string SessionId = "default";
        public const int MaxResults = Limits.MaxResults;
        public const int SearchTimeoutSeconds = Timeouts.SearchSeconds;
        public const int MaxReadBytes = Limits.MaxReadBytes;
        public const int MaxOutputCharacters = Limits.MaxOutputCharacters;
        public const int ShellTimeoutSeconds = Timeouts.ShellSeconds;
        public const bool AllowShell = false;
        public const string SessionDirectory = ".pi-ia/sessions";
        public const int DecisionTimeoutSeconds = Timeouts.DecisionSeconds;
        public const int ExecutionTimeoutSeconds = Timeouts.ExecutionSeconds;
    }

    /// <summary>Nomes de arquivos, diretórios, sufixos e prefixos de caminho.</summary>
    // Origem: sistema de arquivos e diretório do repositório
    // Finalidade: caminhos relativos e nomes de arquivos locais.
    public static class Paths
    {
        public const string SessionDirectory = Defaults.SessionDirectory;
        public const string SourceFolderName = "src";
        public const string TuiFolderName = "Pi.Tui";
        public const string TuiRelativeDisplay = "src/Pi.Tui";
        public const string PackageJsonFileName = "package.json";
        public const string GlobalJsonFileName = "global.json";
        public const string NodeModulesFolderName = "node_modules";
        public const string TuiEntryPoint = "src/index.ts";
        public const string EnvFileName = ".env";
        public const string GitIgnoreFileName = ".gitignore";
        public const string AllEntriesPattern = "*";
        public const string JsonSearchPattern = "*.json";
        public const string JsonExtension = ".json";
        public const string TemporarySuffix = ".tmp-";
        public const string BackupSuffix = ".bak-";
        public const string ProgramsFolderName = "Programs";
        public const string OllamaFolderName = "Ollama";
        public const string OllamaWindowsExecutableName = "ollama.exe";
        public const string OllamaMacApplicationPath = "/Applications/Ollama.app/Contents/Resources/ollama";
        public const string OllamaUsrLocalBinPath = "/usr/local/bin/ollama";
        public const string OllamaUsrBinPath = "/usr/bin/ollama";
        public const string OllamaHomeBrewBinPath = "/opt/homebrew/bin/ollama";
        public const string OllamaHomeFolderName = ".ollama";
        public const string BinFolderName = "bin";
        public const string ArtifactsFolderName = "artifacts";
        public const string DebugFolderName = "debug";
        public const string HostLogFileName = "pi-host.log";
        public const char WindowsSeparator = '\\';
        public const char UnixSeparator = '/';
        public static readonly char[] SessionIdSafeSpecialCharacters = ['-', '_'];
        public const char SessionIdReplacementCharacter = '_';
    }

    /// <summary>Endereços HTTP e hosts locais.</summary>
    // Origem: URLs dos provedores e da API Ollama
    // Finalidade: endereços e hosts usados nas requisições ao backend.
    public static class Endpoints
    {
        public const string Generate = "/api/generate";
        public const string Chat = "/api/chat";
        public const string Tags = "/api/tags";
        public const string OllamaDownload = "https://ollama.com/download";
        public static readonly string[] LocalHosts = ["localhost", "127.0.0.1", "::1", "[::1]"];
    }

    /// <summary>Nomes de executáveis.</summary>
    // Origem: processos externos iniciados pelo Host
    // Finalidade: nomes de executáveis e sufixos de plataforma.
    public static class ProcessNames
    {
        public const string Ollama = "ollama";
        public const string OllamaWindowsExecutable = Paths.OllamaWindowsExecutableName;
        public const string WindowsExecutableSuffix = ".exe";
        public const string Bun = "bun";
        public const string Git = "git";
    }

    /// <summary>Nomes de variáveis de ambiente e caracteres do formato .env.</summary>
    // Origem: variáveis de ambiente lidas na inicialização
    // Finalidade: nomes de variáveis e caracteres do formato .env.
    public static class Env
    {
        public const string OllamaBaseUrl = "OLLAMA_BASE_URL";
        public const string PiModel = "PI_MODEL";
        public const string OllamaExecutable = "OLLAMA_EXECUTABLE";
        public const string OllamaAutoStart = "OLLAMA_AUTO_START";
        public const string OllamaStopOnExit = "OLLAMA_STOP_ON_EXIT";
        public const string PiLogLevel = "PI_LOG_LEVEL";
        public const string PiContextWindowTokens = "PI_CONTEXT_WINDOW_TOKENS";
        public const string PiSessionDir = "PI_SESSION_DIR";
        public const string PiAllowShell = "PI_ALLOW_SHELL";
        public const string PiDebug = "PI_DEBUG";
        public const string PiDebugLogDir = "PI_DEBUG_LOG_DIR";
        public const string PiWorkspace = "PI_WORKSPACE";
        public const string PiBackendDll = "PI_BACKEND_DLL";
        public const string PiSkipTui = "PI_SKIP_TUI";
        public const string PiTuiInspect = "PI_TUI_INSPECT";
        public const string PiHostLog = "PI_HOST_LOG";
        public const char CommentPrefix = '#';
        public const char AssignmentSeparator = '=';
        public static readonly char[] Quotes = ['"', '\''];
    }

    /// <summary>Seções do appsettings.json lidas pelo Host.</summary>
    // Origem: appsettings.json
    // Finalidade: seções lidas pelo Host para montar as opções.
    public static class Configuration
    {
        public const string OllamaSection = "Ollama";
        public const string AgentSection = "Agent";
        public const string ToolsSection = "Tools";
        public const string SessionsSection = "Sessions";
    }
}
