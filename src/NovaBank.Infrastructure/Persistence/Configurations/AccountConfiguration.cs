using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaBank.Domain.Accounts;
using NovaBank.Domain.Customers;

namespace NovaBank.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Iban).HasColumnName("iban").HasMaxLength(Iban.Length)
            .HasConversion(iban => iban.Value, value => Iban.Parse(value));
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).HasConversion<string>();
        builder.Property(x => x.Balance).HasColumnName("balance").HasPrecision(18, 2);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>();
        builder.Property(x => x.OpenedAt).HasColumnName("opened_at");
        builder.Property(x => x.ClosedAt).HasColumnName("closed_at");

        // PostgreSQL'in her güncellemede değişen gizli sütunu; eşzamanlı değişikliği yakalar.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasIndex(x => x.Iban).IsUnique().HasDatabaseName("ix_accounts_iban");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("ix_accounts_customer_id");

        // Hesabı olan müşteri silinemez.
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
