using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class PaymentAttemptConfiguration :
    IEntityTypeConfiguration<PaymentAttempt>
{
  public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
  {
    builder.ToTable("PaymentAttempts");


    // Primary Key

    builder.HasKey(x => x.Id);


    builder.Property(x => x.Id)
        .ValueGeneratedOnAdd();


    // Operation relation

    builder.Property(x => x.OperationId)
        .HasMaxLength(100)
        .IsRequired();


    // Attempt number

    builder.Property(x => x.AttemptNumber)
        .IsRequired();


    // Status enum

    builder.Property(x => x.Status)
        .HasConversion<string>()
        .HasMaxLength(20)
        .IsRequired();


    // Dates

    builder.Property(x => x.StartedAt)
        .IsRequired();


    builder.Property(x => x.FinishedAt);


    // Provider

    builder.Property(x => x.ProviderPaymentId)
        .HasMaxLength(100);


    // Error

    builder.Property(x => x.Error)
        .HasMaxLength(1000);



    // Indexes

    builder.HasIndex(x => new
    {
      x.OperationId,
      x.AttemptNumber
    })
    .IsUnique();


    builder.HasIndex(x => x.ProviderPaymentId);



    // связь с Operation

    builder.HasOne<Operation>()
        .WithMany()
        .HasForeignKey(x => x.OperationId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}
