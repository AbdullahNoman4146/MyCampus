namespace MyCampus.AI.Models
{
    public class AiResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? ToolExecuted { get; set; }
        public bool Success { get; set; } = true;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AiPromptRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}
