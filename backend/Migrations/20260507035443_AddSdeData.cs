using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sextant.Migrations
{
    /// <inheritdoc />
    public partial class AddSdeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SdeRefreshRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WormholeSystemCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WormholeTypeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdeRefreshRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WormholeSystems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolarSystemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryName = table.Column<string>(type: "TEXT", nullable: true),
                    Class = table.Column<string>(type: "TEXT", nullable: false),
                    ClassNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Effect = table.Column<string>(type: "TEXT", nullable: true),
                    Statics = table.Column<string>(type: "TEXT", nullable: false),
                    IsShattered = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WormholeSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WormholeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    EveTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    DestinationType = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationClass = table.Column<int>(type: "INTEGER", nullable: true),
                    LifetimeHours = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMassKg = table.Column<long>(type: "INTEGER", nullable: true),
                    MaxJumpMassKg = table.Column<long>(type: "INTEGER", nullable: true),
                    ScanStrength = table.Column<decimal>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WormholeTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WormholeSystems_Name",
                table: "WormholeSystems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WormholeSystems_SolarSystemId",
                table: "WormholeSystems",
                column: "SolarSystemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WormholeTypes_Code",
                table: "WormholeTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SdeRefreshRuns");

            migrationBuilder.DropTable(
                name: "WormholeSystems");

            migrationBuilder.DropTable(
                name: "WormholeTypes");
        }
    }
}
