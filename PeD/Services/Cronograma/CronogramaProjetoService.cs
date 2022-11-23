using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PeD.Core.ApiModels.Cronograma;
using PeD.Core.Models.Projetos;
using TaesaCore.Interfaces;
using TaesaCore.Services;
using PeD.Data;
using PeD.Services.Projetos;
using PeD.Core.ApiModels.Projetos;

namespace PeD.Services.Cronograma
{
    public class CronogramaProjetoService : BaseService<Projeto>
    {
        private int duracaoProjeto = 0;
        private List<Orcamento> OrcamentosProjeto = new List<Orcamento>();
        private List<RegistroFinanceiroInfo> RegistrosFinanceirosProjeto = new List<RegistroFinanceiroInfo>();
        private Dictionary<int, double> valorHoraRecurso = new Dictionary<int, double>();
        private DbSet<Etapa> _etapas;
        private DbSet<Projeto> _projetos;
        private DbSet<AlocacaoRhHorasMes> _horasMes;
        private ProjetoService _projetoService;
        
        public CronogramaProjetoService(IRepository<Projeto> repository, GestorDbContext context, ProjetoService projetoService) : base(repository)
        {
            _etapas = context.Set<Etapa>();
            _projetos = context.Set<Projeto>();
            _horasMes = context.Set<AlocacaoRhHorasMes>();
            _projetoService = projetoService;
        }

        private Dictionary<int, double> GetDesembolsosEmpresa(Projeto projeto, Empresa empresa) {
            var desembolsos = new Dictionary<int, double>();

            //Zerando hash de desembolsos
            for (int i = 0; i < duracaoProjeto; i++) desembolsos.Add(i+1, 0);

            //Recuperando desembolsos de RH
            var orcamentosEmpresa = OrcamentosProjeto.Where(o => o.RecebedoraId == empresa.Id).ToList();
            foreach (var orcamento in orcamentosEmpresa) {
                foreach (var etapa in projeto.Etapas) {
                    if (orcamento.EtapaId == etapa.Id) {
                        if (orcamento.Tipo == "AlocacaoRh") {
                            var horasEtapa = orcamento.HorasEtapas.Split(',').Select(int.Parse).ToList();
                            int indexMes = 0;
                            foreach (var mes in etapa.Meses) {
                                desembolsos[mes] += (double) orcamento.Custo * horasEtapa[indexMes];
                                indexMes++;
                            }
                        } else {                            
                            desembolsos[etapa.Meses[0]] += (double) orcamento.Total;                            
                        }                        
                    }
                }
            }
             
            return desembolsos;
        }

        private Dictionary<int, double> GetRegistrosEmpresa(Projeto projeto, Empresa empresa) {
            var registros = new Dictionary<int, double>();

            //Zerando hash de desembolsos
            for (int i = 0; i < duracaoProjeto; i++) registros.Add(i+1, 0);

            //Recuperando desembolsos de RH
            var registrosEmpresa = RegistrosFinanceirosProjeto.Where(o => o.RecebedoraId == empresa.Id).ToList();

            foreach (var etapa in projeto.Etapas) {                                        
                foreach (var mes in etapa.Meses) {
                    var mesReferencia = projeto.DataInicioProjeto.AddMonths(mes-1);
                    registros[mes] += (double) registrosEmpresa.Where(x=>x.Etapa == etapa.Ordem && x.MesReferencia == mesReferencia).Sum(x=>x.Custo);
                }
            }
            
            foreach (var registro in registrosEmpresa) {
               
            }
             
            return registros;
        }

        private List<EmpresaCronogramaDto> GetEmpresas(Projeto projeto) {
            var empresas = new List<EmpresaCronogramaDto>();
            projeto.Empresas.ForEach(empresa => {
                var desembolso = GetDesembolsosEmpresa(projeto, empresa);
                var executado = GetRegistrosEmpresa(projeto, empresa);
                if (desembolso.Sum(d => d.Value) > 0) {
                    empresas.Add(new EmpresaCronogramaDto {
                        Nome = empresa.Nome,
                        Desembolso = desembolso.Values.ToList(),
                        Executado = executado.Values.ToList(),
                        Total = Enumerable.Sum(desembolso.Values.ToList())
                    }); 
                }
            });
            return empresas;
        }

