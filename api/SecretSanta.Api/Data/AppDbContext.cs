using Microsoft.EntityFrameworkCore;

namespace SecretSanta.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<Participant> Participants => Set<Participant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exchange>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.HasMany(x => x.Participants)
                .WithOne(p => p.Exchange)
                .HasForeignKey(p => p.ExchangeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(p =>
        {
            p.Property(x => x.Name).HasMaxLength(100);
            p.Property(x => x.InviteToken).HasMaxLength(64);
            p.HasIndex(x => x.InviteToken).IsUnique();
            p.HasOne(x => x.Recipient)
                .WithMany()
                .HasForeignKey(x => x.RecipientId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
