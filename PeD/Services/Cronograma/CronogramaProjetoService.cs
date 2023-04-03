using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PeD.Core.ApiModels.Cronograma;
using PeD.Core.ApiModels.Projetos;
using PeD.Core.Models.Projetos;
using PeD.Core.Requests.Projetos;
using PeD.Data;
using PeD.Services.Captacoes;
using PeD.Services.Projetos;
using TaesaCore.Interfaces;
using TaesaCore.Services;

namespace PeD.Services.Cronograma
{
    public class CronogramaProjetoService : BaseService<Projeto>
    {
        private int duracaoProjeto = 0;
        private int saltoId = 1000000;
        private List<Orcamento> OrcamentosProjeto = new List<Orcamento>();
        private List<RegistroFinanceiroInfo> RegistrosFinanceirosProjeto = new List<RegistroFinanceiroInfo>();
        private Dictionary<int, double> contrapartidaMesValor = new Dictionary<int, double>();
        private Dictionary<int, double> valorHoraRecurso = new Dictionary<int, double>();
        private DbSet<Etapa> _etapas;
        private DbSet<Projeto> _projetos;
        private DbSet<AlocacaoRhHorasMes> _horasMes;
        private ProjetoService _projetoService;
        private CronogramaService _cronogramaService;
        private ContabPedDbContext _contabPedContext;

        public CronogramaProjetoService(IRepository<Projeto> repository, GestorDbContext context, ContabPedDbContext contabPedContext, ProjetoService projetoService, CronogramaService cronogramaService) : base(repository)
        {
            _etapas = context.Set<Etapa>();
            _projetos = context.Set<Projeto>();
            _horasMes = context.Set<AlocacaoRhHorasMes>();
            _projetoService = projetoService;
            _cronogramaService = cronogramaService;
            _contabPedContext = contabPedContext;
        }

        private Dictionary<int, double> GetDesembolsosEmpresa(Projeto projeto, Empresa empresa)
        {
            var desembolsos = new Dictionary<int, double>();

            //Zerando hash de desembolsos
            for (int i = 0; i < duracaoProjeto; i++) desembolsos.Add(i + 1, 0);

            //Recuperando desembolsos de RH
            var orcamentosEmpresa = OrcamentosProjeto.Where(o => o.RecebedoraId == empresa.Id).ToList();
            foreach (var orcamento in orcamentosEmpresa)
            {
                foreach (var etapa in projeto.Etapas)
                {
                    if (orcamento.EtapaId == etapa.Id)
                    {
                        if (orcamento.Tipo == "AlocacaoRh")
                        {
                            var horasEtapa = orcamento?.HorasEtapas?.Split(',').Select(int.Parse).ToList();
                            if (horasEtapa != null)
                            {
                                int indexMes = 0;
                                foreach (var mes in etapa.Meses)
                                {
                                    if (orcamento.Recebedora.ToUpper() == "NORTE ENERGIA" || orcamento.FinanciadoraId != orcamento.RecebedoraId)
                                    {
                                        desembolsos[mes] += (double)orcamento.Custo * horasEtapa[indexMes];
                                    }
                                    //Registrando Contrapartida
                                    if (orcamento.Financiadora.ToUpper() != "NORTE ENERGIA")
                                    {
                                        RegistrarContrapartida(mes, (double)(orcamento.Custo * horasEtapa[indexMes]));
                                    }

                                    indexMes++;
                                }
                            }
                        }
                        else
                        {
                            if (orcamento.Recebedora.ToUpper() == "NORTE ENERGIA" || orcamento.FinanciadoraId != orcamento.RecebedoraId)
                            {
                                desembolsos[etapa.Meses[0]] += (double)orcamento.Total;
                            }
                            //Registrando Contrapartida
                            if (orcamento.Financiadora.ToUpper() != "NORTE ENERGIA")
                                RegistrarContrapartida(etapa.Meses[0], (double)orcamento.Total);
                        }
                    }
                }
            }

            return desembolsos;
        }

