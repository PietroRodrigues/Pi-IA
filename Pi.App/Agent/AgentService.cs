using Pi.App.LLM;
using Pi.App.Tools;
using Pi.App.Core;

namespace Pi.App.Agent
{
    public class AgentService
    {
        private readonly OllamaClient _ollama;

        private readonly List<String> _conversationHistory = new();

        private readonly ToolRouter _toolRouter;

        public AgentService()
        {
            _ollama = new OllamaClient();
            _toolRouter = new ToolRouter();
        }

        public async Task<string> AskAsync(string input)
        {
           var toolResponse = await _toolRouter.TryExecuteAsync(input);

            if (toolResponse != null)
                return toolResponse;

            var history = string.Join("\n", _conversationHistory);

            var prompt = BuildPrompt(history, input);

            var response = await _ollama.SendMessageAsync(prompt);

            _conversationHistory.Add($"Usuário: {input}");
            _conversationHistory.Add($"Pi: {response}");

            if(_conversationHistory.Count > 20)
            {
                _conversationHistory.RemoveRange(0, _conversationHistory.Count - 20);
            }

            return response;
        }

        private string BuildPrompt(string history, string input)
        {
            return $"""

            {AgentConfig.SystemPrompt}

            Histórico da conversa:
            {history}

            Usuário:
            {input}

            Pi:
            """;
        }

    }
}