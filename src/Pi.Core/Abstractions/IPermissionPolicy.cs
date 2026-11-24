// Política de aprovação por ação/caminho. A decisão `Ask` exige tool_decision da TUI.
namespace Pi.Core.Abstractions;

public enum PermissionDecision
{
    Allow = 0,
    Auto = 0,
    Ask = 1,
    Deny = 2
}

public interface IPermissionPolicy
{
    PermissionDecision Decide(IAgentTool tool, string input);
}
