using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("A04-AddMesDesembolso")]
    public partial class AddMesDesembolso : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Int16>(
                name: "MesDesembolso",
                table: "PropostaRecursosMateriaisAlocacao",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MesDesembolso",
                table: "PropostaRecursosMateriaisAlocacao");
        }
    }
}