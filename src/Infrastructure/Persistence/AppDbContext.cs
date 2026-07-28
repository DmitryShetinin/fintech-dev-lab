// Infrastructure/Persistence/AppDbContext.cs





using Core.Models;
using Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{


  public DbSet<Operation> Operations => Set<Operation>(); public DbSet<OperationEvent> OperationEvents => Set<OperationEvent>();

  public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();



  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {

    modelBuilder.ApplyConfiguration(new OperationConfiguration());
    modelBuilder.ApplyConfiguration(new OperationEventConfiguration());
    modelBuilder.ApplyConfiguration(new PaymentAttemptConfiguration());

  }
}
