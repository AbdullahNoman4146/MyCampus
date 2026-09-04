using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using MyCampus.AI.Services;
using MyCampus.Data;
using MyCampus.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

// Register JSON Seed Import Service & AI Agent Service
builder.Services.AddScoped<IJsonImportService, JsonImportService>();
builder.Services.AddScoped<ICampusAiService, CampusAiService>();

var app = builder.Build();

// Auto-migrate and seed initial JSON data into SQL Server if empty
using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedDatabaseAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

