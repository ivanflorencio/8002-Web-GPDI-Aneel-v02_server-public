using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.Extensions;
using PeD.Core.Models;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Propostas;
using PeD.Core.Validators;
using PeD.Data;
using PeD.Services.Analises;
using PeD.Views.Email.Captacao.Propostas;
using TaesaCore.Interfaces;
using TaesaCore.Services;


namespace PeD.Services.Captacoes
{
    public class PropostaService : BaseService<Proposta>
    {
        private DbSet<Proposta> _captacaoPropostas;
        private DbSet<Captacao> _captacao;
        private DbSet<PropostaRelatorioDiretoria> _propostaRelatorioDiretoria;
        private DbSet<PropostaNotaTecnica> _propostaNotaTecnica;
        private DbSet<RecursoHumano> _recursoHumano;
        private DbSet<RecursoMaterial> _recursoMaterial;
        private DbSet<Risco> _risco;
        private DbSet<Escopo> _escopo;
        private DbSet<AlocacaoRh> _recursoHumanoAlocacao;
        private DbSet<AlocacaoRm> _recursoMaterialAlocacao;
        private DbSet<Meta> _metas;
        private DbSet<PlanoTrabalho> _planoTrabalho;
        private IMapper _mapper;
        private ILogger<PropostaService> _logger;
        private IViewRenderService renderService;
        private GestorDbContext context;
        private SendGridService _sendGridService;
        private UserService _userService;
        private PdfService _pdfService;
        private AnalisePedService _analisePedService;
        private AnaliseTecnicaService _analiseTecnicaService;

        public PropostaService(IRepository<Proposta> repository, GestorDbContext context, IMapper mapper,
            IViewRenderService renderService, SendGridService sendGridService, UserService userService,
            ILogger<PropostaService> logger, ArquivoService arquivoService, PdfService pdfService,
            AnalisePedService analisePedService, AnaliseTecnicaService analiseTecnicaService)
            : base(repository)
        {
            this.context = context;
            _mapper = mapper;
            this.renderService = renderService;
            _sendGridService = sendGridService;
            _userService = userService;
            _logger = logger;
            _pdfService = pdfService;
            _captacaoPropostas = context.Set<Proposta>();
            _captacao = context.Set<Captacao>();
            _propostaRelatorioDiretoria = context.Set<PropostaRelatorioDiretoria>();
            _propostaNotaTecnica = context.Set<PropostaNotaTecnica>();
            _recursoHumano = context.Set<RecursoHumano>();
            _recursoMaterial = context.Set<RecursoMaterial>();
            _risco = context.Set<Risco>();
            _escopo = context.Set<Escopo>();
            _metas = context.Set<Meta>();
            _planoTrabalho = context.Set<PlanoTrabalho>();
            _recursoHumanoAlocacao = context.Set<AlocacaoRh>();
            _recursoMaterialAlocacao = context.Set<AlocacaoRm>();
            _analisePedService = analisePedService;
            _analiseTecnicaService = analiseTecnicaService;
        }

        #region Lists

        public IEnumerable<Proposta> GetPropostasEncerradasPendentes()
        {
            return _captacaoPropostas
                .AsQueryable()
                .Include(p => p.Fornecedor)
                .ThenInclude(f => f.Responsavel)
                .Include(p => p.Captacao)
                .Where(p => p.Participacao == StatusParticipacao.Aceito
                            && p.Captacao.Status == Captacao.CaptacaoStatus.Fornecedor
                            && p.Captacao.Termino < DateTime.Today).ToList();
        }


        public List<Proposta> GetPropostasEncerradas(string responsavelId) =>
            _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Contrato)
                .Include(p =>
                    p.Captacao)
                .Where(cp => cp.ResponsavelId == responsavelId &&
                                (cp.Captacao.Status >= Captacao.CaptacaoStatus.Encerrada ||
                                cp.Participacao == StatusParticipacao.Rejeitado)).ToList();

