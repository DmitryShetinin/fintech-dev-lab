using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MasterDataService.Infrastructure.Configurations
{
    public class PlantConfiguration : IEntityTypeConfiguration<Plant>
    {
        public void Configure(EntityTypeBuilder<Plant> builder)
        {
            // 1. Таблица
            builder.ToTable("Plants");

            // 2. Первичный ключ
            builder.HasKey(p => p.Id);

            // 3. Свойства
            builder.Property(p => p.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Name)
                .HasColumnName("Name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Code)
                .HasColumnName("Code")
                .IsRequired()
                .HasMaxLength(50);

            // 4. Связь с Equipments (Один ко многим)
            builder.HasMany(p => p.Equipments)
                .WithOne(e => e.Plant)
                .HasForeignKey(e => e.PlantId)
                .OnDelete(DeleteBehavior.Restrict); // Чтобы при удалении Plant не удалялись Equipment

            // 5. Уникальный индекс для Code
            builder.HasIndex(p => p.Code)
                .IsUnique()
                .HasDatabaseName("IX_Plants_Code_Unique");

            // 6. Индекс для поиска по Name (опционально)
            builder.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Plants_Name");
        }
    }
}