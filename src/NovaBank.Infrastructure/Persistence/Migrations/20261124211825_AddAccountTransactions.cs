using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NovaBank.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_transactions", x => x.id);
                    table.CheckConstraint("ck_account_transactions_amount", "amount > 0");
                    table.CheckConstraint("ck_account_transactions_balance_after", "balance_after >= 0");
                    table.ForeignKey(
                        name: "FK_account_transactions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_accounts_balance",
                table: "accounts",
                sql: "balance >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_account_transactions_account_id_sequence",
                table: "account_transactions",
                columns: new[] { "account_id", "sequence" },
                unique: true);

            // Hareket kaydı eklenir ama değiştirilemez ve silinemez; uygulama dışından gelen SQL de engellenir.
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_account_transaction_change() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'account_transactions rows cannot be changed';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER tr_account_transactions_immutable
                BEFORE UPDATE OR DELETE OR TRUNCATE ON account_transactions
                FOR EACH STATEMENT EXECUTE FUNCTION reject_account_transaction_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_transactions");

            migrationBuilder.Sql("DROP FUNCTION reject_account_transaction_change();");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accounts_balance",
                table: "accounts");
        }
    }
}