        private Dictionary<int, double> GetRegistrosEmpresa(Projeto projeto, Empresa empresa)
        {
            var registros = new Dictionary<int, double>();

            //Recuperando desembolsos de RH
            var registrosEmpresa = RegistrosFinanceirosProjeto.Where(o => o.RecebedoraId == empresa.Id).ToList();


            //Zerando hash de desembolsos
            for (int i = 0; i < duracaoProjeto; i++)
            {
                registros.Add(i + 1, 0);
            }

            for (int mes = 1; mes <= duracaoProjeto; mes++)
            {
                var mesReferencia = projeto.DataInicioProjeto.AddMonths(mes - 1);
                var valorMes = (double)registrosEmpresa.Where(x => x.MesReferencia == mesReferencia).Sum(x => x.Custo);
                registros[mes] += valorMes;
            }

            return registros;
        }

        private List<EmpresaCronogramaDto> GetEmpresas(Projeto projeto)
        {
            RegistrosFinanceirosProjeto = _projetoService.GetRegistrosFinanceiros(projeto.Id, StatusRegistro.Aprovado).ToList();
            var empresas = new List<EmpresaCronogramaDto>();
            projeto.Empresas.ForEach(empresa =>
            {
                var desembolso = GetDesembolsosEmpresa(projeto, empresa);
                var executado = GetRegistrosEmpresa(projeto, empresa);
                if (desembolso.Sum(d => d.Value) > 0 || executado.Sum(d => d.Value) > 0)
                {
                    empresas.Add(new EmpresaCronogramaDto
                    {
                        Nome = empresa.Nome,
                        Desembolso = desembolso.Values.ToList(),
                        Executado = executado.Values.ToList(),
                        Total = Enumerable.Sum(desembolso.Values.ToList()),
                        TotalExecutado = Enumerable.Sum(executado.Values.ToList())
                    });
                }
            });
            return empresas;
        }

        private (int qtd, double valor) GetQuantidadeRecurso(List<Orcamento> itensOrcamento, int empresaId, string categoriaContabil)
        {
            return (
                Decimal.ToInt32(itensOrcamento.Where(x => x.CategoriaContabilCodigo == categoriaContabil && x.RecebedoraId == empresaId).Sum(x => x.Quantidade)),
                (double)itensOrcamento.Where(x => x.CategoriaContabilCodigo == categoriaContabil && x.RecebedoraId == empresaId).Sum(x => x.Total)
            );
        }

