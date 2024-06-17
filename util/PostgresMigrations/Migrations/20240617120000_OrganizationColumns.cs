using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.PostgresMigrations.Migrations;

/// <inheritdoc />
public partial class OrganizationColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AmazonSNSDevice",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false),
                DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                EndpointARN = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                SubscriptionARN = table.Column<string>(type: "character varying(32)", maxLength: 2048, nullable: true),
                CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AmazonSNSDevice", x => x.Id);
                table.ForeignKey(
                    name: "FK_AmazonSNSDevice_Device",
                    column: x => x.DeviceId,
                    principalTable: "Device",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HubConnection",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: true),
                ConnectionId = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Token = table.Column<Guid>(type: "uuid", nullable: false),
                MessageType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                MessagePayload = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevisionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HubConnection", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HubConnection_Token",
            table: "HubConnection",
            column: "Token");

        migrationBuilder.AlterColumn<bool>(
            name: "UseCustomPermissions",
            table: "Organization",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "Skip2faForSso",
            table: "Organization",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AlterColumn<string>(
            name: "ApiKey",
            table: "OrganizationApiKey",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30,
            oldNullable: false);

        migrationBuilder.AddColumn<bool>(
            name: "ForcePasswordReset",
            table: "OrganizationUser",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AlterColumn<string>(
            name: "MasterPasswordHint",
            table: "User",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50,
            oldNullable: false);

        migrationBuilder.AlterColumn<string>(
            name: "TwoFactorRecoveryCode",
            table: "User",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32,
            oldNullable: false);

        migrationBuilder.AlterColumn<string>(
            name: "ApiKey",
            table: "User",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30,
            oldNullable: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AmazonSNSDevice");

        migrationBuilder.DropTable(
            name: "HubConnection");

        migrationBuilder.AlterColumn<bool>(
            name: "UseCustomPermissions",
            table: "Organization",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.DropColumn(
            name: "Skip2faForSso",
            table: "Organization");

        migrationBuilder.AlterColumn<string>(
            name: "ApiKey",
            table: "OrganizationApiKey",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: false);

        migrationBuilder.DropColumn(
            name: "ForcePasswordReset",
            table: "OrganizationUser");

        migrationBuilder.AlterColumn<string>(
            name: "MasterPasswordHint",
            table: "User",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: false);

        migrationBuilder.AlterColumn<string>(
            name: "TwoFactorRecoveryCode",
            table: "User",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: false);

        migrationBuilder.AlterColumn<string>(
            name: "ApiKey",
            table: "User",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: false);
    }
}
