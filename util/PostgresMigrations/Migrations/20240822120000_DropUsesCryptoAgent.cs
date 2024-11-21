using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Bit.PostgresMigrations.Migrations
{
    /// <inheritdoc />
    public partial class DropUsesCryptoAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsesCryptoAgent",
                table: "User");
			
            migrationBuilder.DropIndex(
                name: "IX_HubConnection_Token",
                table: "HubConnection");

            migrationBuilder.AlterColumn<bool>(
                name: "ForcePasswordReset",
                table: "OrganizationUser",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "UseCustomPermissions",
                table: "Organization",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "HubConnection",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            //migrationBuilder.AlterColumn<long>(
            //    name: "Id",
            //    table: "HubConnection",
            //    type: "bigint",
            //   nullable: false,
            //    oldClrType: typeof(long),
            //    oldType: "bigint")
            //    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionARN",
                table: "AmazonSNSDevice",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EndpointARN",
                table: "AmazonSNSDevice",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            //migrationBuilder.AlterColumn<long>(
            //    name: "Id",
            //    table: "AmazonSNSDevice",
            //    type: "bigint",
            //    nullable: false,
            //    oldClrType: typeof(long),
            //    oldType: "bigint")
            //    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_AmazonSNSDevice_DeviceId",
                table: "AmazonSNSDevice",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsesCryptoAgent",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);
			
            migrationBuilder.DropIndex(
                name: "IX_AmazonSNSDevice_DeviceId",
                table: "AmazonSNSDevice");

            migrationBuilder.AlterColumn<bool>(
                name: "ForcePasswordReset",
                table: "OrganizationUser",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "UseCustomPermissions",
                table: "Organization",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "HubConnection",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            //migrationBuilder.AlterColumn<long>(
            //    name: "Id",
            //    table: "HubConnection",
            //    type: "bigint",
            //    nullable: false,
            //    oldClrType: typeof(long),
            //    oldType: "bigint")
            //    .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionARN",
                table: "AmazonSNSDevice",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EndpointARN",
                table: "AmazonSNSDevice",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            //migrationBuilder.AlterColumn<long>(
            //    name: "Id",
            //    table: "AmazonSNSDevice",
            //    type: "bigint",
            //    nullable: false,
            //    oldClrType: typeof(long),
            //    oldType: "bigint")
             //   .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_HubConnection_Token",
                table: "HubConnection",
                column: "Token");
        }
    }
}
