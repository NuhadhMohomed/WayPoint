using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WayPoint.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "PaymentAttempts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "LKR");

            migrationBuilder.AddColumn<Guid>(
                name: "HoldId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PassengerId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_GatewayTransactionId",
                table: "PaymentAttempts",
                column: "GatewayTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_HoldId",
                table: "PaymentAttempts",
                column: "HoldId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PassengerId",
                table: "PaymentAttempts",
                column: "PassengerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_PassengerProfiles_PassengerId",
                table: "PaymentAttempts",
                column: "PassengerId",
                principalTable: "PassengerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentAttempts_SeatHolds_HoldId",
                table: "PaymentAttempts",
                column: "HoldId",
                principalTable: "SeatHolds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_PassengerProfiles_PassengerId",
                table: "PaymentAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentAttempts_SeatHolds_HoldId",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_GatewayTransactionId",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_HoldId",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_PassengerId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "HoldId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "PassengerId",
                table: "PaymentAttempts");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "PaymentAttempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