        private List<RecursoDto> GetRecursos(Etapa etapa)
        {

            var recursos = new List<RecursoDto>();
            var recursoSoma = new RecursoDto();

            var orcamentosEtapa = OrcamentosProjeto.Where(x => x.EtapaId == etapa.Id).ToList();
            var empresasEtapa = OrcamentosProjeto.Select(x => x.RecebedoraId).Distinct().ToList();

            empresasEtapa.ForEach(empresaId =>
            {

                var itens = orcamentosEtapa.Where(x => x.RecebedoraId == empresaId).ToList();
                if (itens.Count > 0)
                {

                    var recurso = new RecursoDto();
                    recurso.Empresa = itens[0].Recebedora;

                    var totalAudConFin = GetQuantidadeRecurso(itens, empresaId, "AC");
                    var totalMatConsu = GetQuantidadeRecurso(itens, empresaId, "MC");
                    var totalMatPerm = GetQuantidadeRecurso(itens, empresaId, "MP");
                    var totalViaDia = GetQuantidadeRecurso(itens, empresaId, "VD");
                    var totalRH = GetQuantidadeRecurso(itens, empresaId, "RH");
                    var totalServTerc = GetQuantidadeRecurso(itens, empresaId, "ST");
                    var totalStartups = GetQuantidadeRecurso(itens, empresaId, "SU");
                    var totalOutros = GetQuantidadeRecurso(itens, empresaId, "OU");

                    recurso.QtdAudConFin = totalAudConFin.qtd;
                    recurso.QtdMatConsu = totalMatConsu.qtd;
                    recurso.QtdMatPerm = totalMatPerm.qtd;
                    recurso.QtdViaDia = totalViaDia.qtd;
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

        private List<EtapaCronogramaDto> GetEtapas(Projeto projeto)
        {

            var etapas = new List<EtapaCronogramaDto>();
            OrcamentosProjeto = _projetoService.GetOrcamentos(projeto.Id).ToList();

            var etapasProjeto = _etapas.Where(e => e.ProjetoId == projeto.Id)
                                        .Include(x => x.Produto)
                                        .ThenInclude(x => x.FaseCadeia)
                                        .Include(x => x.Produto)
                                        .ThenInclude(x => x.TipoDetalhado)
                                        .Include(x => x.Produto)
                                        .ThenInclude(x => x.ProdutoTipo)
                                        .OrderBy(x => x.Ordem).ToList();
            var numero = 1;
            etapasProjeto.ForEach(e =>
            {
                etapas.Add(new EtapaCronogramaDto
                {

                    Numero = e.Ordem,
                    Etapa = e.DescricaoAtividades,
                    Meses = e.Meses,
                    Produto = e.Produto.Titulo,
                    Detalhe = new DetalheEtapaDto
                    {
                        Etapa = e.DescricaoAtividades,
                        ProdutoTitulo = e.Produto.Titulo,
                        ProdutoDescricao = e.Produto.Descricao,
                        InicioPeriodo = projeto.DataInicioProjeto.AddMonths(e.Meses.Min() - 1),
                        FimPeriodo = projeto.DataInicioProjeto.AddMonths(e.Meses.Max() - 1),
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

        private InicioCronogramaDto GetInicio(Projeto projeto)
        {
            return new InicioCronogramaDto
            {
                Ano = projeto.DataInicioProjeto.Year,
                Mes = projeto.DataInicioProjeto.Month,
                NumeroMeses = projeto.Duracao
            };
        }

        public CronogramaDto GetCronograma(int id, bool consolidado = false)
        {
            var projeto = this._projetos.Where(x => x.Id == id)
                                        .Include(x => x.Empresas)
                                        .Include(x => x.Etapas)
                                        .FirstOrDefault();
            var cronograma = new CronogramaDto();
            if (projeto != null)
            {
                duracaoProjeto = projeto.Duracao;

                //Zerando hash de contrapartidas
                for (int i = 0; i < duracaoProjeto; i++) contrapartidaMesValor.Add(i + 1, 0);

                cronograma.Inicio = GetInicio(projeto);
                if (!consolidado) cronograma.Etapas = GetEtapas(projeto);
                cronograma.Empresas = GetEmpresas(projeto);
                cronograma.Contrapartidas = contrapartidaMesValor.Select(x => x.Value).ToList();
            }
            return cronograma;
        }

        private void RegistrarContrapartida(int mes, double valor)
        {
            if (contrapartidaMesValor.ContainsKey(mes))
                contrapartidaMesValor[mes] += valor;
            else contrapartidaMesValor[mes] = valor;
        }

        public CronogramaConsolidadoDto GetCronogramaConsolidadoSimulado(List<CronogramaSimuladoRequest> projetos)
        {
            List<Projeto> projetosSimulados = new List<Projeto>();
            foreach (var projeto in projetos)
            {
                var mesProjeto = Int32.Parse(projeto.MesInicio.Split("-")[1]);
                var anoProjeto = Int32.Parse(projeto.MesInicio.Split("-")[0]);
                var dataInicio = new DateTime(anoProjeto, mesProjeto, 1);
                projetosSimulados.Add(new Projeto
                {
                    Id = projeto.Id + saltoId,
                    Titulo = projeto.Etapa,
                    TituloCompleto = "Simulação",
                    Codigo = projeto.Proposta,
                    DataInicioProjeto = dataInicio.Date,
                    DataFinalProjeto = dataInicio.AddMonths(projeto.Duracao).Date,
                }); ;
            }
            return GetCronogramaConsolidado(projetosSimulados);
        }

        public CronogramaConsolidadoDto GetCronogramaConsolidado(List<Projeto> projetosSimulados = null)
        {
            int mesInicio = 0;
            int anoInicio = 0;
            int mesFim = 0;
            int anoFim = 0;
            var projetos = this._projetos.Where(x => x.Status == Status.Execucao)
                                        .Include(x => x.Empresas)
                                        .Include(x => x.Etapas)
                                        .OrderBy(x => x.DataInicioProjeto)
                                        .ToList();

            var cronograma = new CronogramaConsolidadoDto();
            cronograma.Etapas = new List<ProjetoCronogramaDto>();

            var empresasConsolidado = new List<EmpresaCronogramaDto>();
            var cronogramasProjetos = new Dictionary<int, CronogramaDto>();

            if (projetosSimulados != null)
                foreach (Projeto simulado in projetosSimulados)
                {
                    projetos.Add(simulado);
                }

            foreach (var projeto in projetos)
            {
                OrcamentosProjeto = _projetoService.GetOrcamentos(projeto.Id).ToList();
                var projetoDto = new ProjetoCronogramaDto
                {
                    ProjetoId = projeto.Id,
                    Titulo = projeto.Titulo,
                    TituloCompleto = projeto.TituloCompleto,
                    Etapa = (projeto.Id > saltoId) ? projeto.Titulo : projeto.Codigo + " - " + projeto.Titulo,
                    Produto = projeto.TituloCompleto,
                    Codigo = projeto.Codigo,
                    Numero = projeto.Id.ToString(),
                    MesInicio = projeto.DataInicioProjeto.Month,
                    AnoInicio = projeto.DataInicioProjeto.Year,
                    MesFim = projeto.DataFinalProjeto.Month,
                    AnoFim = projeto.DataFinalProjeto.Year,

                };
                cronograma.Etapas.Add(projetoDto);

                if (GetPesoMesAno(projetoDto.MesInicio, projetoDto.AnoInicio) < GetPesoMesAno(mesInicio, anoInicio) || mesInicio == 0)
                {
                    mesInicio = projetoDto.MesInicio;
                    anoInicio = projetoDto.AnoInicio;
                }

                if (GetPesoMesAno(projetoDto.MesFim, projetoDto.AnoFim) > GetPesoMesAno(mesFim, anoFim) || mesFim == 0)
                {
                    mesFim = projetoDto.MesFim;
                    anoFim = projetoDto.AnoFim;
                }
                //Recuperando cronograma de cada projeto
                duracaoProjeto = projeto.Duracao;

                if (projeto.Id < saltoId)
                {
                    cronogramasProjetos.Add(projeto.Id, new CronogramaDto
                    {
                        Inicio = GetInicio(projeto),
                        Empresas = GetEmpresas(projeto),
                    });
                }
                else
                {
                    if (cronogramasProjetos.ContainsKey(projeto.Id))
                    {
                        cronogramasProjetos[projeto.Id] = _cronogramaService.GetCronograma(new Guid(projeto.Codigo));
                    }
                    else
                    {
                        cronogramasProjetos.Add(projeto.Id, _cronogramaService.GetCronograma(new Guid(projeto.Codigo)));
                    }
                }
            }


            //Calculando o número de meses do cronograma consolidado
            cronograma.Inicio = new InicioCronogramaDto
            {
                Mes = mesInicio,
                Ano = anoInicio,
                NumeroMeses = (anoFim - anoInicio) * 12 + (mesFim - mesInicio) + 1
            };

            //Recuperando as empresas de todos os projetos
            var desembolsosConsolidado = new Dictionary<int, double>();

            //Zerando hash de desembolsos
            for (int i = 0; i < cronograma.Inicio.NumeroMeses; i++) desembolsosConsolidado.Add(i + 1, 0);

            foreach (var projeto in cronograma.Etapas)
            {
                if (projeto.ProjetoId < saltoId)
                {
                    projeto.Meses = GetMesesProjeto(cronogramasProjetos[projeto.ProjetoId], cronograma);
                    cronogramasProjetos[projeto.ProjetoId].Empresas.ForEach(x =>
                    {
                        if (!empresasConsolidado.Any(y => y.Nome == x.Nome))
                        {
                            empresasConsolidado.Add(
                                new EmpresaCronogramaDto
                                {
                                    Nome = x.Nome,
                                    Desembolso = desembolsosConsolidado.Values.ToList(),
                                    Executado = desembolsosConsolidado.Values.ToList(),
                                }
                            );
                        }
                    });
                }
                else
                {
                    cronogramasProjetos[projeto.ProjetoId].Inicio.Mes = projeto.MesInicio;
                    cronogramasProjetos[projeto.ProjetoId].Inicio.Ano = projeto.AnoInicio;
                    projeto.Meses = GetMesesProjeto(cronogramasProjetos[projeto.ProjetoId], cronograma);
                }
            }

            foreach (var projeto in cronograma.Etapas)
            {
                SomarDesembolsosEmpresa(ref empresasConsolidado, projeto.ProjetoId, projeto, cronogramasProjetos[projeto.ProjetoId], cronograma);
            }

            cronograma.Empresas = empresasConsolidado;

            //Recuperando total do contabiped
            cronograma.SaldoAtual = _contabPedContext.Set<PeD.Core.Models.DadosContabPed>().Sum(x => x.Valor);
            return cronograma;
        }

        private void SomarDesembolsosEmpresa(
            ref List<EmpresaCronogramaDto> empresas,
            int projetoId,
            ProjetoCronogramaDto projetoDto,
            CronogramaDto cronogramaProjeto,
            CronogramaConsolidadoDto cronogramaConsolidado
        )
        {
            int mesInicioProjeto = GetMesInicioProjeto(cronogramaProjeto, cronogramaConsolidado);
            foreach (var empresa in empresas)
            {
                var desembolsoEmpresa = cronogramaProjeto.Empresas.Where(x => x.Nome == empresa.Nome).FirstOrDefault();
                if (desembolsoEmpresa != null)
                {
                    for (int i = 0; i < desembolsoEmpresa.Desembolso.Count; i++)
                    {
                        empresa.Desembolso[mesInicioProjeto + i] += desembolsoEmpresa.Desembolso[i];
                    }
                    if (desembolsoEmpresa.Executado != null)
                        for (int i = 0; i < desembolsoEmpresa.Executado.Count; i++)
                        {
                            empresa.Executado[mesInicioProjeto + i] += desembolsoEmpresa.Executado[i];
                        }
                }
            }
        }

        private List<int> GetMesesProjeto(CronogramaDto cronogramaProjeto, CronogramaConsolidadoDto cronogramaConsolidado)
        {
            var mesInicial = GetMesInicioProjeto(cronogramaProjeto, cronogramaConsolidado);
            var meses = new List<int>();
            for (int i = 1; i <= cronogramaProjeto.Inicio.NumeroMeses; i++)
            {
                meses.Add(mesInicial + i);
            }
            return meses;
        }

        private int GetMesInicioProjeto(CronogramaDto cronogramaProjeto, CronogramaConsolidadoDto cronogramaConsolidado)
        {
            var dataInicioCronograma = new DateTime(cronogramaConsolidado.Inicio.Ano, cronogramaConsolidado.Inicio.Mes, 1);
            var dataInicioProjeto = new DateTime(cronogramaProjeto.Inicio.Ano, cronogramaProjeto.Inicio.Mes, 1);
            return (int)Math.Round(dataInicioProjeto.Subtract(dataInicioCronograma).TotalDays / 30);
        }

        private double GetPesoMesAno(int mes, int ano)
        {
            return ano + (mes / 12.0);
        }

    }
}
