using LoanAmortization.Domain.Entities;
using LoanAmortization.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LoanAmortization.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Simulation> Simulations => Set<Simulation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new SimulationConfiguration());
    }
}
