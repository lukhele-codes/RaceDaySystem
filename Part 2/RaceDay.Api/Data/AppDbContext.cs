using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table names match the Part 1 SQL script exactly.
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.Property(x => x.RoleName).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.RoleName).IsUnique();
            e.HasData(
                new Role { RoleId = 1, RoleName = "Organiser" },
                new Role { RoleId = 2, RoleName = "Participant" });
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Role).WithMany(r => r.Users)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(10).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.Property(x => x.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.MaxParticipants).HasDefaultValue(100);
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Confirmed");
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.Property(x => x.CapturedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.EnrolmentId).IsUnique(); // strict 1:1 with Enrolments
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CapturedBy).WithMany(u => u.CapturedResults)
                .HasForeignKey(x => x.CapturedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
