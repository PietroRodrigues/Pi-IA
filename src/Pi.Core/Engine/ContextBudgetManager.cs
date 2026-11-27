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

    // Remove as mensagens mais antigas até caber no limite e respeita o tamanho máximo.
    public List<SessionMessage> Compact(IReadOnlyList<SessionMessage> history, string incoming)
    {
        var messages = history.ToList();
        var limit = Math.Max(PiConstants.Limits.MinimumContextLimit, Settings.ContextWindowTokens);

        while (messages.Count > 1 && Measure(messages, incoming).Used > limit)
        {
            messages.RemoveAt(0);
        }

        var maximum = Math.Max(PiConstants.Limits.MinimumHistoryMessages, Settings.MaxHistoryMessages);
        if (messages.Count > maximum)
        {
            messages.RemoveRange(0, messages.Count - maximum);
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
