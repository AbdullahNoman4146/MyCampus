using MyCampus.AI.Models;

namespace MyCampus.AI.Services
{
    public interface ICampusAiService
    {
        Task<AiResponse> ProcessPromptAsync(string prompt);
    }
}
