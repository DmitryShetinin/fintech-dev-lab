using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class OperationConfiguration : IEntityTypeConfiguration<Operation>
{
  public void Configure(EntityTypeBuilder<Operation> builder)
  {
    builder.ToTable("Operations");

    // Primary Key
    builder.HasKey(x => x.OperationId);

    // Properties
    builder.Property(x => x.OperationId)
        .HasMaxLength(100)
        .IsRequired();

    builder.Property(x => x.Amount)
        .HasPrecision(18, 2)
        .IsRequired();

    builder.Property(x => x.Currency)
        .HasMaxLength(3)
        .IsRequired();

    builder.Property(x => x.Description)
        .HasMaxLength(500);

    builder.Property(x => x.Status)
        .HasConversion<string>()
        .HasMaxLength(20)
        .IsRequired();

    builder.Property(x => x.ProviderPaymentId)
        .HasMaxLength(100);

    // Indexes
    builder.HasIndex(x => x.Status);

    builder.HasIndex(x => x.ProviderPaymentId)
        .IsUnique(false);

    // Relationships
    builder.HasMany(x => x.Events)
        .WithOne()
        .HasForeignKey(x => x.OperationId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
