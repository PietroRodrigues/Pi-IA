// Parte da fonte única de literais compartilhados por Pi.Contracts, Pi.Core, Pi.Infrastructure e Pi.Host.
namespace Pi.Contracts.Constants;

public static partial class PiConstants
{

    /// <summary>Argumentos e comandos passados aos processos.</summary>
    // Origem: argumentos de processos externos
    // Finalidade: comandos, flags e sinais repassados ao Ollama/bun/git.
    public static class Commands
    {
        public const string OllamaServe = "serve";
        public const string BunInstall = "install";
        public const string BunFrozenLockfile = "--frozen-lockfile";
        public const string BunRun = "run";
        public const string GitCurrentBranch = "rev-parse --abbrev-ref HEAD";
    }

    /// <summary>Chaves, valores e formatos do payload JSON.</summary>
    // Origem: envelopes NDJSON e schemas de ferramentas
    // Finalidade: chaves e valores usados na serialização.
    public static class Json
    {
        public const char ObjectStart = '{';
        public const string EmptyObject = "{}";
        public const string EmptyToolSchema = "{\"type\":\"object\",\"properties\":{}}";
        public const string Response = "response";
        public const string Done = "done";
        public const string PromptEvalCount = "prompt_eval_count";
        public const string EvalCount = "eval_count";
        public const string Message = "message";
        public const string Content = "content";
        public const string ToolCalls = "tool_calls";
        public const string Function = "function";
        public const string Arguments = "arguments";
        public const string Role = "role";
        public const string Name = "name";
        public const string Id = "id";
        public const string Type = "type";
        public const string ToolCallId = "tool_call_id";
        public const string Path = "path";
        public const string FilePath = "filePath";
        public const string File = "file";
        public const string Offset = "offset";
        public const string Limit = "limit";
        public const string MaxBytes = "maxBytes";
        public const string NotFoundMarker = "not found";
        public static readonly string[] PathPropertyNames = [Path, FilePath, File];

        public const string ReadFileDescription =
            "Lê um arquivo de texto dentro do workspace, com paginação opcional por linha e limite de bytes.";

        public const string ReadFileSchema = """
            {
              "type": "object",
              "properties": {
                "path": { "type": "string", "description": "Caminho relativo à raiz do workspace." },
                "offset": { "type": "integer", "minimum": 0, "default": 0 },
                "limit": { "type": "integer", "minimum": 1, "maximum": 500, "default": 200 },
                "maxBytes": { "type": "integer", "minimum": 256 }
              },
              "required": ["path"]
            }
            """;
    }

    /// <summary>Papéis das mensagens enviadas ao modelo.</summary>
    // Origem: contrato de mensagens do modelo
    // Finalidade: papéis aceitos em system/user/assistant/tool.
    public static class Roles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string Tool = "tool";
    }

    /// <summary>Strings de protocolo ainda não centralizadas em Pi.Contracts.Protocol.</summary>
    // Origem: ponte Host/TUI e parsing de argumentos
    // Finalidade: separadores, flags e marcadores do protocolo.
    public static class Protocol
    {
        public const string PipeSeparator = "|";
        public const string EditArrow = "=>";
        public const string ClearReason = "clear";
        public const string ShutdownReason = "shutdown";
        public const string TrueValue = "1";
        public const string FalseValue = "0";
        public const string TrueWord = "true";
        public const string FalseWord = "false";
        public const string BridgeFlag = "--bridge";
        public const string BridgeFlagPrefix = "--bridge=";
        public const string BridgeTransportStdio = "stdio";
        public const string InspectorFlagPrefix = "--inspect=";
        public const string HostLogPrefix = "[Pi.Host] ";
        public const string UnknownBranch = "—";
        public const string AttemptsSeparator = "; ";
        public const char LineFeed = '\n';
        public const char CarriageReturn = '\r';
        public const char Space = ' ';
    }
}
