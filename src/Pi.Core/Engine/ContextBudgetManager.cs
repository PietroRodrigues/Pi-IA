// Cálculo de tokens, janela de contexto e compactação determinística do histórico.
// Não chama o modelo: a compactação é local, previsível e auditável.
using Pi.Contracts.Constants;
using Pi.Contracts.Models;

namespace Pi.Core.Engine;

public sealed record AgentSettings(
    string SystemPrompt,
    int ContextWindowTokens,
    int MaxHistoryMessages);

public sealed class ContextBudgetManager(AgentSettings settings)
{
    public AgentSettings Settings { get; } = settings;

    public int EstimateTokens(string text) =>
        Math.Max(PiConstants.Limits.MinimumTokenEstimate, text.Length / PiConstants.Limits.CharsPerToken);

    public ContextUsage Measure(IReadOnlyList<SessionMessage> history, string incoming)
    {
        var used = EstimateTokens(Settings.SystemPrompt) +
                   EstimateTokens(incoming) +
                   history.Sum(message => EstimateTokens(message.Text));

        var limit = Math.Max(PiConstants.Limits.MinimumContextLimit, Settings.ContextWindowTokens);
        return new ContextUsage(
            used,
            limit,
            Math.Min(PiConstants.Limits.MaxPercent, used * PiConstants.Limits.MaxPercent / limit));
    }

    // Remove blocos de turno inteiros até caber no limite e no teto de mensagens.
    // Um bloco começa em "user" e inclui toda a sequência assistant/tool seguinte.
    public List<SessionMessage> Compact(IReadOnlyList<SessionMessage> history, string incoming)
    {
        var blocks = NormalizeBlocks(history);
        var limit = Math.Max(PiConstants.Limits.MinimumContextLimit, Settings.ContextWindowTokens);

        while (blocks.Count > 1 && Measure(Flatten(blocks), incoming).Used > limit)
        {
            blocks.RemoveAt(0);
        }

        var maximum = Math.Max(PiConstants.Limits.MinimumHistoryMessages, Settings.MaxHistoryMessages);
        while (blocks.Count > 1 && Flatten(blocks).Count > maximum)
        {
            blocks.RemoveAt(0);
        }

        return Flatten(blocks);
    }

    private static List<List<SessionMessage>> NormalizeBlocks(IReadOnlyList<SessionMessage> history)
    {
        var blocks = new List<List<SessionMessage>>();
        List<SessionMessage>? current = null;

        foreach (var message in history)
        {
            if (message.Role == PiConstants.Roles.User || current is null)
            {
                current = [];
                blocks.Add(current);
            }

            current.Add(message);
        }

        return blocks
            .Select(NormalizeBlock)
            .Where(block => block.Count > 0)
            .ToList();
    }

    private static List<SessionMessage> NormalizeBlock(IReadOnlyList<SessionMessage> block)
    {
        var pending = new HashSet<string>(StringComparer.Ordinal);
        var kept = new List<SessionMessage>(block.Count);

        foreach (var message in block)
        {
            if (message.Role == PiConstants.Roles.Tool)
            {
                var id = message.ToolCallId;
                if (string.IsNullOrEmpty(id) || !pending.Remove(id))
                {
                    continue;
                }

                kept.Add(message);
                continue;
            }

            if (message.Role == PiConstants.Roles.Assistant && message.ToolCalls is not null)
            {
                var valid = message.ToolCalls
                    .Where(call => !string.IsNullOrEmpty(call.Id) && pending.Add(call.Id))
                    .ToList();

                kept.Add(valid.Count == 0
                    ? message with { ToolCalls = null }
                    : message with { ToolCalls = valid });
                continue;
            }

            kept.Add(message);
        }

        return kept
            .Select(message =>
            {
                if (message.ToolCalls is null) return message;

                var valid = message.ToolCalls
                    .Where(call => !string.IsNullOrEmpty(call.Id) && !pending.Contains(call.Id))
                    .ToList();

                return valid.Count == 0
                    ? message with { ToolCalls = null }
                    : message with { ToolCalls = valid };
            })
            .ToList();
    }

    private static List<SessionMessage> Flatten(IReadOnlyList<List<SessionMessage>> blocks)
    {
        var messages = new List<SessionMessage>();
        foreach (var block in blocks)
        {
            messages.AddRange(block);
        }

        return messages;
    }

    public string BuildPrompt(string input, IReadOnlyList<SessionMessage> history)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine(Settings.SystemPrompt);
        prompt.AppendLine();
        prompt.AppendLine(PiConstants.Prompt.ConversationHeader);
        prompt.AppendLine(PiConstants.Prompt.ConversationOpenTag);

        foreach (var message in history)
        {
            prompt.AppendLine(string.Format(PiConstants.Prompt.MessageTemplate, message.Role, message.Text));
        }

        prompt.AppendLine(PiConstants.Prompt.ConversationCloseTag);
        prompt.AppendLine();
        prompt.AppendLine(PiConstants.Prompt.UserInstruction);
        prompt.AppendLine(PiConstants.Prompt.UserInputOpenTag);
        prompt.AppendLine(input);
        prompt.AppendLine(PiConstants.Prompt.UserInputCloseTag);
        prompt.AppendLine();
        prompt.Append(PiConstants.Prompt.AssistantPrefix);

        return prompt.ToString();
    }
}
