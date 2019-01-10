﻿// <auto-generated />
using System;
using APIGestor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace APIGestor.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    partial class GestorDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("APIGestor.Models.Empresa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("NomeFantasia");

                    b.Property<string>("RazaoSocial");

                    b.Property<string>("Uf");

                    b.HasKey("Id");

                    b.ToTable("Empresas");
                });

            modelBuilder.Entity("APIGestor.Models.Produto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Created");

                    b.Property<string>("Desc");

                    b.Property<int>("ProjetoId");

                    b.Property<string>("Titulo");

                    b.HasKey("Id");

                    b.HasIndex("ProjetoId");

                    b.ToTable("Produto");
                });

            modelBuilder.Entity("APIGestor.Models.Projeto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Aplicabilidade");

                    b.Property<string>("AvaliacaoInicial");

                    b.Property<string>("CompartResultados");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getdate()");

                    b.Property<int>("EmpresaProponente");

                    b.Property<string>("Motivacao");

                    b.Property<string>("Numero");

                    b.Property<string>("Originalidade");

                    b.Property<string>("Pesquisas");

                    b.Property<string>("Razoabilidade");

                    b.Property<string>("Relevancia");

                    b.Property<int>("SegmentoId");

                    b.Property<string>("Status");

                    b.Property<string>("Titulo");

                    b.Property<string>("TituloDesc");

                    b.HasKey("Id");

                    b.ToTable("Projetos");
                });

            modelBuilder.Entity("APIGestor.Models.Produto", b =>
                {
                    b.HasOne("APIGestor.Models.Projeto", "Projeto")
                        .WithMany("Produtos")
                        .HasForeignKey("ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
