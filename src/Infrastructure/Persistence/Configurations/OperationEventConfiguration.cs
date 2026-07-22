using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class OperationEventConfiguration : IEntityTypeConfiguration<OperationEvent>
{
  public void Configure(EntityTypeBuilder<OperationEvent> builder)
  {
    builder.ToTable("OperationEvents");

    builder.HasKey(x => x.EventId);

    builder.Property(x => x.EventId)
        .ValueGeneratedOnAdd();

    builder.Property(x => x.OperationId)
        .IsRequired()
        .HasMaxLength(100);

    builder.Property(x => x.Type)
        .IsRequired()
        .HasMaxLength(50);

    builder.Property(x => x.FromStatus)
        .HasConversion<string>()
        .HasMaxLength(20);

    builder.Property(x => x.ToStatus)
        .HasConversion<string>()
        .HasMaxLength(20)
        .IsRequired();

    builder.Property(x => x.Message)
        .IsRequired()
        .HasMaxLength(500);

    builder.Property(x => x.OccurredAt)
        .IsRequired();

    builder.HasIndex(x => new
    {
      x.OperationId,
      x.EventId
    });

    builder.HasOne<Operation>()
        .WithMany(x => x.Events)
        .HasForeignKey(x => x.OperationId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
