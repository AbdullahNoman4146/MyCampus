using Microsoft.EntityFrameworkCore;
using MyCampus.Models;

namespace MyCampus.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }


        public DbSet<Schedule> Schedules { get; set; }

        public DbSet<Room> Rooms { get; set; }

        public DbSet<CampusEvent> Events { get; set; }

        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<Assignment> Assignments { get; set; }

    }
}