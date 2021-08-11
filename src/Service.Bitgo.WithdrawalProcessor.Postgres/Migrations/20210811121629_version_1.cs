using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Service.Bitgo.WithdrawalProcessor.Postgres.Migrations
{
    public partial class version_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "withdrawals");

            migrationBuilder.CreateTable(
                name: "withdrawals",
                schema: "withdrawals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrokerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WalletId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    AssetSymbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Comment = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Integration = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Txid = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MatchingEngineId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RetriesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EventDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ToAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_withdrawals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_withdrawals_Status",
                schema: "withdrawals",
                table: "withdrawals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawals_TransactionId",
                schema: "withdrawals",
                table: "withdrawals",
                column: "TransactionId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "withdrawals",
                schema: "withdrawals");
        }
    }
}
