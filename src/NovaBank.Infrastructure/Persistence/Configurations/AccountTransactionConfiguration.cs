using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaBank.Domain.Accounts;

namespace NovaBank.Infrastructure.Persistence.Configurations;

public sealed class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public const string SequenceProperty = "Sequence";

    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("account_transactions", table =>
        {
            table.HasCheckConstraint("ck_account_transactions_amount", "amount > 0");
            table.HasCheckConstraint("ck_account_transactions_balance_after", "balance_after >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(20).HasConversion<string>();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasColumnName("balance_after").HasPrecision(18, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        // Ekleme sırası; sıralama sunucu saatine bağlı kalmaz.
        builder.Property<long>(SequenceProperty).HasColumnName("sequence").UseIdentityAlwaysColumn();

        builder.HasIndex(nameof(AccountTransaction.AccountId), SequenceProperty)
            .IsUnique().HasDatabaseName("ix_account_transactions_account_id_sequence");

        // Hareketi olan hesap silinemez.
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
