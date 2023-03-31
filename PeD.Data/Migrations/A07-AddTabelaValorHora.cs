using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("A07-AddTabelaValorHora")]
    public partial class AddTabelaValorHora : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TabelaValorHora",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                    Nome = table.Column<string>(nullable: false),
                    Registros = table.Column<string>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabelaValorHora", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "TabelaValorHoraId",
                table: "Demandas",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_TabelaValorHoraId",
                table: "Demandas",
                column: "TabelaValorHoraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Demandas_TabelaValorHora_TabelaValorHoraId",
                table: "Demandas",
                column: "TabelaValorHoraId",
                principalTable: "TabelaValorHora",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Demandas_TabelaValorHora_TabelaValorHoraId",
                table: "Demandas");

            migrationBuilder.DropIndex(
                name: "IX_Demandas_TabelaValorHoraId",
                table: "Demandas");

            migrationBuilder.DropColumn(
                name: "TabelaValorHoraId",
                table: "Demandas");
        }
    }
}