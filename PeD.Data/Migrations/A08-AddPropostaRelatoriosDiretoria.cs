using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("A08-AddPropostaRelatoriosDiretoria")]
    public partial class AddPropostaRelatoriosDiretoria : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropostaRelatoriosDiretoria",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    PropostaId = table.Column<int>(nullable: false),
                    ParentId = table.Column<int>(nullable: false),
                    Conteudo = table.Column<string>(nullable: true),
                    Finalizado = table.Column<bool>(nullable: false),
                    FileId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostaRelatoriosDiretoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropostaRelatoriosDiretoria_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropostaRelatoriosDiretoria_Contratos_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RelatoriosDiretoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropostaRelatoriosDiretoria_Propostas_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Propostas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaRelatoriosDiretoria_Files_FileId",
                table: "PropostaRelatoriosDiretoria");

            migrationBuilder.DropForeignKey(
                name: "FK_PropostaRelatoriosDiretoria_Propostas_PropostaId",
                table: "PropostaRelatoriosDiretoria");

            migrationBuilder.DropForeignKey(
                name: "FK_PropostaRelatoriosDiretoria_Contratos_ParentId",
                table: "PropostaRelatoriosDiretoria");

            migrationBuilder.DropTable(
                name: "PropostaRelatoriosDiretoria"
                );
        }
    }
}