using Pi.App.Core;

namespace Pi.App.Tools
{
    public class OpenAppTool : ITool
    {
        public string Name => ToolNames.OpenApp;

        public bool CanHandle(string input)
        {
            return input.StartsWith(
                "abra",
                StringComparison.OrdinalIgnoreCase
            );
        }

        public async Task<string> Execute(string input)
        {            
            return await Task.Run(() =>
            {
                return "Tool de abrir app ainda não implementada.";
            });
        }
    }
}