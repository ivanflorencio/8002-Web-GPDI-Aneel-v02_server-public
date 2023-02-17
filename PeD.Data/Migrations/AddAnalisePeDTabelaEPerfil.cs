using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("AddAnalisePedTabelaEPerfil")]
    public partial class AddAnalisePedTabelaEPerfil : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AVALIACAO PED DA PROPOSTA
            migrationBuilder.CreateTable(
                name: "PropostaAnalisePed",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                    PropostaId = table.Column<int>(nullable: true),
                    ResponsavelId = table.Column<string>(nullable: true, maxLength: 450),
                    DataHora = table.Column<DateTime>(nullable: false, defaultValue: "CURRENT_TIMESTAMP"),
                    Originalidade = table.Column<string>(nullable: true),
                    Aplicabilidade = table.Column<string>(nullable: true),
                    Relevancia = table.Column<string>(nullable: true),
                    RazoabilidadeCustos = table.Column<string>(nullable: true),                    
                    PontuacaoOriginalidade = table.Column<int>(nullable: true),
                    PontuacaoAplicabilidade = table.Column<int>(nullable: true),
                    PontuacaoRelevancia = table.Column<int>(nullable: true),
                    PontuacaoRazoabilidadeCustos = table.Column<int>(nullable: true),
                    PontosCriticos = table.Column<string>(nullable: true),
                    Comentarios = table.Column<string>(nullable: true),
                    Conceito = table.Column<string>(nullable: false),
                    PontuacaoFinal = table.Column<double>(nullable: true),
                    Status = table.Column<string>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostaAnalisePed", x => x.Id);                    
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaAnalisePed_Proposta_PropostaId",
                table: "PropostaAnalisePed",
                column: "PropostaId",
                principalTable: "Propostas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaAnalisePed_AspNetUsers_ResponsavelId",
                table: "PropostaAnalisePed",
                column: "ResponsavelId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // NOVO PERFIL ANALISTA PED
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id","Name", "NormalizedName" },
                values: new object[,]
                {
                    { "aad356ee-aec1-11ed-afa1-0242ac120002", "AnalistaPed", "ANALISTA_PED" },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            // AVALIACAO PED DA PROPOSTA
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaAnalisePed_Proposta_PropostaId",
                table: "PropostaAnalisePed");                
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaAnalisePed_AspNetUsers_ResponsavelId",
                table: "PropostaAnalisePed");
            migrationBuilder.DropTable(
                name: "PropostaAnalisePed");
           
            // NOVO PERFIL ANALISTA PED
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "aad356ee-aec1-11ed-afa1-0242ac120002"
            );
        }
    }
}