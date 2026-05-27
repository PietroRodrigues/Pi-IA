using Pi.App.Tools;

namespace Pi.App.Core
{
    public class ToolRouter
    {

        private readonly List<ITool> _tools;
        
        public ToolRouter()
        {
            _tools = [
                new SearchFilesTool(),
                new OpenAppTool()
            ];
        }

        public async Task<string?> TryExecuteAsync(string input)
        {
            foreach (var tool in _tools)
            {
                if (tool.CanHandle(input))
                {
                    return await tool.Execute(input);
                }
            }

            return null;            
        }
        
        
    }

}