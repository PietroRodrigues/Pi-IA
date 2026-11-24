// Eventos do Pi.Host para a TUI. stream_delta com Final encerra o turno;
// tool_request espera um tool_decision antes de executar a ferramenta.
using Pi.Contracts.Models;

namespace Pi.Contracts.Protocol;

public static class EventTypes
{
    public const string SessionSnapshot = "session_snapshot";
    public const string StreamDelta = "stream_delta";
    public const string ToolRequest = "tool_request";
    public const string ToolResult = "tool_result";
    public const string ContextMetrics = "context_metrics";
    public const string ErrorEvent = "error_event";
}

public static class ErrorCodes
{
    public const string InvalidMessage = "invalid_message";
    public const string UnsupportedVersion = "unsupported_version";
    public const string EmptyPrompt = "empty_prompt";
    public const string UnknownCommand = "unknown_command";
    public const string ToolDenied = "tool_denied";
    public const string ToolTimeout = "tool_timeout";
    public const string LlmFailure = "llm_failure";
    public const string SessionFailure = "session_failure";
    public const string Internal = "internal";
}

public sealed record SessionSnapshotEvent(SessionSnapshot Session);

public sealed record StreamDelta(string Text, bool Final = false);

public sealed record ToolRequest(
    string ToolRequestId,
    string Name,
    string Input,
    string Risk,
    bool RequiresDecision);

public sealed record ToolResult(string ToolRequestId, string Name, bool Ok, string Text);

public sealed record ContextMetrics(int Used, int Limit, double Percent);

public sealed record ErrorInfo(string Code, string Message);

public sealed record SessionReady(string SessionId);
