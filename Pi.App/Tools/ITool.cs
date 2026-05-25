namespace Pi.App.Tools
{
    public interface ITool
    {
        string Name { get; }
        
        Task<string> Execute(string input);
        
    }
}