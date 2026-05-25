using Pi.App.LLM;
using Pi.App.Tools;
using Pi.App.Core;

namespace Pi.App.Agent
{
    public class AgentService
    {
        private readonly OllamaClient _ollama;

        private readonly List<ITool> _tools;

        public AgentService()
        {
            _ollama = new OllamaClient();

            _tools = [
                new SearchFilesTool()
            ];
        }

        public async Task<string> AskAsync(string input)
        {
            input = input.ToLower();

            //Procura arqivos
            if (input.StartsWith("procure"))
            {
                var searchTerm = input.Replace("procure", "").Trim();

                var tool = _tools.FirstOrDefault(x => x.Name == ToolNames.SearchFiles);

                if (tool != null)
                    return await tool.Execute(searchTerm);
            }

            //fallback para IA

            var prompt = BuildPrompt(input);

            var response = await _ollama.SendMessageAsync(prompt);

            return response;
        }

        private string BuildPrompt(string input)
        {
            return $"""
            Você é a Pi, um assistente local.
            Responda de forma objetiva e natural.
            Usuário: {input}
            """;
        }

    }
}