        public IEnumerable<Proposta> GetPropostasPorResponsavel(string userId,
            Captacao.CaptacaoStatus status = Captacao.CaptacaoStatus.Fornecedor)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .Include(p => p.Contrato)
                .Where(cp =>
                    cp.ResponsavelId == userId &&
                    cp.Captacao.Status == status &&
                    cp.Participacao != StatusParticipacao.Rejeitado)
                .ToList();
        }

        #endregion

        public Proposta GetProposta(int id)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .FirstOrDefault(p => p.Id == id);
        }

        public Proposta GetProposta(Guid guid)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Contrato)
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Arquivos)
                .FirstOrDefault(p => p.Guid == guid);
        }

        public TabelaValorHora GetTabelaValorHora(Guid guid)
        {
            var captacao = _captacaoPropostas
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Demanda)
                .ThenInclude(d => d.TabelaValorHora)
                .FirstOrDefault(p => p.Guid == guid);
            return captacao?.Captacao?.Demanda?.TabelaValorHora;
        }

        public Proposta GetPropostaFull(int id)
        {
            var proposta = _captacaoPropostas
                .AsNoTracking()
                //Captacao
                .Include(p => p.Captacao).ThenInclude(c => c.Tema)
                .Include(p => p.Captacao).ThenInclude(c => c.SubTemas).ThenInclude(s => s.SubTema)
                //
                .Include(p => p.Etapas)
                .ThenInclude(e => e.Produto)
                // Produto
                .Include("Produtos.ProdutoTipo")
                .Include("Produtos.FaseCadeia")
                .Include("Produtos.TipoDetalhado")

                //Empresas
                .Include(p => p.Empresas)

                .Include(p => p.Escopo)
                .Include(p => p.PlanoTrabalho)
                .Include(p => p.Fornecedor)

                .FirstOrDefault(p => p.Id == id);

            proposta.Riscos = _risco.Where(r => r.PropostaId == id).ToList();
            proposta.Metas = _metas.Where(m => m.PropostaId == id).ToList();
            proposta.RecursosHumanos = _recursoHumano.Where(r => r.PropostaId == id).ToList();
            proposta.RecursosHumanosAlocacoes = _recursoHumanoAlocacao.Where(r => r.PropostaId == id).ToList();
            proposta.RecursosMateriais = _recursoMaterial.Where(r => r.PropostaId == id).ToList();
            proposta.RecursosMateriaisAlocacoes = _recursoMaterialAlocacao.Where(r => r.PropostaId == id).ToList();
            proposta.AnalisePed = _analisePedService.GetAnalisePedProposta(id);
            proposta.AnaliseTecnica = _analiseTecnicaService.GetAnaliseTecnicaProposta(id);

            return proposta;
        }

        public Proposta GetPropostaPorResponsavel(int captacaoId, string userId)
        {
            return GetPropostaPorResponsavel(captacaoId, userId, Captacao.CaptacaoStatus.Fornecedor);
        }

        public List<Proposta> GetPropostasSimulacao()
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .Include(p => p.Contrato)
                .Where(cp =>
                    cp.DataParticipacao != null &&
                    cp.Captacao.Status != Captacao.CaptacaoStatus.Cancelada &&
                    cp.Captacao.Status != Captacao.CaptacaoStatus.Fornecedor &&
                    cp.Captacao.Status != Captacao.CaptacaoStatus.AnaliseRisco &&
                    cp.Participacao != StatusParticipacao.Rejeitado)
                .ToList();
        }

        public Proposta GetPropostaPorResponsavel(int captacaoId, string userId,
            params Captacao.CaptacaoStatus[] status)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Arquivos)
                .FirstOrDefault(cp => cp.Fornecedor.ResponsavelId == userId &&
                                        cp.CaptacaoId == captacaoId &&
                                        status.Contains(cp.Captacao.Status) &&
                                        cp.Participacao != StatusParticipacao.Rejeitado);
        }

        public PropostaContrato GetContrato(int propostaId)
        {
            var proposta = _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Contrato)
                .Include(p => p.Contrato).ThenInclude(c => c.File)
                .Include(p => p.Contrato)
                .ThenInclude(c => c.Parent)
                .FirstOrDefault(c => c.Id == propostaId);


            if (proposta?.Captacao?.ContratoId != null)
                return proposta.Contrato ?? new PropostaContrato()
                {
                    PropostaId = proposta.Id,
                    Parent = proposta.Captacao.Contrato,
                    ParentId = (int)proposta.Captacao?.ContratoId
                };

            return null;
        }

        public void SalvarRelatorioDiretoria(int relatorioId, string conteudo, bool isDraft)
        {
            var relatorioProposta = _propostaRelatorioDiretoria
                                        .Include(x => x.Proposta)
                                        .Where(x => x.Id == relatorioId)
                                        .FirstOrDefault();

            var hash = relatorioProposta.Conteudo?.ToSHA256() ?? "";
            var hasChanges = !hash.Equals(conteudo.ToSHA256());

            relatorioProposta.Finalizado = relatorioProposta.Finalizado || !isDraft;
            relatorioProposta.Conteudo = conteudo;

            if (!isDraft && (hasChanges || relatorioProposta.FileId == null))
            {
                var file = SaveRelatorioDiretoriaPdf(relatorioProposta);
                relatorioProposta.File = file;
                relatorioProposta.FileId = file.Id;
            }

            context.Update(relatorioProposta);
            context.SaveChanges();
        }

        public void SalvarNotaTecnica(int relatorioId, string conteudo, bool isDraft)
        {
            var relatorioProposta = _propostaNotaTecnica
                                        .Include(x => x.Proposta)
                                        .Where(x => x.Id == relatorioId)
                                        .FirstOrDefault();

            var hash = relatorioProposta.Conteudo?.ToSHA256() ?? "";
            var hasChanges = !hash.Equals(conteudo.ToSHA256());

            relatorioProposta.Finalizado = relatorioProposta.Finalizado || !isDraft;
            relatorioProposta.Conteudo = conteudo;

            if (!isDraft && (hasChanges || relatorioProposta.FileId == null))
            {
                var file = SaveNotaTecnicaPdf(relatorioProposta);
                relatorioProposta.File = file;
                relatorioProposta.FileId = file.Id;
            }

            context.Update(relatorioProposta);
            context.SaveChanges();
        }

        public PropostaNotaTecnica GetNotaTecnica(int captacaoId)
        {
            var proposta = GetNotaTecnicaPorCaptacao(captacaoId);

            if (proposta == null)
            {
                var captacao = _captacao
                                    .Include(c => c.RelatorioDiretoria)
                                    .Where(c => c.Id == captacaoId)
                                    .FirstOrDefault();
                CriarNotaTecnica(captacao);
                proposta = GetNotaTecnicaPorCaptacao(captacaoId);
            }

            return proposta;

        }

        public PropostaRelatorioDiretoria GetRelatorioDiretoria(int captacaoId)
        {
            var proposta = GetRelatorioDiretoriaPorCaptacao(captacaoId);

            if (proposta == null)
            {
                var captacao = _captacao
                                    .Include(c => c.RelatorioDiretoria)
                                    .Where(c => c.Id == captacaoId)
                                    .FirstOrDefault();
                CriarRelatorioDiretoria(captacao);
                proposta = GetRelatorioDiretoriaPorCaptacao(captacaoId);
            }

            return proposta;

        }

        public void CriarRelatorioDiretoria(Captacao captacao)
        {
            context.Add(new PropostaRelatorioDiretoria
            {
                ParentId = captacao.RelatorioDiretoriaId.Value,
                PropostaId = captacao.PropostaSelecionadaId.Value,
                Conteudo = "\n<!-- HEADER -->\n" + captacao.RelatorioDiretoria.Header +
                                "\n<!-- CONTEUDO -->\n" + captacao.RelatorioDiretoria.Conteudo +
                                "\n<!-- FOOTER -->\n" + captacao.RelatorioDiretoria.Footer
            });
            context.SaveChanges();
        }

        public void CriarNotaTecnica(Captacao captacao)
        {
            context.Add(new PropostaNotaTecnica
            {
                ParentId = captacao.RelatorioDiretoriaId.Value,
                PropostaId = captacao.PropostaSelecionadaId.Value,
                Conteudo = "\n<!-- HEADER -->\n" + captacao.RelatorioDiretoria.Header +
                                "\n<!-- CONTEUDO -->\n" + captacao.RelatorioDiretoria.Conteudo +
                                "\n<!-- FOOTER -->\n" + captacao.RelatorioDiretoria.Footer
            });
            context.SaveChanges();
        }

        public PropostaRelatorioDiretoria GetRelatorioDiretoriaPorCaptacao(int captacaoId)
        {
            return _propostaRelatorioDiretoria
                .Include(p => p.Proposta)
                .ThenInclude(p => p.Captacao)
                .Include(p => p.Parent)
                .Include(p => p.Proposta)
                .ThenInclude(p => p.Fornecedor)
                .Include(p => p.File)
                .Where(x => x.Proposta.Captacao.Id == captacaoId)
                .FirstOrDefault();
        }

        public PropostaNotaTecnica GetNotaTecnicaPorCaptacao(int captacaoId)
        {
            return _propostaNotaTecnica
                .Include(p => p.Proposta)
                .ThenInclude(p => p.Captacao)
                .Include(p => p.Parent)
                .Include(p => p.Proposta)
                .ThenInclude(p => p.Fornecedor)
                .Include(p => p.File)
                .Where(x => x.Proposta.Captacao.Id == captacaoId)
                .FirstOrDefault();
        }

        public PropostaContrato GetContrato(Guid guid)
        {
            var proposta = _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Contrato)
                .Include(p => p.Contrato).ThenInclude(c => c.File)
                .Include(p => p.Contrato)
                .ThenInclude(c => c.Parent)
                .FirstOrDefault(c => c.Guid == guid);


            if (proposta?.Captacao?.ContratoId != null)
                return proposta.Contrato ?? new PropostaContrato()
                {
                    PropostaId = proposta.Id,
                    Parent = proposta.Captacao.Contrato,
                    ParentId = (int)proposta.Captacao?.ContratoId
                };

            return null;
        }

        public PropostaContrato GetContratoFull(int propostaId)
        {
            var proposta = _captacaoPropostas
                .AsNoTracking()
                .Include(p => p.Fornecedor)
                .Include(p => p.Produtos)
                .ThenInclude(p => p.FaseCadeia)
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Contrato)
                .Include(p => p.Captacao)
                .ThenInclude(c => c.Tema)
                .Include(p => p.Contrato)
                .ThenInclude(c => c.Parent)
                .Include(p => p.Etapas)
                .ThenInclude(e => e.RecursosHumanosAlocacoes)
                .ThenInclude(r => r.Recurso)
                .Include(p => p.Etapas)
                .ThenInclude(e => e.RecursosMateriaisAlocacoes)
                .ThenInclude(r => r.Recurso)
                .FirstOrDefault(c => c.Id == propostaId);
            if (proposta?.Captacao?.ContratoId != null)
                return proposta.Contrato ?? new PropostaContrato()
                {
                    PropostaId = proposta.Id,
                    Parent = proposta.Captacao.Contrato,
                    ParentId = (int)proposta.Captacao?.ContratoId
                };
            return null;
        }

        public string PrintContrato(int propostaId)
        {
            var contrato = GetContratoFull(propostaId);
            var contratoDto = _mapper.Map<PropostaContratoDto>(contrato);
            return renderService.RenderToStringAsync("Proposta/Contrato", contratoDto).Result;
        }

        public string PrintRelatorioDiretoria(int propostaId)
        {
            var propostaFull = GetPropostaFull(propostaId);
            var relatorio = GetRelatorioDiretoria(propostaFull.CaptacaoId);
            var relatorioDto = _mapper.Map<PropostaRelatorioDiretoriaDto>(relatorio);
            relatorioDto.Titulo = "RELATÓRIO DIRETORIA";
            relatorioDto.Conteudo = RelatorioDiretoriaService.ReplaceShortcodes(relatorio.Conteudo, propostaFull);
            return renderService.RenderToStringAsync("Proposta/RelatorioDiretoria", relatorioDto).Result;
        }
        public string PrintNotaTecnica(int propostaId)
        {
            var propostaFull = GetPropostaFull(propostaId);
            var relatorio = GetNotaTecnica(propostaFull.CaptacaoId);
            var relatorioDto = _mapper.Map<PropostaNotaTecnicaDto>(relatorio);
            relatorioDto.Titulo = "NOTA TÉCNICA";
            relatorioDto.Conteudo = RelatorioDiretoriaService.ReplaceShortcodes(relatorio.Conteudo, propostaFull);
            return renderService.RenderToStringAsync("Proposta/NotaTecnica", relatorioDto).Result;
        }
        public List<PropostaContratoRevisao> GetContratoRevisoes(int propostaId)
        {
            return context.Set<PropostaContratoRevisao>()
                .Include(cr => cr.Parent)
                .ThenInclude(c => c.Parent)
                .Where(cr => cr.PropostaId == propostaId)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToList();
        }

        public PropostaContratoRevisao GetContratoRevisao(int propostaId, int id)
        {
            return context.Set<PropostaContratoRevisao>()
                .Include(cr => cr.Parent)
                .ThenInclude(c => c.Parent)
                .FirstOrDefault(cr => cr.PropostaId == propostaId && cr.Id == id);
        }

        public void UpdatePropostaDataAlteracao(int propostaId, DateTime time)
        {
            var proposta = GetProposta(propostaId);
            proposta.DataAlteracao = time;
            context.Update(proposta);
            context.SaveChanges();
        }

        public void UpdatePropostaDataAlteracao(int propostaId)
        {
            UpdatePropostaDataAlteracao(propostaId, DateTime.Now);
        }

        public async Task FinalizarProposta(int propostaId)
        {
            await FinalizarProposta(GetProposta(propostaId));
        }

        public async Task FinalizarProposta(Proposta proposta)
        {
            if (proposta.Participacao == StatusParticipacao.Aceito)
            {
                proposta.Finalizado = true;
                proposta.DataResposta = DateTime.Now;
                Put(proposta);
                await SendEmailFinalizado(proposta);
            }
        }

        public async Task FinalizarPropostasExpiradas(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;
            var propostas = GetPropostasEncerradasPendentes().ToList();
            if (propostas.Count() > 0)
            {
                _logger.LogInformation("Propostas expiradas: {Count}", propostas.Count());
                foreach (var proposta in propostas)
                {
                    proposta.Participacao = StatusParticipacao.Concluido;
                    Put(proposta);
                    await SendEmailCaptacaoEncerrada(proposta);
                }
            }
        }

        public async Task ConcluirRefinamento(Proposta proposta)
        {
            if (proposta.PlanoTrabalhoAprovacao == StatusAprovacao.Aprovado &&
                proposta.ContratoAprovacao == StatusAprovacao.Aprovado)
            {
                var captacao = context.Set<Captacao>().AsQueryable().First(c => c.Id == proposta.CaptacaoId);
                captacao.Status = Captacao.CaptacaoStatus.AnaliseRisco;
                context.Update(captacao);
                context.SaveChanges();
                await SendEmailRefinamentoConcluido(proposta);
                await SendEmailNovaIdentificacaoRisco(proposta);
            }
        }

        #region Relatórios

        public List<AlocacaoInfo> GetAlocacoes(int propostaId)
        {
            return context.Set<AlocacaoInfo>().Where(a => a.PropostaId == propostaId).ToList();
        }

        public Relatorio UpdateRelatorio(int propostaId)
        {
            var proposta = GetPropostaFull(propostaId);

            var alocacoes = GetAlocacoes(propostaId);
            var modelView = _mapper.Map<Core.Models.Relatorios.Fornecedores.Proposta>(proposta);
            modelView.Etapas.ForEach(e => { e.Alocacoes = alocacoes.Where(a => a.EtapaId == e.Id).ToList(); });
            proposta = context.Set<Proposta>().FirstOrDefault(p => p.Id == propostaId);

            var validacao = new PropostaValidator().Validate(modelView);
            var content = renderService.RenderToStringAsync("Proposta/Proposta", modelView).Result;
            var relatorio = context.Set<Relatorio>().Where(r => r.PropostaId == propostaId).FirstOrDefault() ??
                            new Relatorio()
                            {
                                Content = content,
                                DataAlteracao = DateTime.Now,
                                PropostaId = propostaId,
                                Validacao = validacao
                            };


            if (relatorio.Id == 0)
            {
                context.Add(relatorio);
                context.SaveChanges();
            }
            else
            {
                relatorio.Content = content;
                relatorio.DataAlteracao = DateTime.Now;
                relatorio.Validacao = validacao;
                context.Update(relatorio);
                context.SaveChanges();
            }

            var file = SaveRelatorioPdf(relatorio);
            relatorio.File = file;
            relatorio.FileId = file.Id;
            proposta.RelatorioId = relatorio.Id;
            context.Update(proposta);
            context.SaveChanges();
            return relatorio;
        }

        public Relatorio GetRelatorio(int propostaId)
        {
            var proposta = _captacaoPropostas.Include(p => p.Relatorio)
                .ThenInclude(r => r.File)
                .FirstOrDefault(p => p.Id == propostaId);
            if (proposta != null)
            {
                if (proposta.Relatorio == null || proposta.Relatorio.DataAlteracao < proposta.DataAlteracao)
                {
                    return UpdateRelatorio(propostaId);
                }

                return proposta.Relatorio;
            }

            return null;
        }

        public FileUpload SaveRelatorioPdf(Relatorio relatorio)
        {
            try
            {
                if (relatorio != null)
                {
                    var arquivo = _pdfService.HtmlToPdf(relatorio.Content,
                        $"relatorio-{relatorio.PropostaId}-{relatorio.DataAlteracao}");
                    PdfService.AddPagesToPdf(arquivo.Path, 500, 820);
                    return arquivo;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Erro ao salvar relatório: {Error}", e.Message);
            }

            return null;
        }

        public FileUpload GetRelatorioPdf(int propostaId)
        {
            var proposta = _captacaoPropostas.AsQueryable().Include(p => p.Relatorio)
                .ThenInclude(r => r.File)
                .FirstOrDefault(p => p.Id == propostaId);
            if (proposta != null && proposta?.Relatorio.File != null)
            {
                return proposta.Relatorio.File;
            }

            return null;
        }

        public FileUpload SaveContratoPdf(PropostaContrato contrato)
        {
            var contratoContent = PrintContrato(contrato.PropostaId);
            if (contratoContent != null)
            {
                var arquivo = _pdfService.HtmlToPdf(contratoContent, $"contrato-{contrato.PropostaId}");
                PdfService.AddPagesToPdf(arquivo.Path, 475, 90);
                return arquivo;
            }

            return null;
        }

        public FileUpload SaveRelatorioDiretoriaPdf(PropostaRelatorioDiretoria relatorio)
        {
            var relatorioContent = PrintRelatorioDiretoria(relatorio.PropostaId);
            if (relatorioContent != null)
            {
                var arquivo = _pdfService.HtmlToPdf(relatorioContent, $"relatorio-{relatorio.PropostaId}");
                PdfService.AddPagesToPdf(arquivo.Path, 475, 90);
                return arquivo;
            }

            return null;
        }

        public FileUpload SaveNotaTecnicaPdf(PropostaNotaTecnica relatorio)
        {
            var relatorioContent = PrintNotaTecnica(relatorio.PropostaId);
            if (relatorioContent != null)
            {
                var arquivo = _pdfService.HtmlToPdf(relatorioContent, $"nota-tecnica-{relatorio.PropostaId}");
                PdfService.AddPagesToPdf(arquivo.Path, 475, 90);
                return arquivo;
            }

            return null;
        }

        public FileUpload GetContratoPdf(int propostaId)
        {
            var contrato = GetContrato(propostaId);
            if (contrato?.File != null)
            {
                return contrato.File;
            }

            var contratoContent = PrintContrato(propostaId);
            if (contratoContent != null)
            {
                var arquivo = _pdfService.HtmlToPdf(contratoContent, $"contrato-{contrato.PropostaId}");
                PdfService.AddPagesToPdf(arquivo.Path, 475, 90);
                return arquivo;
            }

            return null;
        }

        public FileUpload GetRelatorioDiretoriaPdf(int captacaoId)
        {
            var relatorio = GetRelatorioDiretoria(captacaoId);
            if (relatorio?.File != null)
            {
                return relatorio.File;
            }

            var relatorioContent = PrintRelatorioDiretoria(relatorio.PropostaId);
            if (relatorioContent != null)
            {
                var arquivo = _pdfService.HtmlToPdf(relatorioContent, $"relatorio-{relatorio.PropostaId}");
                PdfService.AddPagesToPdf(arquivo.Path, 475, 90);
                return arquivo;
            }

            return null;
        }

        public FileUpload GetNotaTecnicaPdf(int captacaoId)
        {
            var relatorio = GetNotaTecnica(captacaoId);
            if (relatorio?.File != null)
            {
                return relatorio.File;
            }

            var relatorioContent = PrintNotaTecnica(relatorio.PropostaId);
            if (relatorioContent != null)
            {
                var arquivo = _pdfService.HtmlToPdf(relatorioContent, $"nota-tecnica-{relatorio.PropostaId}");
                PdfService.AddPagesToPdf(arquivo.Path, 475, 90);
                return arquivo;
            }

            return null;
        }

        #endregion

        #region Emails

        public async Task SendEmailFinalizado(Proposta proposta)
        {
            var pf = _mapper.Map<PropostaFinalizada>(proposta);
            var suprimentoUsers = _userService.GetInRole("Suprimento").Select(u => u.Email).ToArray();
            var subject = pf.Cancelada
                ? $"O fornecedor “{pf.Fornecedor}” cancelou sua participação no projeto"
                : $"O fornecedor “{pf.Fornecedor}” finalizou com sucesso a sua participação no projeto";
            await _sendGridService.Send(suprimentoUsers,
                subject,
                "Email/Captacao/Propostas/PropostaFinalizada", pf);
        }

        public async Task SendEmailCaptacaoEncerrada(Proposta proposta)
        {
            var pf = _mapper.Map<PropostaFinalizada>(proposta);

            var subject = pf.Finalizado
                ? $"Sua participação no projeto “{pf.Projeto}” foi concluída com sucesso."
                : $"Os itens do projeto “{pf.Projeto}” não foram enviados até a data máxima.";
            await _sendGridService.Send(proposta.Fornecedor.Responsavel.Email,
                subject,
                "Email/Captacao/Propostas/PropostaEncerrada", pf);
        }

        public async Task SendEmailNovoPlano(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Fornecedor)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var plano = new NovoPlanoTrabalho()
            {
                Captacao = captacao,
                Fornecedor = captacao.PropostaSelecionada.Fornecedor.Nome,
                PropostaGuid = captacao.PropostaSelecionada.Guid
            };
            try
            {
                await _sendGridService.Send(captacao.UsuarioRefinamento.Email,
                    $"Uma nova versão do Plano de trabalho do projeto {captacao.Titulo} foi enviada",
                    "Email/Captacao/Propostas/NovoPlanoTrabalho", plano);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task SendEmailNovoContrato(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Fornecedor)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var contrato = new NovoContrato()
            {
                Captacao = captacao,
                Fornecedor = captacao.PropostaSelecionada.Fornecedor.Nome,
                PropostaGuid = captacao.PropostaSelecionada.Guid
            };
            try
            {
                await _sendGridService.Send(captacao.UsuarioRefinamento.Email,
                    $"Uma nova versão do Contrato do projeto {captacao.Titulo} foi enviada",
                    "Email/Captacao/Propostas/NovoContrato", contrato);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task SendEmailContratoAlteracao(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Fornecedor)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(f => f.Responsavel)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var contrato = new ContratoRevisor()
            {
                Captacao = captacao,
                Fornecedor = captacao.PropostaSelecionada.Fornecedor.Nome,
                PropostaGuid = captacao.PropostaSelecionada.Guid
            };
            try
            {
                await _sendGridService.Send(captacao.PropostaSelecionada.Responsavel.Email,
                    $"Novo comentário sobre o contrato do projeto \"{captacao.Titulo}\"",
                    "Email/Captacao/Propostas/ContratoRevisor", contrato);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task SendEmailPlanoTrabalhoAlteracao(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Responsavel)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var plano = new PlanoTrabalhoRevisor()
            {
                Captacao = captacao,
                PropostaGuid = captacao.PropostaSelecionada.Guid
            };
            await _sendGridService.Send(captacao.PropostaSelecionada.Responsavel.Email,
                $"Novo comentário sobre o Plano de Trabalho do projeto \"{captacao.Titulo}\"",
                "Email/Captacao/Propostas/PlanoTrabalhoRevisor", plano);
        }

        public async Task SendEmailRefinamentoConcluido(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(f => f.Responsavel)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var message = new RefinamentoConcluido()
            {
                Captacao = captacao,
                PropostaGuid = captacao.PropostaSelecionada.Guid
            };
            try
            {
                await _sendGridService.Send(captacao.PropostaSelecionada.Responsavel.Email,
                    $"Parabéns! A revisão do projeto \"{captacao.Titulo}\" foi concluída",
                    "Email/Captacao/Propostas/RefinamentoConcluido", message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task SendEmailNovaIdentificacaoRisco(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var message = new IdentificacaoRiscos()
            {
                Captacao = captacao
            };
            await _sendGridService.Send(captacao.UsuarioRefinamento.Email,
                "Existe um novo projeto preparado para Identificação de Riscos",
                "Email/Captacao/Propostas/IdentificacaoRiscos", message);
        }

        public async Task SendEmailRefinamentoCancelado(Proposta proposta)
        {
            var captacao = context.Set<Captacao>().AsNoTracking()
                .Include(c => c.UsuarioRefinamento)
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Responsavel)
                .FirstOrDefault(c => c.Id == proposta.CaptacaoId) ?? throw new NullReferenceException();
            var message = new RefinamentoCancelado()
            {
                Captacao = captacao,
                PropostaGuid = captacao.PropostaSelecionada.Guid
            };
            try
            {
                await _sendGridService.Send(captacao.PropostaSelecionada.Responsavel.Email,
                    $"A revisão do projeto \"{captacao.Titulo}\" foi cancelada",
                    "Email/Captacao/Propostas/RefinamentoCancelado", message);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion
    }
}