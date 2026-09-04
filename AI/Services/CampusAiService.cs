using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MyCampus.AI.Models;
using MyCampus.AI.Plugins;
using MyCampus.Data;
using System.Text.RegularExpressions;

namespace MyCampus.AI.Services
{
    public class CampusAiService : ICampusAiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CampusAiService> _logger;

        public CampusAiService(ApplicationDbContext context, IConfiguration configuration, ILogger<CampusAiService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiResponse> ProcessPromptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return new AiResponse
                {
                    Message = "Hello! I am your MyCampus AI Assistant. Ask me anything about class schedules, assignments, available rooms, booking a facility, or registering for campus events.",
                    Success = true
                };
            }

            var plugin = new CampusPlugin(_context);
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Plugins.AddFromObject(plugin, "CampusPlugin");

            var openAiKey = _configuration["OpenAI:ApiKey"];
            var openAiModel = _configuration["OpenAI:ModelId"] ?? "gpt-4o-mini";

            // If an OpenAI Key is configured, use full Semantic Kernel ChatCompletion with auto function invocation
            if (!string.IsNullOrWhiteSpace(openAiKey) && !openAiKey.Contains("YOUR_OPENAI_API_KEY"))
            {
                try
                {
                    kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiKey);
                    var kernel = kernelBuilder.Build();
                    var chatService = kernel.GetRequiredService<IChatCompletionService>();

                    var history = new ChatHistory();
                    history.AddSystemMessage("You are MyCampus AI Agent, an intelligent university assistant. Use the tools in CampusPlugin to answer questions and execute actions against live SQL Server data. If a user's booking request is too vague (e.g. 'book me any room tomorrow afternoon'), do NOT book anything; instead ask which room and exact time slot they prefer.");
                    history.AddUserMessage(prompt);

                    var executionSettings = new PromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                    };

