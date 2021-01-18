﻿// <auto-generated />

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PeD.Data.Migrations
{
    [DbContext(typeof(GestorDbContext))]
    [Migration("20190212134037_AddNewCategoriasUpload")]
    partial class AddNewCategoriasUpload
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("PeD.Models.AlocacaoRh", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("EmpresaId");

                    b.Property<int?>("EtapaId");

                    b.Property<int>("HrsMes1");

                    b.Property<int>("HrsMes2");

                    b.Property<int>("HrsMes3");

                    b.Property<int>("HrsMes4");

                    b.Property<int>("HrsMes5");

                    b.Property<int>("HrsMes6");

                    b.Property<string>("Justificativa");

                    b.Property<int?>("ProjetoId");

                    b.Property<int?>("RecursoHumanoId");

                    b.HasKey("Id");

                    b.HasIndex("EmpresaId");

                    b.HasIndex("EtapaId");

                    b.HasIndex("ProjetoId");

                    b.HasIndex("RecursoHumanoId");

                    b.ToTable("AlocacoesRh");
                });

            modelBuilder.Entity("PeD.Models.AlocacaoRm", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("EmpresaFinanciadoraId");

                    b.Property<int?>("EmpresaRecebedoraId");

                    b.Property<int?>("EtapaId");

                    b.Property<string>("Justificativa");

                    b.Property<int?>("ProjetoId");

                    b.Property<int>("Qtd");

                    b.Property<int?>("RecursoMaterialId");

                    b.HasKey("Id");

                    b.HasIndex("EmpresaFinanciadoraId");

                    b.HasIndex("EmpresaRecebedoraId");

                    b.HasIndex("EtapaId");

                    b.HasIndex("ProjetoId");

                    b.HasIndex("RecursoMaterialId");

                    b.ToTable("AlocacoesRm");
                });

            modelBuilder.Entity("PeD.Models.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("Cpf");

                    b.Property<int?>("EmpresaId");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<DateTime?>("DataAtualizacao");

                    b.Property<DateTime>("DataCadastro");

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<byte[]>("FotoPerfil");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NomeCompleto");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("RazaoSocial");

                    b.Property<string>("Role");

                    b.Property<string>("SecurityStamp");

                    b.Property<int>("Status");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<DateTime?>("UltimoLogin");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("EmpresaId");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("PeD.Models.Empresa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Nome");

                    b.Property<string>("Valor");

                    b.HasKey("Id");

                    b.ToTable("Empresas");
                });

            modelBuilder.Entity("PeD.Models.Estado", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Nome");

                    b.Property<string>("Valor");

                    b.HasKey("Id");

                    b.ToTable("CatalogEstados");
                });

            modelBuilder.Entity("PeD.Models.Segmento", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Nome");

                    b.Property<string>("Valor");

                    b.HasKey("Id");

                    b.ToTable("Segmentos");
                });

            modelBuilder.Entity("PeD.Models.CatalogStatus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Status");

                    b.HasKey("Id");

                    b.ToTable("CatalogStatus");
                });

            modelBuilder.Entity("PeD.Models.SubTema", b =>
                {
                    b.Property<int>("SubTemaId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CatalogTemaId");

                    b.Property<string>("Nome");

                    b.Property<string>("Valor");

                    b.HasKey("SubTemaId");

                    b.HasIndex("CatalogTemaId");

                    b.ToTable("CatalogSubTemas");
                });

            modelBuilder.Entity("PeD.Models.Tema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Nome");

                    b.Property<string>("Valor");

                    b.HasKey("Id");

                    b.ToTable("Tema");
                });

            modelBuilder.Entity("PeD.Models.CatalogUserPermissao", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Nome");

                    b.Property<string>("Valor");

                    b.HasKey("Id");

                    b.ToTable("CatalogUserPermissoes");
                });

            modelBuilder.Entity("PeD.Models.Empresa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("EmpresaId");

                    b.Property<int?>("CatalogEstadoId");

                    b.Property<int>("Classificacao");

                    b.Property<string>("Cnpj");

                    b.Property<int>("ProjetoId");

                    b.Property<string>("RazaoSocial");

                    b.HasKey("Id");

                    b.HasIndex("EmpresaId");

                    b.HasIndex("CatalogEstadoId");

                    b.HasIndex("ProjetoId");

                    b.ToTable("Empresas");
                });

            modelBuilder.Entity("PeD.Models.Etapa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("DataFim")
                        .HasColumnType("date");

                    b.Property<DateTime?>("DataInicio")
                        .HasColumnType("date");

                    b.Property<string>("Desc");

                    b.Property<int>("ProjetoId");

                    b.HasKey("Id");

                    b.HasIndex("ProjetoId");

                    b.ToTable("Etapas");
                });

            modelBuilder.Entity("PeD.Models.EtapaProduto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("EtapaId");

                    b.Property<int?>("ProdutoId");

                    b.HasKey("Id");

                    b.HasIndex("EtapaId");

                    b.HasIndex("ProdutoId");

                    b.ToTable("EtapaProdutos");
                });

            modelBuilder.Entity("PeD.Models.LogProjeto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Acao");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getdate()");

                    b.Property<int>("ProjetoId");

                    b.Property<string>("StatusAnterior");

                    b.Property<string>("StatusNovo");

                    b.Property<string>("Tela");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ProjetoId");

                    b.HasIndex("UserId");

                    b.ToTable("LogProjetos");
                });

            modelBuilder.Entity("PeD.Models.Produto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Classificacao");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getdate()");

                    b.Property<string>("Desc");

                    b.Property<int>("FaseCadeia");

                    b.Property<int>("ProjetoId");

                    b.Property<int>("Tipo");

                    b.Property<string>("Titulo");

                    b.HasKey("Id");

                    b.HasIndex("ProjetoId");

                    b.ToTable("Produtos");
                });

            modelBuilder.Entity("PeD.Models.Projeto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Aplicabilidade");

                    b.Property<bool?>("AvaliacaoInicial");

                    b.Property<int?>("EmpresaId");

                    b.Property<int?>("CatalogSegmentoId");

                    b.Property<int?>("CatalogStatusId");

                    b.Property<string>("Codigo");

                    b.Property<int?>("CompartResultados");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getdate()");

                    b.Property<DateTime?>("DataInicio");

                    b.Property<string>("Motivacao");

                    b.Property<string>("Numero");

                    b.Property<string>("Originalidade");

                    b.Property<string>("Pesquisas");

                    b.Property<string>("Razoabilidade");

                    b.Property<string>("Relevancia");

                    b.Property<int>("Tipo");

                    b.Property<string>("Titulo");

                    b.Property<string>("TituloDesc");

                    b.HasKey("Id");

                    b.HasIndex("EmpresaId");

                    b.HasIndex("CatalogSegmentoId");

                    b.HasIndex("CatalogStatusId");

                    b.ToTable("Projetos");
                });

            modelBuilder.Entity("PeD.Models.RecursoHumano", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Cpf");

                    b.Property<int>("EmpresaId");

                    b.Property<int>("Funcao");

                    b.Property<int>("Nacionalidade");

                    b.Property<string>("NomeCompleto");

                    b.Property<string>("Passaporte");

                    b.Property<int?>("ProjetoId");

                    b.Property<int>("Titulacao");

                    b.Property<string>("UrlCurriculo");

                    b.Property<decimal>("ValorHora")
                        .HasColumnType("decimal(18, 2)");

                    b.HasKey("Id");

                    b.HasIndex("EmpresaId");

                    b.HasIndex("ProjetoId");

                    b.ToTable("RecursoHumanos");
                });

            modelBuilder.Entity("PeD.Models.RecursoMaterial", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CategoriaContabil");

                    b.Property<string>("Especificacao");

                    b.Property<string>("Nome");

                    b.Property<int?>("ProjetoId");

                    b.Property<decimal>("ValorUnitario")
                        .HasColumnType("decimal(18, 2)");

                    b.HasKey("Id");

                    b.HasIndex("ProjetoId");

                    b.ToTable("RecursoMateriais");
                });

            modelBuilder.Entity("PeD.Models.RegistroFinanceiro", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AtividadeRealizada");

                    b.Property<string>("Beneficiado");

                    b.Property<int>("CategoriaContabil");

                    b.Property<string>("CnpjBeneficiado");

                    b.Property<DateTime?>("DataDocumento")
                        .HasColumnType("date");

                    b.Property<int?>("EmpresaFinanciadoraId");

                    b.Property<int?>("EmpresaRecebedoraId");

                    b.Property<bool>("EquiparLabExistente");

                    b.Property<bool>("EquiparLabNovo");

                    b.Property<string>("EspecificacaoTecnica");

                    b.Property<string>("FuncaoRecurso");

                    b.Property<bool>("ItemNacional");

                    b.Property<string>("Mes");

                    b.Property<string>("NomeItem");

                    b.Property<string>("NumeroDocumento");

                    b.Property<int?>("ProjetoId");

                    b.Property<string>("QtdHrs");

                    b.Property<int>("QtdItens");

                    b.Property<int?>("RecursoHumanoId");

                    b.Property<int?>("RecursoMaterialId");

                    b.Property<int>("Status");

                    b.Property<int>("Tipo");

                    b.Property<string>("TipoDocumento");

                    b.Property<decimal>("ValorUnitario")
                        .HasColumnType("decimal(18, 2)");

                    b.HasKey("Id");

                    b.HasIndex("EmpresaFinanciadoraId");

                    b.HasIndex("EmpresaRecebedoraId");

                    b.HasIndex("RecursoHumanoId");

                    b.HasIndex("RecursoMaterialId");

                    b.ToTable("RegistrosFinanceiros");
                });

            modelBuilder.Entity("PeD.Models.RegistroObs", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Created");

                    b.Property<int?>("RegistroFinanceiroId");

                    b.Property<string>("Texto");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("RegistroFinanceiroId");

                    b.HasIndex("UserId");

                    b.ToTable("RegistroObs");
                });

            modelBuilder.Entity("PeD.Models.Tema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CatalogTemaId");

                    b.Property<string>("OutroDesc");

                    b.Property<int>("ProjetoId");

                    b.HasKey("Id");

                    b.HasIndex("CatalogTemaId");

                    b.HasIndex("ProjetoId")
                        .IsUnique();

                    b.ToTable("Temas");
                });

            modelBuilder.Entity("PeD.Models.TemaSubTema", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CatalogSubTemaId");

                    b.Property<string>("OutroDesc");

                    b.Property<int?>("TemaId");

                    b.HasKey("Id");

                    b.HasIndex("CatalogSubTemaId");

                    b.HasIndex("TemaId");

                    b.ToTable("TemaSubTemas");
                });

            modelBuilder.Entity("PeD.Models.Upload", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("Categoria");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getdate()");

                    b.Property<string>("NomeArquivo");

                    b.Property<int?>("ProjetoId");

                    b.Property<int?>("RegistroFinanceiroId");

                    b.Property<int?>("TemaId");

                    b.Property<string>("Url");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("TemaId");

                    b.HasIndex("UserId");

                    b.ToTable("Uploads");
                });

            modelBuilder.Entity("PeD.Models.UserProjeto", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CatalogUserPermissaoId");

                    b.Property<int>("ProjetoId");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("CatalogUserPermissaoId");

                    b.HasIndex("ProjetoId");

                    b.HasIndex("UserId");

                    b.ToTable("UserProjetos");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("RoleId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("PeD.Models.AlocacaoRh", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "Empresa")
                        .WithMany()
                        .HasForeignKey("EmpresaId");

                    b.HasOne("PeD.Models.Etapa", "Etapa")
                        .WithMany()
                        .HasForeignKey("EtapaId");

                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("AlocacoesRh")
                        .HasForeignKey("ProjetoId");

                    b.HasOne("PeD.Models.RecursoHumano", "RecursoHumano")
                        .WithMany()
                        .HasForeignKey("RecursoHumanoId");
                });

            modelBuilder.Entity("PeD.Models.AlocacaoRm", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "EmpresaFinanciadora")
                        .WithMany()
                        .HasForeignKey("EmpresaFinanciadoraId");

                    b.HasOne("PeD.Models.Empresa", "EmpresaRecebedora")
                        .WithMany()
                        .HasForeignKey("EmpresaRecebedoraId");

                    b.HasOne("PeD.Models.Etapa", "Etapa")
                        .WithMany()
                        .HasForeignKey("EtapaId");

                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("AlocacoesRm")
                        .HasForeignKey("ProjetoId");

                    b.HasOne("PeD.Models.RecursoMaterial", "RecursoMaterial")
                        .WithMany()
                        .HasForeignKey("RecursoMaterialId");
                });

            modelBuilder.Entity("PeD.Models.ApplicationUser", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "Empresa")
                        .WithMany()
                        .HasForeignKey("EmpresaId");
                });

            modelBuilder.Entity("PeD.Models.SubTema", b =>
                {
                    b.HasOne("PeD.Models.Tema")
                        .WithMany("SubTemas")
                        .HasForeignKey("CatalogTemaId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PeD.Models.Empresa", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "Empresa")
                        .WithMany()
                        .HasForeignKey("EmpresaId");

                    b.HasOne("PeD.Models.Estado", "Estado")
                        .WithMany()
                        .HasForeignKey("CatalogEstadoId");

                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("Empresas")
                        .HasForeignKey("ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PeD.Models.Etapa", b =>
                {
                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("Etapas")
                        .HasForeignKey("ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PeD.Models.EtapaProduto", b =>
                {
                    b.HasOne("PeD.Models.Etapa")
                        .WithMany("EtapaProdutos")
                        .HasForeignKey("EtapaId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.Produto")
                        .WithMany("EtapaProduto")
                        .HasForeignKey("ProdutoId");
                });

            modelBuilder.Entity("PeD.Models.LogProjeto", b =>
                {
                    b.HasOne("PeD.Models.Projeto", "Projeto")
                        .WithMany()
                        .HasForeignKey("ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.ApplicationUser", "ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PeD.Models.Produto", b =>
                {
                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("Produtos")
                        .HasForeignKey("ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PeD.Models.Projeto", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "Empresa")
                        .WithMany()
                        .HasForeignKey("EmpresaId");

                    b.HasOne("PeD.Models.Segmento", "Segmento")
                        .WithMany()
                        .HasForeignKey("CatalogSegmentoId");

                    b.HasOne("PeD.Models.CatalogStatus", "CatalogStatus")
                        .WithMany()
                        .HasForeignKey("CatalogStatusId");
                });

            modelBuilder.Entity("PeD.Models.RecursoHumano", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "Empresa")
                        .WithMany()
                        .HasForeignKey("EmpresaId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("RecursosHumanos")
                        .HasForeignKey("ProjetoId");
                });

            modelBuilder.Entity("PeD.Models.RecursoMaterial", b =>
                {
                    b.HasOne("PeD.Models.Projeto")
                        .WithMany("RecursosMateriais")
                        .HasForeignKey("ProjetoId");
                });

            modelBuilder.Entity("PeD.Models.RegistroFinanceiro", b =>
                {
                    b.HasOne("PeD.Models.Empresa", "EmpresaFinanciadora")
                        .WithMany()
                        .HasForeignKey("EmpresaFinanciadoraId");

                    b.HasOne("PeD.Models.Empresa", "EmpresaRecebedora")
                        .WithMany()
                        .HasForeignKey("EmpresaRecebedoraId");

                    b.HasOne("PeD.Models.RecursoHumano", "RecursoHumano")
                        .WithMany()
                        .HasForeignKey("RecursoHumanoId");

                    b.HasOne("PeD.Models.RecursoMaterial", "RecursoMaterial")
                        .WithMany()
                        .HasForeignKey("RecursoMaterialId");
                });

            modelBuilder.Entity("PeD.Models.RegistroObs", b =>
                {
                    b.HasOne("PeD.Models.RegistroFinanceiro")
                        .WithMany("ObsInternas")
                        .HasForeignKey("RegistroFinanceiroId");

                    b.HasOne("PeD.Models.ApplicationUser", "ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PeD.Models.Tema", b =>
                {
                    b.HasOne("PeD.Models.Tema", "Tema")
                        .WithMany()
                        .HasForeignKey("CatalogTemaId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.Projeto")
                        .WithOne("Tema")
                        .HasForeignKey("PeD.Models.Tema", "ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PeD.Models.TemaSubTema", b =>
                {
                    b.HasOne("PeD.Models.SubTema", "SubTema")
                        .WithMany()
                        .HasForeignKey("CatalogSubTemaId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.Tema")
                        .WithMany("SubTemas")
                        .HasForeignKey("TemaId");
                });

            modelBuilder.Entity("PeD.Models.Upload", b =>
                {
                    b.HasOne("PeD.Models.Tema")
                        .WithMany("Uploads")
                        .HasForeignKey("TemaId");

                    b.HasOne("PeD.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("PeD.Models.UserProjeto", b =>
                {
                    b.HasOne("PeD.Models.CatalogUserPermissao", "CatalogUserPermissao")
                        .WithMany()
                        .HasForeignKey("CatalogUserPermissaoId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.Projeto", "Projeto")
                        .WithMany("UsersProjeto")
                        .HasForeignKey("ProjetoId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.ApplicationUser", "ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("PeD.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("PeD.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PeD.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("PeD.Models.ApplicationUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
