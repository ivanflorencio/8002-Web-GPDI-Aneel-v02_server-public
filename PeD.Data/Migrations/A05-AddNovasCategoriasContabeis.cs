using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("A05-AddNovasCategoriasContabeis")]
    public partial class AddNovasCategoriasContabeis : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
               table: "CategoriasContabeis",
               columns: new[] { "Id", "Nome", "Valor" },
               values: new object[,]
               {
                    { 8, "Materiais Permanentes e Equipamentos", "MP" },
                    { 9, "Startups", "SU" }
               });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [CategoriasContabeis] WHERE Id IN [8,9]", true);
        }
    }
}