        private (int qtd, double valor)  GetQuantidadeRecurso(List<Orcamento> itensOrcamento, int empresaId, string categoriaContabil) {  
            return (
                Decimal.ToInt32(itensOrcamento.Where(x=>x.CategoriaContabilCodigo == categoriaContabil && x.RecebedoraId == empresaId).Sum(x=>x.Quantidade)), 
                (double) itensOrcamento.Where(x=>x.CategoriaContabilCodigo==categoriaContabil && x.RecebedoraId == empresaId).Sum(x=>x.Total)
            );
        }

        private List<RecursoDto> GetRecursos(Etapa etapa) {

            var recursos = new List<RecursoDto>();
            var recursoSoma = new RecursoDto();

            var orcamentosEtapa = OrcamentosProjeto.Where(x=>x.EtapaId == etapa.Id).ToList();
            var empresasEtapa = OrcamentosProjeto.Select(x=>x.RecebedoraId).Distinct().ToList();

            
            
            empresasEtapa.ForEach(empresaId => {
                
                var itens = orcamentosEtapa.Where(x=>x.RecebedoraId == empresaId).ToList();
                if (itens.Count > 0)  {

                    var recurso = new RecursoDto();                
                    recurso.Empresa = itens[0].Recebedora;
                    
                    var totalAudConFin = GetQuantidadeRecurso(itens, empresaId, "AC");
                    var totalMatConsu = GetQuantidadeRecurso(itens, empresaId, "MC");
                    var totalMatPerm = GetQuantidadeRecurso(itens, empresaId, "MP");
                    var totalViaDia =GetQuantidadeRecurso(itens, empresaId, "VD");
                    var totalRH = GetQuantidadeRecurso(itens, empresaId, "RH");
                    var totalServTerc = GetQuantidadeRecurso(itens, empresaId, "ST");
                    var totalStartups = GetQuantidadeRecurso(itens, empresaId, "SU");
                    var totalOutros = GetQuantidadeRecurso(itens, empresaId, "OU");

                    recurso.QtdAudConFin = totalAudConFin.qtd;
                    recurso.QtdMatConsu = totalMatConsu.qtd;
                    recurso.QtdMatPerm = totalMatPerm.qtd;
                    recurso.QtdViaDia =totalViaDia.qtd;
                    recurso.QtdRH = totalRH.qtd;
                    recurso.QtdServTerc = totalServTerc.qtd;
                    recurso.QtdStartups = totalStartups.qtd;
                    recurso.QtdOutros = totalOutros.qtd;

                    recurso.ValorAudConFin = totalAudConFin.valor;
                    recurso.ValorMatConsu = totalMatConsu.valor;
                    recurso.ValorMatPerm = totalMatPerm.valor;
                    recurso.ValorViaDia = totalViaDia.valor;
                    recurso.ValorRH = totalRH.valor;
                    recurso.ValorServTerc = totalServTerc.valor;
                    recurso.ValorStartups = totalStartups.valor;
                    recurso.ValorOutros = totalOutros.valor;

                    recurso.Total = recurso.ValorAudConFin + recurso.ValorMatConsu + recurso.ValorViaDia + recurso.ValorRH + recurso.ValorServTerc + recurso.ValorOutros;

                    recursoSoma.QtdAudConFin += recurso.QtdAudConFin;
                    recursoSoma.QtdMatConsu += recurso.QtdMatConsu;
                    recursoSoma.QtdMatPerm = recurso.QtdMatPerm;
                    recursoSoma.QtdViaDia += recurso.QtdViaDia;
                    recursoSoma.QtdRH += recurso.QtdRH;
                    recursoSoma.QtdServTerc += recurso.QtdServTerc;
                    recursoSoma.QtdStartups = recurso.QtdStartups;
                    recursoSoma.QtdOutros += recurso.QtdOutros;

                    recursoSoma.ValorAudConFin += recurso.ValorAudConFin;
                    recursoSoma.ValorMatConsu += recurso.ValorMatConsu;
                    recursoSoma.ValorMatPerm = recurso.ValorMatPerm;
                    recursoSoma.ValorViaDia += recurso.ValorViaDia;
                    recursoSoma.ValorRH += recurso.ValorRH;
                    recursoSoma.ValorServTerc += recurso.ValorServTerc;
                    recursoSoma.ValorStartups = recurso.ValorStartups;
                    recursoSoma.ValorOutros += recurso.ValorOutros;
                    
                    recursoSoma.Total += recurso.Total;

                    recursos.Add(recurso);
                }                
            });

            recursos.Add(recursoSoma);

            return recursos;
        }

