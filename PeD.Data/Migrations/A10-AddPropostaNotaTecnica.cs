using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("A10-AddPropostaNotaTecnica")]
    public partial class AddPropostaNotaTecnica : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropostaNotaTecnica",
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
                    table.PrimaryKey("PK_PropostaNotaTecnica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropostaNotaTecnica_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropostaNotaTecnica_NotaTecnica_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RelatoriosDiretoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropostaNotaTecnica_Propostas_PropostaId",
                        column: x => x.PropostaId,
                        principalTable: "Propostas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaNotaTecnica_Files_FileId",
                table: "PropostaNotaTecnica");

            migrationBuilder.DropForeignKey(
                name: "FK_PropostaNotaTecnica_Propostas_PropostaId",
                table: "PropostaNotaTecnica");

            migrationBuilder.DropForeignKey(
                name: "FK_PropostaNotaTecnica_NotaTecnica_ParentId",
                table: "PropostaNotaTecnica");

            migrationBuilder.DropTable(
                name: "PropostaNotaTecnica"
                );
        }
    }
}