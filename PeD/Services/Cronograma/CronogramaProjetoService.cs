using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PeD.Core.ApiModels.Cronograma;
using PeD.Core.Models.Projetos;
using TaesaCore.Interfaces;
using TaesaCore.Services;
using PeD.Data;

namespace PeD.Services.Cronograma
{
    public class CronogramaProjetoService : BaseService<Projeto>
    {
        private int duracaoProjeto = 0;
        private Dictionary<int, List<AlocacaoRh>> alocacaoRhEmpresa = new Dictionary<int, List<AlocacaoRh>>();
        private Dictionary<int, List<RecursoMaterial.AlocacaoRm>> alocacaoRmEmpresa = new Dictionary<int, List<RecursoMaterial.AlocacaoRm>>();
        private Dictionary<int, double> valorHoraRecurso = new Dictionary<int, double>();
        private DbSet<Etapa> _etapas;
        private DbSet<Projeto> _projetos;
        private DbSet<AlocacaoRhHorasMes> _horasMes;
        
        public CronogramaProjetoService(IRepository<Projeto> repository, GestorDbContext context) : base(repository)
        {
            _etapas = context.Set<Etapa>();
            _projetos = context.Set<Projeto>();
            _horasMes = context.Set<AlocacaoRhHorasMes>();
        }

        private Dictionary<int, double> GetDesembolsosEmpresa(Projeto projeto, Empresa empresa) {
            var desembolsos = new Dictionary<int, double>();

            //Zerando hash de desembolsos
            for (int i = 0; i < duracaoProjeto; i++) desembolsos.Add(i+1, 0);

            //Recuperando desembolsos de RH
            var alocacoesRhEmpresa = alocacaoRhEmpresa.ContainsKey(empresa.Id) ? alocacaoRhEmpresa[empresa.Id] : new List<AlocacaoRh>();
            var horas = _horasMes.Where(x => alocacoesRhEmpresa.Select(a=>a.Id).Contains(x.AlocacaoRhId)).ToList();
            for (int mes = 1; mes <= duracaoProjeto; mes++) {
                var total = horas.Where(x => x.Mes == mes).Sum(x => x.Horas * valorHoraRecurso[x.AlocacaoRhId]);                
                desembolsos[mes] += total;            
            }

            /*//Recuperando desembolsos de RM
            var alocacoesRmEmpresa = alocacaoRmEmpresa.ContainsKey(empresa.Id) ? alocacaoRmEmpresa[empresa.Id] : new List<AlocacaoRm>();
            var etapas = projeto.Etapas?.Where(x => alocacoesRmEmpresa.Select(a=>a.EtapaId).Contains(x.Id)).ToList();
            etapas?.ForEach(e => {
                e.Meses.ForEach(mes => {
                    var total = (double) etapas
                                .Where(x=>x.Id==e.Id)
                                .Sum(x => x.RecursosMateriaisAlocacoes
                                    .Where(x=>x.MesDesembolso==mes && x.EmpresaFinanciadora.Id == empresa.Id)
                                    .Sum(r => r.Valor)
                                );                    
                    desembolsos[mes] += total;
                });
                
            });*/
        
            return desembolsos;
        }

        private List<EmpresaCronogramaDto> GetEmpresas(Projeto projeto) {
            var empresas = new List<EmpresaCronogramaDto>();
            projeto.Empresas.ForEach(empresa => {
                var desembolso = GetDesembolsosEmpresa(projeto, empresa);
                empresas.Add(new EmpresaCronogramaDto {
                    Nome = empresa.Nome,
                    Desembolso = desembolso.Values.ToList(),
                    Total = Enumerable.Sum(desembolso.Values.ToList())
                });
            });
            return empresas;
        }

        private void RegistrarAlocacaoRh(int empresaId, List<AlocacaoRh> alocacao) {
            if (alocacaoRhEmpresa.ContainsKey(empresaId)) {
                alocacaoRhEmpresa[empresaId].AddRange(alocacao);
            } else {
                alocacaoRhEmpresa.Add(empresaId, alocacao);
            }
            alocacao.ForEach(a => {
                if (!valorHoraRecurso.ContainsKey(a.Id)) {
                    valorHoraRecurso.Add(a.Id, (double) a.RecursoHumano.ValorHora);
                }   
            });            
        }