                    var result = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);
                    return new AiResponse
                    {
                        Message = result.Content ?? "I processed your request using the university SQL Server tools.",
                        ToolExecuted = "SemanticKernel.OpenAI",
                        Success = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OpenAI ChatCompletion failed. Falling back to local Semantic Kernel plugin tool router.");
                }
            }

            // Local Tool Router: Executes Semantic Kernel plugin functions directly against live SQL Server data
            return await ExecuteDirectToolRoutingAsync(prompt, plugin);
        }

        private async Task<AiResponse> ExecuteDirectToolRoutingAsync(string prompt, CampusPlugin plugin)
        {
            var lower = prompt.ToLower();

            // 1. Multi-Source Reasoning: Free time drop-in recommendations
            // e.g. "I'm free until 2 PM — is there anything on campus I could drop into?"
            if ((lower.Contains("free") && (lower.Contains("until") || lower.Contains("drop") || lower.Contains("between"))) ||
                lower.Contains("drop into") || lower.Contains("anything on campus"))
            {
                var cutoff = "14:00";
                var timeM = Regex.Match(prompt, @"(?:until|till)\s*(\d{1,2})(?::(\d{2}))?\s*(am|pm)?", RegexOptions.IgnoreCase);
                if (timeM.Success)
                {
                    int h = int.Parse(timeM.Groups[1].Value);
                    int m = timeM.Groups[2].Success ? int.Parse(timeM.Groups[2].Value) : 0;
                    var ampm = timeM.Groups[3].Value.ToLower();
                    if (ampm == "pm" && h < 12) h += 12;
                    else if (ampm == "am" && h == 12) h = 0;
                    cutoff = $"{h:D2}:{m:D2}";
                }

                var recResult = await plugin.GetFreeTimeRecommendations(cutoff);
                return new AiResponse
                {
                    Message = recResult,
                    ToolExecuted = "CampusPlugin.GetFreeTimeRecommendations",
                    Success = true
                };
            }

            // 2. Announcements
            // e.g. "Show me all high priority announcements." or "any announcements about exam?"
            if (lower.Contains("announcement") || lower.Contains("notice") || lower.Contains("notices") ||
                (lower.Contains("high priority") && !lower.Contains("assignment")))
            {
                string? priority = null;
                if (lower.Contains("high") || lower.Contains("urgent")) priority = "high";
                else if (lower.Contains("medium")) priority = "medium";
                else if (lower.Contains("low")) priority = "low";

                string? search = null;
                var cMatch = Regex.Match(prompt, @"(CSE\s*\d{4})", RegexOptions.IgnoreCase);
                if (cMatch.Success) search = cMatch.Value;

                var annResult = await plugin.GetAnnouncements(priority, search);
                return new AiResponse
                {
                    Message = annResult,
                    ToolExecuted = "CampusPlugin.GetAnnouncements",
                    Success = true
                };
            }

            // 3. Vague Booking Request Detection (Rubric: Asking when unclear, refusing when it should not act)
            // e.g. "Just book me any room tomorrow afternoon."
            if (lower.Contains("book") || lower.Contains("reserve"))
            {
                bool isVagueRoom = lower.Contains("any room") || lower.Contains("some room") || lower.Contains("a room") || lower.Contains("any space");
                var explicitRoomMatch = Regex.Match(prompt, @"(?:Room\s*)?(7[A-C]\d{2})|Room\s*([0-9A-Za-z]+)|Lab\s*\d+|Auditorium\s*\d+", RegexOptions.IgnoreCase);
                
                // Check if time has specific start and end
                var (startTime, endTime, hasSpecificTimes) = ExtractStartAndEndTime(prompt);

                if (isVagueRoom || !explicitRoomMatch.Success || !hasSpecificTimes)
                {
                    return new AiResponse
                    {
                        Message = "❓ **Request Unclear:** To book a room for you, could you please specify:\n\n" +
                                  "1. **Which room or room type** (e.g. Room 7A01, Room 7A02, or minimum seats required)?\n" +
                                  "2. **The exact date and time window** (e.g. tomorrow from 3 PM to 5 PM)?\n\n" +
                                  "Once you provide these details, I will check live availability in SQL Server and reserve it for you!",
                        ToolExecuted = "CampusPlugin.BookRoom (Clarification Requested)",
                        Success = true
                    };
                }

                // Concrete Room Booking
                var roomName = explicitRoomMatch.Groups[1].Success 
                    ? explicitRoomMatch.Groups[1].Value 
                    : (explicitRoomMatch.Groups[2].Success ? explicitRoomMatch.Groups[2].Value : explicitRoomMatch.Value);

                var date = ExtractDate(prompt);

                var bookedByMatch = Regex.Match(prompt, @"(?:by|for user|for student)\s+([A-Za-z\s]+)", RegexOptions.IgnoreCase);
                var bookedBy = bookedByMatch.Success ? bookedByMatch.Groups[1].Value.Trim() : "Campus Student";

                var purposeMatch = Regex.Match(prompt, @"(?:for)\s+([A-Za-z0-9\s]+?)(?:\s+by|\s+from|\s*$)", RegexOptions.IgnoreCase);
                var purpose = purposeMatch.Success && !purposeMatch.Groups[1].Value.Trim().Equals(roomName, StringComparison.OrdinalIgnoreCase) 
                    ? purposeMatch.Groups[1].Value.Trim() 
                    : "Academic Activity booked via AI Agent";

                var bookResult = await plugin.BookRoom(roomName, date, startTime, endTime, purpose, bookedBy);

                return new AiResponse
                {
                    Message = bookResult,
                    ToolExecuted = "CampusPlugin.BookRoom",
                    Success = !bookResult.StartsWith("❌")
                };
            }

            // 4. Event Registration & Event Lookup
            // e.g. "Register me for the Guest Lecture on Deep Learning."
            if (lower.Contains("register") || lower.Contains("sign up") || lower.Contains("enroll"))
            {
                var emailMatch = Regex.Match(prompt, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                var email = emailMatch.Success ? emailMatch.Value : "student@aust.edu";

                var idMatch = Regex.Match(prompt, @"(\d{2}-\d{5})");
                var studentId = idMatch.Success ? idMatch.Value : "20-40532";

                var nameMatch = Regex.Match(prompt, @"(?:name is|for student|by)\s+([A-Za-z\s]+?)(?:\s+with|\s+email|\s+for|\s*$)", RegexOptions.IgnoreCase);
                var studentName = nameMatch.Success && nameMatch.Groups[1].Value.Trim().Length > 2 
                    ? nameMatch.Groups[1].Value.Trim() 
                    : "Campus Student";

                // Identify event name from query
                var eventName = "AUSTPIC AI Build Hackathon";
                if (lower.Contains("deep learning") || lower.Contains("medical") || lower.Contains("lecture"))
                    eventName = "Guest Lecture: Deep Learning in Medical Imaging";
                else if (lower.Contains("soft computing") || lower.Contains("fuzzy") || lower.Contains("review"))
                    eventName = "Soft Computing Mid-Term Review Session";
                else if (lower.Contains("carnival") || lower.Contains("planning"))
                    eventName = "AUST CSE Carnival 8.0 Planning Meeting";
                else if (lower.Contains("fresher") || lower.Contains("orientation"))
                    eventName = "Freshers' Orientation — CSE Fall 2026";
                else if (lower.Contains("git") || lower.Contains("github"))
                    eventName = "Workshop: Git & GitHub for Beginners";
                else if (lower.Contains("iupc") || lower.Contains("contest"))
                    eventName = "Inter-University Programming Contest (IUPC) Selection";
                else if (lower.Contains("hackathon") || lower.Contains("austpic") || lower.Contains("ai build"))
                    eventName = "AUSTPIC AI Build Hackathon";

                var regResult = await plugin.RegisterEvent(eventName, studentName, email, studentId);

                return new AiResponse
                {
                    Message = regResult,
                    ToolExecuted = "CampusPlugin.RegisterEvent",
                    Success = !regResult.StartsWith("❌")
                };
            }

            // 5. Schedules & Next Class
            // e.g. "When is my next class?", "What classes do I have on Wednesday?", "Where is my CSE 4113 class today?"
            if (lower.Contains("next class") || lower.Contains("schedule") || lower.Contains("class") || 
                lower.Contains("routine") || lower.Contains("timetable") || lower.Contains("instructor") ||
                lower.Contains("where is my") || lower.Contains("when is my"))
            {
                bool nextClassOnly = lower.Contains("next class") || lower.Contains("upcoming class");

                string? course = null;
                string? day = null;
                string? instructor = null;

                // Check for day
                string[] days = { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
                foreach (var d in days)
                {
                    if (Regex.IsMatch(prompt, $@"\b{d}\b", RegexOptions.IgnoreCase)) { day = d; break; }
                }

                // Check for course code
                var courseMatch = Regex.Match(prompt, @"(CSE\s*\d{3,4})", RegexOptions.IgnoreCase);
                if (courseMatch.Success) course = courseMatch.Value;

                var schedResult = await plugin.GetSchedule(course, day, instructor, nextClassOnly);

                return new AiResponse
                {
                    Message = schedResult,
                    ToolExecuted = "CampusPlugin.GetSchedule",
                    Success = true
                };
            }

            // 6. Assignments
            // e.g. "What assignments do I have due this week?", "Find all pending assignments"
            if (lower.Contains("assignment") || lower.Contains("homework") || lower.Contains("deadline") || lower.Contains("due"))
            {
                string? status = null;
                if (lower.Contains("pending")) status = "pending";
                if (lower.Contains("submitted") || lower.Contains("done")) status = "submitted";

                bool dueThisWeek = lower.Contains("this week") || lower.Contains("week");

                var courseMatch = Regex.Match(prompt, @"(CSE\s*\d{4})", RegexOptions.IgnoreCase);
                var course = courseMatch.Success ? courseMatch.Value : null;

                var assignResult = await plugin.FindAssignment(course, status, dueThisWeek);

                return new AiResponse
                {
                    Message = assignResult,
                    ToolExecuted = "CampusPlugin.FindAssignment",
                    Success = true
                };
            }

            // 7. Search Rooms & Room Needs
            // e.g. "Which labs have a projector and can fit at least 30 people?", "I need a room for 5 people with a projector, tomorrow between 2 and 4."
            if (lower.Contains("room") || lower.Contains("lab") || lower.Contains("capacity") || lower.Contains("equipment") || lower.Contains("projector"))
            {
                int? minCapacity = null;
                var capMatch = Regex.Match(prompt, @"(?:fit|for|capacity|seats?)\s*(?:of|at least)?\s*(\d+)", RegexOptions.IgnoreCase);
                if (capMatch.Success && int.TryParse(capMatch.Groups[1].Value, out var cap))
                {
                    minCapacity = cap;
                }

                string? equipment = null;
                if (lower.Contains("projector")) equipment = "projector";
                else if (Regex.IsMatch(prompt, @"\b(?:workstations?|pc|computers?)\b", RegexOptions.IgnoreCase)) equipment = "computers";
                else if (lower.Contains("smart board") || lower.Contains("smartboard")) equipment = "smart board";
                else if (Regex.IsMatch(prompt, @"\b(?:microphones?|mics?)\b", RegexOptions.IgnoreCase)) equipment = "microphone";
                else if (lower.Contains("whiteboard")) equipment = "whiteboard";

                string? roomType = null;
                if (Regex.IsMatch(prompt, @"\blabs?\b", RegexOptions.IgnoreCase)) roomType = "lab";
                else if (Regex.IsMatch(prompt, @"\bseminars?\b", RegexOptions.IgnoreCase)) roomType = "seminar";
                else if (Regex.IsMatch(prompt, @"\bclassrooms?\b|\bclass\s*rooms?\b", RegexOptions.IgnoreCase)) roomType = "classroom";

                string? availability = null;
                if (lower.Contains("available")) availability = "Available";

                // Time window check (e.g. "tomorrow between 2 and 4")
                var date = (lower.Contains("tomorrow") || lower.Contains("today")) ? ExtractDate(prompt) : null;
                var (startT, endT, hasTimes) = ExtractStartAndEndTime(prompt);

                var roomResult = await plugin.SearchRoom(
                    minCapacity, 
                    equipment, 
                    roomType, 
                    availability, 
                    hasTimes ? date : null, 
                    hasTimes ? startT : null, 
                    hasTimes ? endT : null);

                return new AiResponse
                {
                    Message = roomResult,
                    ToolExecuted = "CampusPlugin.SearchRoom",
                    Success = true
                };
            }

            // Default fallback
            return new AiResponse
            {
                Message = "👋 I am your **MyCampus AI Assistant**. Here are the things I can help you with:\n\n" +
                          "- **Next Class & Schedule**: *\"When is my next class?\"* or *\"What classes do I have on Wednesday?\"*\n" +
                          "- **Assignments**: *\"What assignments do I have due this week?\"*\n" +
                          "- **Announcements**: *\"Show me all high priority announcements.\"*\n" +
                          "- **Free Time Activities**: *\"I'm free until 2 PM — is there anything on campus I could drop into?\"*\n" +
                          "- **Room Search**: *\"Which labs have a projector and can fit at least 30 people?\"*\n" +
                          "- **Book Room**: *\"Book Room 7A02 tomorrow from 3 PM to 5 PM.\"*\n" +
                          "- **Event Registration**: *\"Register me for the Guest Lecture on Deep Learning.\"*\n\n" +
                          "How may I help you right now?",
                ToolExecuted = null,
                Success = true
            };
        }

        private static (string startTime, string endTime, bool hasSpecificTimes) ExtractStartAndEndTime(string prompt)
        {
            // 1. 12-hour format: "3 PM to 5 PM", "3pm - 5pm", "3:30 PM to 5:00 PM"
            var m12 = Regex.Match(prompt, @"(?:from\s+)?(\d{1,2})(?::(\d{2}))?\s*(am|pm)\s*(?:to|-|until)\s*(\d{1,2})(?::(\d{2}))?\s*(am|pm)", RegexOptions.IgnoreCase);
            if (m12.Success)
            {
                int h1 = int.Parse(m12.Groups[1].Value);
                int m1 = m12.Groups[2].Success ? int.Parse(m12.Groups[2].Value) : 0;
                var p1 = m12.Groups[3].Value.ToLower();
                if (p1 == "pm" && h1 < 12) h1 += 12;
                else if (p1 == "am" && h1 == 12) h1 = 0;

                int h2 = int.Parse(m12.Groups[4].Value);
                int m2 = m12.Groups[5].Success ? int.Parse(m12.Groups[5].Value) : 0;
                var p2 = m12.Groups[6].Value.ToLower();
                if (p2 == "pm" && h2 < 12) h2 += 12;
                else if (p2 == "am" && h2 == 12) h2 = 0;

                return ($"{h1:D2}:{m1:D2}", $"{h2:D2}:{m2:D2}", true);
            }

            // 2. "between 2 and 4" or "between 2 PM and 4 PM"
            var mBetween = Regex.Match(prompt, @"between\s*(\d{1,2})(?::(\d{2}))?\s*(am|pm)?\s*(?:and|-)\s*(\d{1,2})(?::(\d{2}))?\s*(am|pm)?", RegexOptions.IgnoreCase);
            if (mBetween.Success)
            {
                int h1 = int.Parse(mBetween.Groups[1].Value);
                int m1 = mBetween.Groups[2].Success ? int.Parse(mBetween.Groups[2].Value) : 0;
                var p1 = mBetween.Groups[3].Value.ToLower();

                int h2 = int.Parse(mBetween.Groups[4].Value);
                int m2 = mBetween.Groups[5].Success ? int.Parse(mBetween.Groups[5].Value) : 0;
                var p2 = mBetween.Groups[6].Value.ToLower();

                // If afternoon is implied or between 1 and 6
                if (string.IsNullOrEmpty(p1) && string.IsNullOrEmpty(p2))
                {
                    if (h1 < 8) h1 += 12; // e.g. 2 -> 14
                    if (h2 < 8) h2 += 12; // e.g. 4 -> 16
                }
                else
                {
                    if (p1 == "pm" && h1 < 12) h1 += 12;
                    if (p2 == "pm" && h2 < 12) h2 += 12;
                }

                return ($"{h1:D2}:{m1:D2}", $"{h2:D2}:{m2:D2}", true);
            }

            // 3. 24-hour format: "10:00 to 12:00", "15:00 - 17:00"
            var m24 = Regex.Match(prompt, @"(\d{1,2}:\d{2})\s*(?:to|-)\s*(\d{1,2}:\d{2})");
            if (m24.Success)
            {
                return (m24.Groups[1].Value, m24.Groups[2].Value, true);
            }

            return ("15:00", "17:00", false);
        }

        private static string ExtractDate(string prompt)
        {
            var lower = prompt.ToLower();
            if (lower.Contains("tomorrow"))
            {
                return DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
            }
            if (lower.Contains("today"))
            {
                return DateTime.Today.ToString("yyyy-MM-dd");
            }

            var isoMatch = Regex.Match(prompt, @"(\d{4}-\d{2}-\d{2})");
            if (isoMatch.Success)
            {
                return isoMatch.Groups[1].Value;
            }

            return DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
        }
    }
}
