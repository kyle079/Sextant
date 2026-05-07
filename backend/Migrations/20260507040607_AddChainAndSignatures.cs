using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sextant.Migrations
{
    /// <inheritdoc />
    public partial class AddChainAndSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChainSystems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolarSystemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Class = table.Column<string>(type: "TEXT", nullable: false),
                    ClassNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Effect = table.Column<string>(type: "TEXT", nullable: true),
                    Statics = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    IsHome = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChainConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceSystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetSystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    WormholeTypeCode = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    MassStatus = table.Column<string>(type: "TEXT", nullable: false),
                    MaxMassKg = table.Column<long>(type: "INTEGER", nullable: true),
                    MaxJumpMassKg = table.Column<long>(type: "INTEGER", nullable: true),
                    LifetimeHours = table.Column<int>(type: "INTEGER", nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EolAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SignatureId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChainConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChainConnections_ChainSystems_SourceSystemId",
                        column: x => x.SourceSystemId,
                        principalTable: "ChainSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChainConnections_ChainSystems_TargetSystemId",
                        column: x => x.TargetSystemId,
                        principalTable: "ChainSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Signatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChainSystemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SignatureId = table.Column<string>(type: "TEXT", nullable: false),
                    Group = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ScanPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    ScannedByCharacterId = table.Column<long>(type: "INTEGER", nullable: false),
                    ScannedByCharacterName = table.Column<string>(type: "TEXT", nullable: false),
                    EolAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signatures_ChainSystems_ChainSystemId",
                        column: x => x.ChainSystemId,
                        principalTable: "ChainSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChainConnections_SourceSystemId_TargetSystemId",
                table: "ChainConnections",
                columns: new[] { "SourceSystemId", "TargetSystemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChainConnections_TargetSystemId",
                table: "ChainConnections",
                column: "TargetSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChainSystems_SolarSystemId",
                table: "ChainSystems",
                column: "SolarSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Signatures_ChainSystemId_SignatureId",
                table: "Signatures",
                columns: new[] { "ChainSystemId", "SignatureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChainConnections");

            migrationBuilder.DropTable(
                name: "Signatures");

            migrationBuilder.DropTable(
                name: "ChainSystems");
        }
    }
}
