using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.Bitgo.WithdrawalProcessor.Postgres.Migrations
{
    public partial class ClientLangAndIp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientIp",
                schema: "withdrawals",
                table: "withdrawals",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientLang",
                schema: "withdrawals",
                table: "withdrawals",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotificationTime",
                schema: "withdrawals",
                table: "withdrawals",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientIp",
                schema: "withdrawals",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "ClientLang",
                schema: "withdrawals",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "NotificationTime",
                schema: "withdrawals",
                table: "withdrawals");
        }
    }
}
