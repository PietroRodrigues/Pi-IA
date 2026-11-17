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

    [MinLength(1)]
    public string[] ExitKeywords { get; set; } = ["sair", "exit"];

    /// <summary>
    /// Rótulo do agente exibido no chat. Hoje é apenas informativo.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string AgentName { get; set; } = "geral";
}
