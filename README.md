# MyCampus — Intelligent University Platform

An intelligent campus management and academic assistant platform powered by an AI agent that understands, queries, and acts on real-time university campus data stored in Microsoft SQL Server.

---

## 1. Project Overview

**MyCampus** is a unified university operations and student productivity web platform engineered to eliminate scattered campus notices, conflicting schedules, and untracked academic deadlines. Built on ASP.NET Core MVC and Microsoft SQL Server, the application centralizes 5 core academic systems: class schedules, room availability & booking, campus events & registration, assignments with due dates, and department announcements. On top of this relational foundation, MyCampus embeds an autonomous AI Agent powered by Microsoft Semantic Kernel that directly queries and mutates the live database via function-calling tools, enabling students and staff to check schedules, search available facilities, book classrooms, track coursework deadlines, and register for campus events through intuitive natural-language conversations.

---

## 2. Tech Stack

- **Languages:** C# (.NET 10), HTML5, CSS3 (Custom Design System), JavaScript (ES6+)
- **Backend Framework:** ASP.NET Core MVC 10.0
- **ORM / Data Access:** Entity Framework Core 10.0 with SQL Server Provider & EF Core Migrations
- **Database:** Microsoft SQL Server (supports LocalDB, Express, and Standard/Enterprise editions)
- **AI Agent / LLM Orchestration:** Microsoft Semantic Kernel (`Microsoft.SemanticKernel` v1.73.0) with OpenAI Chat Completion (`gpt-4o-mini` / `gpt-4o`) and integrated tool-calling plugins
- **Icons & Styling:** Bootstrap 5.3 + Bootstrap Icons 1.11.3 (Dark theme, solid palette, responsive grid)

---

## 3. Setup Instructions

Follow these exact steps to clone, configure, and run the project locally.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 9+) installed
- [Microsoft SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (comes standard with Visual Studio or SQL Server Express)

### Step-by-step Run Commands

1. **Clone the repository:**
   ```bash
   git clone https://github.com/AbdullahNoman4146/MyCampus
   cd MyCampus
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Verify / Configure Connection String (Optional):**
   The default connection string in `appsettings.json` points to SQL Server LocalDB (`Server=(localdb)\mssqllocaldb;Database=MyCampusDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`).
   If using a dedicated SQL Server instance, edit `appsettings.json` or set `ConnectionStrings__DefaultConnection`.

4. **Build the solution:**
   ```bash
   dotnet build
   ```

5. **Start the application:**
   ```bash
   dotnet run
   ```
   > **Automatic Database Migration & Seeding:** On first launch, `Program.cs` automatically executes EF Core migrations and seeds all initial campus datasets (schedules, rooms, events, announcements, assignments) from the `data/` directory into SQL Server.

6. **Open in browser:**
   Navigate to [http://localhost:5099](https://localhost:7148/) (or the URL printed in your console).

---

## 4. Environment Variables

The project uses `.env.example` to document all necessary environment variables. **No real API keys are committed to the repository.**

### Required Environment Keys

| Variable | Description | Example Value |
|---|---|---|
| `OpenAI__ApiKey` | Your OpenAI API key for Semantic Kernel tool orchestration | `sk-proj-xxxxxxxxxxxxxxxxxxxx` |
| `OpenAI__ModelId` | OpenAI model identifier | `gpt-4o-mini` |
| `ConnectionStrings__DefaultConnection` *(Optional)* | Custom SQL Server connection string | `Server=.;Database=MyCampusDB;Trusted_Connection=True;TrustServerCertificate=True` |

### Setting Environment Variables

You can copy `.env.example` to `.env` or set them in your terminal before running:

**PowerShell (Windows):**
```powershell
$env:OpenAI__ApiKey="your-actual-openai-api-key"
$env:OpenAI__ModelId="gpt-4o-mini"
dotnet run
```

**Bash (Linux / macOS):**
```bash
export OpenAI__ApiKey="your-actual-openai-api-key"
export OpenAI__ModelId="gpt-4o-mini"
dotnet run
```

*Alternatively, you can provide your key inside `appsettings.json` under `"OpenAI": { "ApiKey": "your-key-here" }` for local testing.*

---

## 5. How to Use the AI Agent

Access the AI Campus Assistant by navigating to `/AiAgent` in the sidebar or clicking **AI Assistant** on the dashboard.

The agent connects directly to live SQL Server data through Semantic Kernel functions and supports both natural-language queries and multi-step actions.

### Example Prompts & Questions to Ask:

- **Class Schedules:**
  - *"What classes do I have today?"*
  - *"When is my CSE 4113 class, and which room is it in?"*
  - *"Show me all Sunday schedules for section B."*

- **Room Discovery & Bookings:**
  - *"Is room 7A03 available right now?"*
  - *"Find an available lab with more than 30 computers."*
  - *"Book room 7A04 for project group study on Tuesday from 10:00 to 12:00."*
  - *"List all my active room bookings."*

- **Assignments & Deadlines:**
  - *"What assignments are due this week?"*
  - *"Do I have any pending assignments for CSE 4114?"*
  - *"Which assignment is worth the most marks?"*

- **Campus Events & Registration:**
  - *"What events are coming up this month?"*
  - *"Who is organizing the CSE Carnival?"*
  - *"Register me for the Soft Computing Mid-Term Review Session."*

- **Announcements & Notices:**
  - *"Are there any urgent announcements regarding room changes or repairs?"*
  - *"What are the topics for the Soft Computing midterm exam?"*