        private List<EtapaCronogramaDto> GetEtapas(Projeto projeto) {

            var etapas = new List<EtapaCronogramaDto>();
            OrcamentosProjeto = _projetoService.GetOrcamentos(projeto.Id).ToList();
            RegistrosFinanceirosProjeto = _projetoService.GetRegistrosFinanceiros(projeto.Id, StatusRegistro.Aprovado).ToList();

            var etapasProjeto = _etapas.Where(e => e.ProjetoId == projeto.Id)                                        
                                        .Include(x=>x.Produto)                                        
                                        .ThenInclude(x=>x.FaseCadeia)
                                        .Include(x=>x.Produto)                                        
                                        .ThenInclude(x=>x.TipoDetalhado)
                                        .Include(x=>x.Produto)                                        
                                        .ThenInclude(x=>x.ProdutoTipo)                                        
                                        .OrderBy(x=>x.Ordem).ToList();                        
            var numero = 1;
            etapasProjeto.ForEach(e => {
                etapas.Add(new EtapaCronogramaDto {
                    
                    Numero = e.Ordem,
                    Etapa = e.DescricaoAtividades,
                    Meses = e.Meses,
                    Produto = e.Produto.Titulo,
                    Detalhe = new DetalheEtapaDto {
                        Etapa = e.DescricaoAtividades,
                        ProdutoTitulo = e.Produto.Titulo,
                        ProdutoDescricao = e.Produto.Descricao,
                        InicioPeriodo = projeto.DataInicioProjeto.AddMonths(e.Meses.Min()-1),
                        FimPeriodo = projeto.DataInicioProjeto.AddMonths(e.Meses.Max()-1),
                        ProdutoTipoDetalhado = e.Produto.TipoDetalhado.Nome,
                        FaseCadeia = e.Produto.FaseCadeia.Nome,
                        ProdutoTipo = e.Produto.ProdutoTipo.Nome,
                        Recursos = GetRecursos(e),
                    }
                });
                numero++;
            });
            
            return etapas;
        }

        private InicioCronogramaDto GetInicio(Projeto projeto) {
            return new InicioCronogramaDto {
                    Ano = projeto.DataInicioProjeto.Year, 
                    Mes = projeto.DataInicioProjeto.Month, 
                    NumeroMeses = projeto.Duracao
                };
        }
        
        public CronogramaDto GetCronograma(int id)
        {
            var projeto = this._projetos.Where(x => x.Id == id)
                                        .Include(x => x.Empresas)
                                        .Include(x => x.Etapas)
                                        .FirstOrDefault();
            var cronograma = new CronogramaDto();
            if (projeto != null) {
                duracaoProjeto = projeto.Duracao;
                cronograma.Inicio = GetInicio(projeto);
                cronograma.Etapas = GetEtapas(projeto);
                cronograma.Empresas = GetEmpresas(projeto);
            }
            return cronograma;
        }

        public DetalheEtapaDto GetDetalheEtapa(Guid guid, int numeroEtapa)
        {
            var etapa = new DetalheEtapaDto();

            etapa.Etapa = "";
            etapa.ProdutoDescricao = "";
            etapa.InicioPeriodo = new DateTime(2022, 8, 1);
            etapa.FimPeriodo = new DateTime(2022, 11, 1);            
            return etapa;
        }

    }
}
