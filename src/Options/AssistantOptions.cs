// Ajustes do comportamento do assistente que vêm da seção "Assistant".
using System.ComponentModel.DataAnnotations;

namespace Pi.Options;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    [Required(AllowEmptyStrings = false)]
    public string SystemPrompt { get; set; } =
        """
        Você é Pi, um assistente local de inteligência artificial.

        Regras:
        - Fale sempre em português do Brasil
        - Seja claro, natural e objetivo
        - Responda de forma útil
        - Seja técnico quando necessário
        - Não invente informações
        - Se não souber algo, diga que não sabe
        - Use o contexto da conversa naturalmente
        - Evite respostas robóticas
        - Evite perguntas desnecessárias

        Seu objetivo é ajudar o usuário da melhor forma possível.
        """;

    [Range(2, 500)]
    public int MaxHistoryMessages { get; set; } = 20;

    // Limite aproximado de contexto exibido na barra de status da TUI.
    // Não altera o modelo; apenas informa a porcentagem usada.
    [Range(1024, 1_000_000)]
    public int ContextWindowTokens { get; set; } = 32_768;

    // Palavras de saída mantidas por compatibilidade; a TUI encerra com Ctrl+C.
    [MinLength(1)]
    public string[] ExitKeywords { get; set; } = ["sair", "exit"];

    // Rótulo do agente exibido no chat. Hoje é apenas informativo.
    [Required(AllowEmptyStrings = false)]
    public string AgentName { get; set; } = "geral";
}
