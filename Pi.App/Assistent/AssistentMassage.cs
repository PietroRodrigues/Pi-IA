namespace Pi.App.Assistant
{
    public class AssistantMessage
    {
        public string Text { get; set; } = string.Empty;

        public AssistantMessageSource Source { get; set; }
    }
}