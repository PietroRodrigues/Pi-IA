namespace Pi.App.Tools
{
    public interface ITool
    {
        string Name { get; }

        bool CanHandle(string input);
        
        Task<string> Execute(string input);
        
    }
}