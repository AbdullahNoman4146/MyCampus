namespace MyCampus.Services
{
    public interface IJsonImportService
    {
        Task SeedAllAsync();
        Task SeedSchedulesAsync();
        Task SeedRoomsAsync();
        Task SeedEventsAsync();
        Task SeedAnnouncementsAsync();
        Task SeedAssignmentsAsync();
        Task ImportHackathonResourcesAsync(bool clearExisting = true);
    }
}
