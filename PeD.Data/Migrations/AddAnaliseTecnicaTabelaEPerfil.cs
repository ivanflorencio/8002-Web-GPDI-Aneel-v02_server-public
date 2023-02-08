using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("AddAnaliseTecnicaTabelaEPerfil")]
    public partial class AddAnaliseTecnicaTabelaEPerfil : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AVALIACAO TECNICA DA PROPOSTA
            migrationBuilder.CreateTable(
                name: "PropostaAnaliseTecnica",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                    PropostaId = table.Column<int>(nullable: true),
                    ResponsavelId = table.Column<string>(nullable: true, maxLength: 450),
                    PontuacaoFinal = table.Column<double>(nullable: true),
                    DataHora = table.Column<DateTime>(nullable: false, defaultValue: "CURRENT_TIMESTAMP"),
                    Justificativa = table.Column<string>(nullable: true),
                    Comentarios = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostaAnaliseTecnica", x => x.Id);                    
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaAnaliseTecnica_Proposta_PropostaId",
                table: "PropostaAnaliseTecnica",
                column: "PropostaId",
                principalTable: "Propostas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaAnaliseTecnica_AspNetUsers_ResponsavelId",
                table: "PropostaAnaliseTecnica",
                column: "ResponsavelId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // CRITERIOS DE AVALIACAO TECNICA DA PROPOSTA
            migrationBuilder.CreateTable(
                name: "PropostaCriterioAvaliacao",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Guid = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                    DemandaId = table.Column<int>(nullable: true),
                    ResponsavelId = table.Column<string>(nullable: true, maxLength: 450),
                    DataHora = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Descricao = table.Column<string>(nullable: false),
                    Peso = table.Column<int>(nullable: false),
                    DoGestor = table.Column<Boolean>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostaCriterioAvaliacao", x => x.Id);                    
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaCriterioAvaliacao_Demanda_DemandaId",
                table: "PropostaCriterioAvaliacao",
                column: "DemandaId",
                principalTable: "Demandas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaCriterioAvaliacao_AspNetUsers_ResponsavelId",
                table: "PropostaCriterioAvaliacao",
                column: "ResponsavelId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // PARECERES TECNICOS DA PROPOSTA
            migrationBuilder.CreateTable(
                name: "PropostaParecerTecnico",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CriterioId = table.Column<int>(nullable: true),
                    AnaliseTecnicaId = table.Column<int>(nullable: true),
                    ResponsavelId = table.Column<string>(nullable: true, maxLength: 450),
                    DataHora = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Justificativa = table.Column<string>(nullable: false),
                    Pontuacao = table.Column<int>(nullable: false),                    
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostaParecerTecnico", x => x.Id);                    
                });

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaParecerTecnico_Criterio_CriterioId",
                table: "PropostaParecerTecnico",
                column: "CriterioId",
                principalTable: "PropostaCriterioAvaliacao",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaParecerTecnico_AnaliseTecnica_AnaliseTecnicaId",
                table: "PropostaParecerTecnico",
                column: "AnaliseTecnicaId",
                principalTable: "PropostaAnaliseTecnica",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PropostaParecerTecnico_AspNetUsers_ResponsavelId",
                table: "PropostaParecerTecnico",
                column: "ResponsavelId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            
            // NOVO PERFIL ANALISTA TECNICO
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id","Name", "NormalizedName" },
                values: new object[,]
                {
                    { "8f4e3f04-a65d-11ed-afa1-0242ac120002", "AnalistaTecnico", "ANALISTA_TECNICO" },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            // AVALIACAO TECNICA DA PROPOSTA
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaAnaliseTecnica_Proposta_PropostaId",
                table: "PropostaAnaliseTecnica");                
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaAnaliseTecnica_AspNetUsers_ResponsavelId",
                table: "PropostaAnaliseTecnica");
            migrationBuilder.DropTable(
                name: "PropostaAnaliseTecnica");
           
            // CRITERIOS DE AVALIACAO TECNICA DA PROPOSTA
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaCriterioAvaliacao_Demanda_DemandaId",
                table: "PropostaCriterioAvaliacao");                
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaCriterioAvaliacao_AspNetUsers_ResponsavelId",
                table: "PropostaCriterioAvaliacao");
            migrationBuilder.DropTable(
                name: "PropostaCriterioAvaliacao");

            // PARECERES TECNICOS DA PROPOSTA
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaParecerTecnico_Criterio_CriterioId",
                table: "PropostaParecerTecnico");                
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaParecerTecnico_AnaliseTecnica_AnaliseTecnicaId",
                table: "PropostaParecerTecnico");
            migrationBuilder.DropForeignKey(
                name: "FK_PropostaParecerTecnico_AspNetUsers_ResponsavelId",
                table: "PropostaParecerTecnico");
            migrationBuilder.DropTable(
                name: "PropostaParecerTecnico");     

            // NOVO PERFIL ANALISTA TECNICO
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "8f4e3f04-a65d-11ed-afa1-0242ac120002"
            );
        }
    }
}