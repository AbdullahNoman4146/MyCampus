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

        public DbSet<RoomBooking> RoomBookings { get; set; }

        public DbSet<EventRegistration> EventRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RoomBooking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(r => r.CampusEvent)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}