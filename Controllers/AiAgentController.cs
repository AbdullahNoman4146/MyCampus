using Microsoft.AspNetCore.Mvc;
using MyCampus.AI.Models;
using MyCampus.AI.Services;

namespace MyCampus.Controllers
{
    public class AiAgentController : Controller
    {
        private readonly ICampusAiService _aiService;

        public AiAgentController(ICampusAiService aiService)
        {
            _aiService = aiService;
        }

        // GET: /AiAgent
        public IActionResult Index()
        {
            return View();
        }

        // POST: /AiAgent/Ask
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AiPromptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Json(new AiResponse
                {
                    Message = "Please provide a query for the AI Agent.",
                    Success = false
                });
            }

            var response = await _aiService.ProcessPromptAsync(request.Prompt);
            return Json(response);
        }
    }
}
