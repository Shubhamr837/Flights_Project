using Flights_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Flights_Project.Data;

public class FlightHistoryDBContext : DbContext
{
    public DbSet<Subscription> subscriptions { get; set; }
    public DbSet<Route> routes { get; set; }
    public DbSet<Flight> flights { get; set; }

    public FlightHistoryDBContext(DbContextOptions<FlightHistoryDBContext> options) : base(options)
    {
    }

    /*
     * This function specifies the requirements for an Entity.
     */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>()
            .HasKey(s => new { s.AgencyId, s.OriginCityId, s.DestinationCityId });

        modelBuilder.Entity<Route>()
            .HasKey(r => r.RouteId);

        modelBuilder.Entity<Route>()
            .HasMany(r => r.Flights)
            .WithOne(f => f.Route)
            .HasForeignKey(f => f.RouteId);

        modelBuilder.Entity<Flight>()
            .HasKey(f => f.FlightId);

        modelBuilder.Entity<Flight>()
            .HasOne(f => f.Route)
            .WithMany(r => r.Flights)
            .HasForeignKey(f => f.RouteId);
    }
}
