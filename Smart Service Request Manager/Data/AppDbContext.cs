using Microsoft.EntityFrameworkCore;
using Smart_Service_Request_Manager.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ServiceRequest relationships
        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.CreatedByUser)
            .WithMany()
            .HasForeignKey(sr => sr.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(sr => sr.AssignedToUser)
            .WithMany()
            .HasForeignKey(sr => sr.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}