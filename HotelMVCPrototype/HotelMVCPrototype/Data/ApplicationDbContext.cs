using HotelMVCPrototype.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace HotelMVCPrototype.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<GuestAssignment> GuestAssignments { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Room>()
                   .HasIndex(r => r.Number)
                   .IsUnique();

            builder.Entity<GuestAssignment>()
                .Property(g => g.Id)
                .ValueGeneratedOnAdd();
        }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ServiceRequestItem> ServiceRequestItems { get; set; }

        public DbSet<RequestItem> RequestItems { get; set; }

      
        public DbSet<CleaningLog> CleaningLogs { get; set; }
        public DbSet<SecurityIncident> SecurityIncidents { get; set; }

        public DbSet<Guest> Guests { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<RoomIssue> RoomIssues { get; set; }





    }
}
