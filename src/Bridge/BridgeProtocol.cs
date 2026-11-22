// As mensagens que atravessam a ponte. Cada record vira uma linha NDJSON
// com nomes em camelCase: RequestId -> requestId.
namespace Pi.Bridge;

// Contrato NDJSON entre a TUI e o backend. As convenções de nomenclatura
// vêm de System.Text.Json.JsonSerializerDefaults.Web:
// Type vira type, RequestId vira requestId, etc.

// TUI -> C#
public sealed record PromptInput(string Type, string RequestId, string Text);

public sealed record CancelInput(string Type, string? RequestId);

public sealed record ClearInput(string Type);

// C# -> TUI
public sealed record ReadyEvent(string Model, string Agent, string Session, string Cwd, string Branch)
{
    public string Type => "ready";
}

public sealed record TokenEvent(string RequestId, string Text)
{
    public string Type => "token";
}

public sealed record ThoughtEvent(string RequestId, string Text)
{
    public string Type => "thought";
}

public sealed record ToolCallEvent(string RequestId, string Name, string Input)
{
    public string Type => "tool_call";
}

public sealed record ToolResultEvent(string RequestId, string Name, bool Ok, string Text)
{
    public string Type => "tool_result";
}

public sealed record ContextStatsEvent(int Used, int Limit, double Percent)
{
    public string Type => "context_stats";
}

public sealed record DoneEvent(string RequestId, string Text, bool Cancelled = false)
{
    public string Type => "done";
}

public sealed record ErrorEvent(string RequestId, string Message)
{
    public string Type => "error";
}