        private void RegistrarAlocacaoRm(int empresaId, List<RecursoMaterial.AlocacaoRm> alocacao) {
            if (alocacaoRmEmpresa.ContainsKey(empresaId)) {
                alocacaoRmEmpresa[empresaId].AddRange(alocacao);
            } else {
                alocacaoRmEmpresa.Add(empresaId, alocacao);
            }
        }

        private (int qtd, double valor)  GetQuantidadeRecurso(List<AlocacaoRh> alocacaoRh, List<RecursoMaterial.AlocacaoRm> alocacaoRm, Empresa empresa, string categoriaContabil) {                    
            if (categoriaContabil == "RH") {
                var alocacao = alocacaoRh.Where(r => r.EmpresaFinanciadora.Id == empresa.Id).ToList();
                RegistrarAlocacaoRh(empresa.Id, alocacao);
                return (
                    Decimal.ToInt32(alocacao.Sum(x => x.HorasMeses.Sum(y => y.Horas))), 
                    (double) alocacao.Sum(x => x.RecursoHumano.ValorHora * x.HorasMeses.Sum(y => y.Horas))
                );
            } else {
                var alocacao = alocacaoRm.Where(r => r.EmpresaFinanciadora.Id == empresa.Id).ToList();
                RegistrarAlocacaoRm(empresa.Id, alocacao);
                var alocacaoCategoria = alocacao.Where(r => r.RecursoMaterial.CategoriaContabil.Valor == categoriaContabil);
                return (
                    Decimal.ToInt32(alocacaoCategoria.Sum(x => x.Quantidade)), 
                    (double) alocacaoCategoria.Sum(x => x.Custo)
                );
            }
        }

        /*private List<RecursoDto> GetRecursos(Etapa etapa) {

            var empresas = new List<Empresa>();
            var recursos = new List<RecursoDto>();
            var recursoSoma = new RecursoDto();

            var alocacaoRh = etapa.Alocacoes.Where(x=>x.Tipo== "AlocacaoRh").ToList();
            var alocacaoRm = etapa.Alocacoes.Where(x => x.Tipo == "AlocacaoRm").ToList();

            empresas.AddRange(alocacaoRh.Select(r => r.EmpresaFinanciadora));
            empresas.AddRange(alocacaoRm.Select(r => r.EmpresaFinanciadora));
            empresas = empresas.GroupBy(e => e.Id).Select(e => e.First()).ToList();
            
            empresas.ForEach(e => {
                var recurso = new RecursoDto();
                recurso.Empresa = e.Nome;

                var totalAudConFin = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "AC");
                var totalMatConsu = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "MC");
                var totalMatPerm = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "MP");
                var totalViaDia =GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "VD");
                var totalRH = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "RH");
                var totalServTerc = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "ST");
                var totalStartups = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "SU");
                var totalOutros = GetQuantidadeRecurso(alocacaoRh, alocacaoRm, e, "OU");

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
            });

            recursos.Add(recursoSoma);

            return recursos;
        }*/

        private List<EtapaCronogramaDto> GetEtapas(Projeto projeto) {

            var etapas = new List<EtapaCronogramaDto>();

            var etapasProjeto = _etapas.Where(e => e.ProjetoId == projeto.Id)                                        
                                        //Produto
                                        .Include(x=>x.Produto)                                        
                                        .ThenInclude(x=>x.FaseCadeia)
                                        .Include(x=>x.Produto)                                        
                                        .ThenInclude(x=>x.TipoDetalhado)
                                        .Include(x=>x.Produto)                                        
                                        .ThenInclude(x=>x.ProdutoTipo)
                                        //Recursos Materiais
                                        .Include(x=>x.Alocacoes)
                                        .ThenInclude(x=>x.EmpresaFinanciadora)                                                                                
                                        //Recursos Humanos                                        
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
            var projeto = this._projetos.Where(x => x.Id == id).Include(x => x.Empresas).FirstOrDefault();
            duracaoProjeto = projeto.Duracao;
            var cronograma = new CronogramaDto();
            cronograma.Inicio = GetInicio(projeto);
            cronograma.Etapas = GetEtapas(projeto);
            cronograma.Empresas = GetEmpresas(projeto);
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
