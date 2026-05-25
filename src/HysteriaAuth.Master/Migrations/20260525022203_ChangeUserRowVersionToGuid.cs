using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HysteriaAuth.Master.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserRowVersionToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RowVersion",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }
    }
}
