using Logistic_Shipment_tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistic_Shipment_tracker.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {

        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<TrackingUpdate> TrackingUpdates { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<DriverRating> DriverRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp with time zone");
                    }
                }
            }

            modelBuilder.Entity<User>(
                entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.HasIndex(e => e.Email).IsUnique();
                    entity.Property(e => e.Role).HasConversion<string>();
                });

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TrackingNumber).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentShipments)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssignedDriver)
                    .WithMany(u => u.AssignedShipments)
                    .HasForeignKey(e => e.AssignedDriverId)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<TrackingUpdate>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Shipment)
                    .WithMany(u => u.TrackingUpdates)
                    .HasForeignKey(e=> e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany(u => u.TrackingUpdates)
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();

                entity.HasOne(e => e.Shipment)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(e => e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e=> e.GeneratedByUser)
                    .WithMany(u => u.GeneratedReports)
                    .HasForeignKey(e => e.GeneratedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<Driver>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DriverRating>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Ensure That One customer can give rating once to each shipment
                entity.HasIndex(e => new { e.CustomerId, e.ShipmentId }).IsUnique();

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e=> e.Shipment)
                    .WithMany()
                    .HasForeignKey(e => e.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
