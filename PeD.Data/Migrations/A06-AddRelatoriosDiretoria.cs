using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("A06-AddRelatoriosDiretoria")]
    public partial class AddRelatoriosDiretoria : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelatoriosDiretoria",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(nullable: true),
                    Header = table.Column<string>(nullable: true),
                    Conteudo = table.Column<string>(nullable: true),
                    Footer = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatorioDiretoria", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "RelatorioDiretoriaId",
                table: "Captacoes",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Captacoes_RelatoriosDiretoria_RelatorioDiretoriaId",
                table: "Captacoes",
                column: "RelatorioDiretoriaId",
                principalTable: "RelatoriosDiretoria",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Captacoes_RelatoriosDiretoria_RelatorioDiretoriaId",
                table: "Captacoes");

            migrationBuilder.DropColumn(
                name: "RelatorioDiretoriaId",
                table: "Captacoes");

            migrationBuilder.DropTable(
                name: "RelatoriosDiretoria"
                );
        }
    }
}