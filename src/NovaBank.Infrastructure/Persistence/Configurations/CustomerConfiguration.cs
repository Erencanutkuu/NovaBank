using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaBank.Domain.Customers;

namespace NovaBank.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public const string EmailIndexName = "ix_customers_email";

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever(); // Guid'i Domain üretir.
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName(EmailIndexName);
    }
}
