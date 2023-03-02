using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("AddAnalistasResponsaveisDemanda")]
    public partial class AddAnalistasResponsaveisDemanda : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalistaTecnicoId",
                table: "Demandas",
                nullable: true, 
                maxLength: 450);
            migrationBuilder.AddForeignKey(
                name: "FK_Demandas_AspNetUsers_AnalistaTecnicoId",
                table: "Demandas",
                column: "AnalistaTecnicoId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<string>(
                name: "AnalistaPedId",
                table: "Demandas",
                nullable: true, 
                maxLength: 450);
            migrationBuilder.AddForeignKey(
                name: "FK_Demandas_AspNetUsers_AnalistaPedId",
                table: "Demandas",
                column: "AnalistaPedId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropForeignKey(
                name: "FK_Demandas_AspNetUsers_AnalistaTecnicoId",
                table: "Demandas");                
            migrationBuilder.DropForeignKey(
                name: "FK_Demandas_AspNetUsers_AnalistaTecnicoId",
                table: "Demandas");
            migrationBuilder.DropColumn(
                name: "AnalistaTecnicoId",
                table: "Demandas");

            migrationBuilder.DropIndex(
                name: "IX_Demandas_AnalistaPedId",
                table: "Demandas");
            migrationBuilder.DropIndex(
                name: "IX_Demandas_AnalistaPedId",
                table: "Demandas");
            migrationBuilder.DropColumn(
                name: "AnalistaPedId",
                table: "Demandas");
           
        }
    }
}