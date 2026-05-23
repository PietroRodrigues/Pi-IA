using Pi.App.LLM;

namespace Pi.App.Agent
{
    public class AgentService
    {
        private readonly OllamaClient _ollama;

        public AgentService()
        {
            _ollama = new OllamaClient();
        }

        public async Task<string> AskAsync(string input)
        {
            return await _ollama.SendMessageAsync(input);
        }

    }
}