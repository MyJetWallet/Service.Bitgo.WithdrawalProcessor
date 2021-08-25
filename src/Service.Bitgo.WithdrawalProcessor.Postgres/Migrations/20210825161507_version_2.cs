using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.Bitgo.WithdrawalProcessor.Postgres.Migrations
{
    public partial class version_2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowState",
                schema: "withdrawals",
                table: "withdrawals",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkflowState",
                schema: "withdrawals",
                table: "withdrawals");
        }
    }
}
