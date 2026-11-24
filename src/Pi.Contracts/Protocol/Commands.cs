// Comandos da TUI para o Pi.Host. O correlationId do envelope identifica o turno.
namespace Pi.Contracts.Protocol;

public static class CommandTypes
{
    public const string PromptSubmit = "prompt_submit";
    public const string InterruptTurn = "interrupt_turn";
    public const string ToolDecision = "tool_decision";
    public const string SessionSwitch = "session_switch";
    public const string SessionClear = "session_clear";
}

public sealed record PromptSubmit(string Text, string? SessionId = null);

public sealed record InterruptTurn(string? Reason = null);

public sealed record ToolDecision(string ToolRequestId, bool Approved, string? Reason = null);

public sealed record SessionSwitch(string SessionId);

public sealed record SessionClear(bool ClearHistory = true);
