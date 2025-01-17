using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeD.Core.Exceptions.Captacoes;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Fornecedores;
using PeD.Core.Models.Propostas;
using PeD.Data;
using PeD.Services.Demandas;
using PeD.Views.Email.Captacao;
using PeD.Views.Email.Captacao.Propostas;
using TaesaCore.Interfaces;
using TaesaCore.Services;
using Empresa = PeD.Core.Models.Propostas.Empresa;
using Funcao = PeD.Core.Models.Propostas.Funcao;

namespace PeD.Services.Captacoes
{
    public class CaptacaoService : BaseService<Captacao>
    {
        private ILogger<CaptacaoService> _logger;
        private SendGridService _sendGridService;
        private GestorDbContext _context;
        private DemandaService _demandaService;
        private DbSet<CaptacaoArquivo> _captacaoArquivos;
        private DbSet<CaptacaoFornecedor> _captacaoFornecedors;
        private DbSet<Proposta> _captacaoPropostas;

        private Func<IQueryable<Captacao>, IQueryable<Captacao>> _queryCanceladas = q =>
        {
            var maxDate = DateTime.Today.Subtract(TimeSpan.FromDays(1));
            return q
                .Include(c => c.Criador)
                .Include(c => c.UsuarioSuprimento)
                .Include(c => c.Propostas)
                .Where(c => c.Status == Captacao.CaptacaoStatus.Cancelada ||
                            (c.Status == Captacao.CaptacaoStatus.Fornecedor && c.Termino <= maxDate || c.Status >= Captacao.CaptacaoStatus.Encerrada) &&
                            (c.Propostas.Count == 0 || c.Propostas.All(p => !p.Finalizado || !p.Contrato.Finalizado))
                        );

        };

        private Func<IQueryable<Captacao>, IQueryable<Captacao>> _queryEncerradas = q =>
        {
            var maxDate = DateTime.Today.Subtract(TimeSpan.FromDays(1));
            return q
                .Include(c => c.Criador)
                .Include(c => c.UsuarioSuprimento)
                .Include(c => c.Propostas)
                .ThenInclude(p => p.Fornecedor)
                .Include(c => c.Propostas)
                .ThenInclude(p => p.Contrato)
                .Where(c => (c.Status >= Captacao.CaptacaoStatus.Encerrada || c.Termino <= maxDate)
                            && c.Propostas.Any(p => p.Finalizado && p.Contrato.Finalizado)
                );
        };

        private Func<IQueryable<Captacao>, IQueryable<Captacao>> _queryAbertas = q =>
        {
            var maxDate = DateTime.Now.Subtract(TimeSpan.FromDays(1));
            return q.Where(c => c.Status == Captacao.CaptacaoStatus.Fornecedor && c.Termino > maxDate);
        };

        public CaptacaoService(IRepository<Captacao> repository, GestorDbContext context,
            SendGridService sendGridService, ILogger<CaptacaoService> logger, DemandaService demandaService, PropostaService propostaService,
            IMapper mapper) : base(repository)
        {
            _context = context;
            _sendGridService = sendGridService;
            _logger = logger;
            _captacaoArquivos = context.Set<CaptacaoArquivo>();
            _captacaoFornecedors = context.Set<CaptacaoFornecedor>();
            _captacaoPropostas = context.Set<Proposta>();
            _demandaService = demandaService;
        }

        protected void ThrowIfNotExist(int id)
        {
            if (!Exist(id))
            {
                throw new Exception("Captação não encontrada");
            }
        }

        public string UserSuprimento(int id) => _context.Set<Captacao>().Where(c => c.Id == id)
            .Select(c => c.UsuarioSuprimentoId).FirstOrDefault();

        public Dictionary<Captacao.CaptacaoStatus, int> GetCountByStatus()
        {
            var query = _context.Set<Captacao>().AsQueryable();
            var d = new Dictionary<Captacao.CaptacaoStatus, int>();
            d.Add(Captacao.CaptacaoStatus.Pendente,
                query.Where(c => c.Status == Captacao.CaptacaoStatus.Pendente).Count());
            d.Add(Captacao.CaptacaoStatus.Elaboracao,
                query.Where(c => c.Status == Captacao.CaptacaoStatus.Elaboracao).Count());
            d.Add(Captacao.CaptacaoStatus.Cancelada, _queryCanceladas(query).Count());
            d.Add(Captacao.CaptacaoStatus.Encerrada, _queryEncerradas(query).Count());
            d.Add(Captacao.CaptacaoStatus.Fornecedor, _queryAbertas(query).Count());
            return d;
        }

        public List<Captacao> GetCaptacoes(Captacao.CaptacaoStatus status)
        {
            return Filter(q => q
                    .Include(c => c.Criador)
                    .Include(c => c.UsuarioSuprimento)
                    .Include(c => c.Propostas)
                    .ThenInclude(p => p.Fornecedor)
                    .Where(c => c.Status == status))
                .ToList();
        }

        public List<Captacao> GetCaptacoesFalhas()
        {
            return Filter(_queryCanceladas).ToList();
        }

        public List<Captacao> GetCaptacoesEncerradas()
        {
            return Filter(_queryEncerradas).ToList();
        }

        public List<Captacao> GetCaptacoesSelecaoPendente()
        {
            var captacoes = _context.Set<Captacao>().AsQueryable();
            var propostas = _context.Set<Proposta>().AsQueryable();
            var contratos = _context.Set<PropostaContrato>().AsQueryable();
            var pendentes =
                from captacao in captacoes
                join proposta in propostas on captacao.Id equals proposta.CaptacaoId
                join contrato in contratos on proposta.Id equals contrato.PropostaId
                where captacao.Status == Captacao.CaptacaoStatus.Encerrada
                        && captacao.PropostaSelecionadaId == null
                        && proposta.Finalizado
                        && contrato.Finalizado
                select captacao;
            return pendentes.Include(c => c.Propostas)
                .ThenInclude(p => p.Contrato)
                .Distinct()
                .ToList();
        }

        public List<Captacao> GetCaptacoesSelecaoFinalizada()
        {
            return Filter(q => q
                    .Include(c => c.Propostas)
                    .ThenInclude(p => p.Fornecedor)
                    .Include(c => c.UsuarioRefinamento)
                    .Include(c => c.ArquivoComprobatorio)
                    .Where(c => c.Status >= Captacao.CaptacaoStatus.Encerrada && c.PropostaSelecionadaId != null))
                .ToList();
        }

        public List<Captacao> GetCaptacoesPorSuprimento(string userId)
        {
            var captacoesQuery =
                from captacao in _context.Set<Captacao>().AsQueryable()
                where
                    captacao.UsuarioSuprimentoId == userId &&
                    (
                        captacao.Status == Captacao.CaptacaoStatus.Elaboracao ||
                        captacao.Status == Captacao.CaptacaoStatus.Fornecedor ||
                        captacao.Status == Captacao.CaptacaoStatus.Encerrada
                    )
                select captacao;


            return captacoesQuery
                .Include(c => c.UsuarioSuprimento)
                .ToList();
        }

        public List<Captacao> GetCaptacoesPorSuprimento(string userId, Captacao.CaptacaoStatus status)
        {
            var captacoesQuery =
                from captacao in _context.Set<Captacao>().AsQueryable()
                where
                    captacao.UsuarioSuprimentoId == userId && captacao.Status == status
                select captacao;

            return captacoesQuery
                .Include(c => c.UsuarioSuprimento)
                .ToList();
        }

        public List<Captacao> GetCaptacoesPorSuprimentoCanceladas(string userId)
        {
            var maxDate = DateTime.Today.Subtract(TimeSpan.FromDays(1));
            return Filter(q => q
                    .Include(c => c.Criador)
                    .Include(c => c.UsuarioSuprimento)
                    .Include(c => c.Propostas)
                    .Where(c => c.UsuarioSuprimentoId == userId &&
                                (c.Status == Captacao.CaptacaoStatus.Cancelada ||
                                 (c.Status == Captacao.CaptacaoStatus.Fornecedor &&
                                  c.Termino <= maxDate ||
                                  c.Status >= Captacao.CaptacaoStatus.Encerrada) &&
                                 (c.Propostas.Count == 0 ||
                                  c.Propostas.All(p =>
                                      !p.Finalizado || !p.Contrato.Finalizado)))))
                .ToList();
        }

        public List<Captacao> GetCaptacoesPorSuprimentoFinalizada(string userId)
        {
            var maxDate = DateTime.Today.Subtract(TimeSpan.FromDays(1));
            return Filter(q => q
                .Include(c => c.Criador)
                .Include(c => c.UsuarioSuprimento)
                .Include(c => c.Propostas)
                .ThenInclude(p => p.Fornecedor)
                .Include(c => c.Propostas)
                .ThenInclude(p => p.Contrato)
                .Where(c => c.UsuarioSuprimentoId == userId && (c.Status >= Captacao.CaptacaoStatus.Encerrada ||
                                                                c.Termino <= maxDate)
                                                            && c.Propostas.Any(p =>
                                                                p.Finalizado && p.Contrato.Finalizado)
                )).ToList();
        }

        public List<Captacao> GetCaptacoesPorSuprimentoAberta(string userId)
        {
            var maxDate = DateTime.Today.Subtract(TimeSpan.FromDays(1));
            return Filter(q => q
                .Include(c => c.UsuarioSuprimento)
                .Where(c =>
                    userId == c.UsuarioSuprimentoId && c.Status == Captacao.CaptacaoStatus.Fornecedor &&
                    c.Termino > maxDate)).ToList();
        }

        public async Task ConfigurarCaptacao(int id, DateTime termino, string consideracoes,
            IEnumerable<int> arquivosIds, IEnumerable<int> fornecedoresIds, int contratoId)
        {
            ThrowIfNotExist(id);
            if (termino < DateTime.Today)
            {
                throw new CaptacaoException("A data máxima não pode ser anterior a data de hoje");
            }

            var captacao = Get(id);
            var jaPossuiFornecedor = _captacaoFornecedors.Any(cf => cf.CaptacaoId == id);
            var jaTerminou = captacao.Termino != null;

            if (jaPossuiFornecedor || jaTerminou)
            {
                throw new CaptacaoException("Captação já foi configurada");
            }

            captacao.Termino = termino;
            captacao.Consideracoes = consideracoes;
            captacao.ContratoId = contratoId;

            #region Arquivos

            var arquivos = _captacaoArquivos.Where(ca => ca.CaptacaoId == id).ToList();
            arquivos.ForEach(arquivo => arquivo.AcessoFornecedor = arquivosIds.Contains(arquivo.Id));
            _captacaoArquivos.UpdateRange(arquivos);

            #endregion

            #region Fornecedores

            var fornecedores = fornecedoresIds.Select(fid => new CaptacaoFornecedor
            { CaptacaoId = id, FornecedorId = fid });

            var captacaoFornecedors = _captacaoFornecedors.Where(f => f.CaptacaoId == id).ToList();
            _captacaoFornecedors.RemoveRange(captacaoFornecedors);
            _captacaoFornecedors.AddRange(fornecedores);

            #endregion

            await _context.SaveChangesAsync();
        }


        public async Task EnviarParaFornecedores(int id)
        {
            ThrowIfNotExist(id);

            var captacao = Get(id);
            captacao.Status = Captacao.CaptacaoStatus.Fornecedor;
            var fornecedores = _captacaoFornecedors
                .Include(cf => cf.Fornecedor)
                .ThenInclude(f => f.Responsavel)
                .Where(cf => cf.CaptacaoId == id)
                .Select(cf => cf.Fornecedor).ToList();
            var norteEnergia = _context.Empresas.FirstOrDefault(e => e.Nome.ToUpper() == "NORTE ENERGIA") ??
                        throw new Exception("Empresa NORTE ENERGIA não encontrada!");
            var contratoId = captacao.ContratoId.HasValue
                ? captacao.ContratoId.Value
                : throw new Exception("Contrato não definido");

            fornecedores.ForEach(f =>
                {
                    var proposta = new Proposta()
                    {
                        FornecedorId = f.Id,
                        Fornecedor = f,
                        ResponsavelId = f.ResponsavelId,
                        CaptacaoId = id,
                        Contrato = new PropostaContrato()
                        {
                            ParentId = contratoId
                        },
                        Participacao = StatusParticipacao.Pendente,
                        DataCriacao = DateTime.Now,
                        Empresas = new List<Empresa>()
                        {
                            new Empresa()
                            {
                                Required = true,
                                Funcao = Funcao.Executora,
                                EmpresaRefId = f.Id,
                                RazaoSocial = f.Nome,
                                CNPJ = f.Cnpj,
                                UF = f.UF
                            }
                        }
                    };
                    _context.Add(proposta);
                    _context.SaveChanges();
                    _context.Add(new Empresa()
                    {
                        Required = true,
                        PropostaId = proposta.Id,
                        Funcao = Funcao.Cooperada,
                        EmpresaRefId = norteEnergia.Id,
                        Codigo = norteEnergia.Codigo,
                        RazaoSocial = norteEnergia.Nome,
                        CNPJ = norteEnergia.Cnpj
                    });
                    _context.SaveChanges();
                }
            );
            _context.Update(captacao);
            await _context.SaveChangesAsync();

            await SendEmailConvite(captacao);
        }

        public async Task EstenderCaptacao(int id, DateTime termino)
        {
            ThrowIfNotExist(id);
            var captacao = Get(id);
            if (termino < DateTime.Today || termino < captacao.Termino)
            {
                throw new CaptacaoException("A data máxima não pode ser anterior a data previamente escolhida");
            }

            captacao.Termino = termino;
            Put(captacao);

            try
            {
                await SendEmailAtualizacao(captacao);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task CancelarCaptacao(int id)
        {
            ThrowIfNotExist(id);
            var captacao = Get(id);
            captacao.Status = Captacao.CaptacaoStatus.Cancelada;
            captacao.Cancelamento = DateTime.Now;
            Put(captacao);

            var fornecedores = _captacaoFornecedors
                .Include(cf => cf.Fornecedor)
                .ThenInclude(f => f.Responsavel)
                .Where(cf => cf.CaptacaoId == id)
                .Select(cf => cf.Fornecedor);
            await SendEmailCancelamento(captacao, fornecedores.ToList());
        }

        public Proposta GetProposta(int id)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .Include(p => p.Contrato)
                .FirstOrDefault(p => p.Id == id);
        }

        public IEnumerable<Proposta> GetPropostasPorCaptacao(int id)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Where(cp => cp.CaptacaoId == id)
                .ToList();
        }

        public IEnumerable<Proposta> GetPropostasPorCaptacao(int id, StatusParticipacao status)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Where(cp => cp.CaptacaoId == id && cp.Participacao == status)
                .ToList();
        }

        public IEnumerable<Proposta> GetPropostasEmAberto(int captacaoId)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Contrato)
                .Include(p => p.Captacao)
                .Where(cp => cp.CaptacaoId == captacaoId && cp.Participacao != StatusParticipacao.Rejeitado &&
                             (!cp.Finalizado || cp.Contrato != null && !cp.Contrato.Finalizado)
                )
                .ToList();
        }

        public IEnumerable<Proposta> GetPropostasRecebidas(int captacaoId)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Contrato)
                .Include(p => p.Captacao)
                .Where(cp => cp.CaptacaoId == captacaoId &&
                             (cp.Participacao == StatusParticipacao.Aceito ||
                              cp.Participacao == StatusParticipacao.Concluido) &&
                             cp.Finalizado && cp.Contrato != null &&
                             cp.Contrato.Finalizado
                )
                .ToList();
        }

        public IEnumerable<Proposta> GetPropostasPorResponsavel(string userId)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .Include(p => p.Captacao)
                .Where(cp =>
                    cp.ResponsavelId == userId &&
                    cp.Captacao.Status == Captacao.CaptacaoStatus.Fornecedor &&
                    cp.Participacao != StatusParticipacao.Rejeitado)
                .ToList();
        }

        public void EncerrarCaptacoesExpiradas()
        {

            var expiradas = Filter(q =>
                    q.Include(c => c.Propostas)
                        .ThenInclude(p => p.Contrato)
                        .Where(c => c.Status == Captacao.CaptacaoStatus.Fornecedor && c.Termino < DateTime.Now))
                .ToList();
            if (expiradas.Count() == 0)
                return;
            foreach (var expirada in expiradas)
            {
                var isOk = expirada.Propostas.Count > 0 &&
                           expirada.Propostas.Any(p => p.Finalizado && p.Contrato.Finalizado);
                expirada.Status = isOk ? Captacao.CaptacaoStatus.Encerrada : Captacao.CaptacaoStatus.Cancelada;
                if (expirada.Status == Captacao.CaptacaoStatus.Cancelada)
                {
                    expirada.Cancelamento = DateTime.Now;
                }

                // Notificando analistas responsáveis
                _demandaService.NotificarAnalistaPed(expirada.Demanda, true);
                _demandaService.NotificarAnalistaTecnico(expirada.Demanda, true);

            }

            Put(expiradas);

            _logger.LogInformation("Captações expiradas: {Count}", expiradas.Count);
        }

        public IEnumerable<Proposta> GetPropostasRefinamento(string userId, bool asFornecedor = false)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .Where(proposta =>
                    proposta.Id == proposta.Captacao.PropostaSelecionadaId &&
                    proposta.Participacao != StatusParticipacao.Cancelado &&
                    proposta.Captacao.Status != Captacao.CaptacaoStatus.Cancelada &&
                    (
                        string.IsNullOrEmpty(userId) ||
                        asFornecedor && proposta.ResponsavelId == userId ||
                        proposta.Captacao.UsuarioRefinamentoId == userId
                    )
                    && proposta.Captacao.Status == Captacao.CaptacaoStatus.Refinamento &&
                    (proposta.ContratoAprovacao != StatusAprovacao.Aprovado ||
                     proposta.PlanoTrabalhoAprovacao != StatusAprovacao.Aprovado)
                )
                .ToList();
        }

        #region 2.5

        public List<Captacao> GetIdentificaoRiscoPendente(string userId = null)
        {
            var captacoes = _context.Set<Captacao>().AsQueryable();
            var pendentes =
                from captacao in captacoes
                where captacao.Status == Captacao.CaptacaoStatus.AnaliseRisco
                      && (userId == null || captacao.UsuarioRefinamentoId == userId)
                      && captacao.PropostaSelecionadaId != null
                      && (captacao.UsuarioAprovacaoId == null || captacao.ArquivoRiscosId == null)
                select captacao;

            return pendentes
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Fornecedor)
                .Include(c => c.UsuarioRefinamento)
                .ToList();
        }

        public List<Captacao> GetIdentificaoRiscoFinalizada(string userId = null)
        {
            var captacoes = _context.Set<Captacao>().AsQueryable();
            var finalizados =
                from captacao in captacoes
                where (captacao.Status == Captacao.CaptacaoStatus.AnaliseRisco ||
                       captacao.Status == Captacao.CaptacaoStatus.Formalizacao)
                      && (userId == null || captacao.UsuarioRefinamentoId == userId)
                      && captacao.PropostaSelecionadaId != null
                      && (captacao.UsuarioAprovacaoId != null || captacao.ArquivoRiscosId != null)
                select captacao;

            return finalizados
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Fornecedor)
                .Include(c => c.UsuarioAprovacao)
                .ToList();
        }

        #endregion

        #region 2.6

        public List<Captacao> GetFormalizacao(bool? formalizacao, string userId = null)
        {
            var captacoes = _context.Set<Captacao>().AsQueryable();
            var captacoesQuery =
                from captacao in captacoes
                where captacao.Status == Captacao.CaptacaoStatus.Formalizacao
                      && (userId == null || captacao.UsuarioAprovacaoId == userId)
                      && captacao.PropostaSelecionadaId != null
                      && captacao.IsProjetoAprovado == formalizacao
                select captacao;

            return captacoesQuery
                .Include(c => c.PropostaSelecionada)
                .ThenInclude(p => p.Fornecedor)
                .Include(c => c.UsuarioAprovacao)
                .Include(c => c.UsuarioExecucao)
                .Include(c => c.ArquivoFormalizacao)
                .ToList();
        }

        #endregion


        #region Emails

        public async Task SendEmailSuprimento(Captacao captacao, string autor)
        {
            var nova = new NovaCaptacao()
            {
                Autor = autor,
                CaptacaoId = captacao.Id,
                CaptacaoTitulo = captacao.Titulo
            };
            var email = _context.Users.AsQueryable()
                .Where(u => u.Role == "Suprimento" && u.Id == captacao.UsuarioSuprimentoId)
                .Select(u => u.Email)
                .FirstOrDefault();
            await _sendGridService.Send(email, "Novo Projeto para Captação de Proposta no Mercado cadastrado",
                "Email/Captacao/NovaCaptacao", nova);
        }

        public async Task SendEmailConvite(Captacao captacao)
        {
            var propostas = _context.Set<Proposta>().AsQueryable().AsNoTracking()
                .Include(p => p.Fornecedor)
                .Include(p => p.Responsavel)
                .Where(p => p.CaptacaoId == captacao.Id);
            foreach (var proposta in propostas)
            {
                var convite = new ConviteFornecedor()
                {
                    Fornecedor = proposta.Fornecedor.Nome,
                    Projeto = captacao.Titulo,
                    PropostaGuid = proposta.Guid
                };

                if (proposta.Responsavel == null ||
                    string.IsNullOrWhiteSpace(proposta.Responsavel.Email))
                {
                    _logger.LogWarning("Fornecedor como responsável|email vazio, não será possível enviar o convite");
                }
                else
                {
                    await _sendGridService
                        .Send(proposta.Responsavel.Email,
                            "Você foi convidado para participar de um novo projeto para a área de PDI da Norte Energia",
                            "Email/Captacao/ConviteFornecedor",
                            convite);
                }
            }
        }

        public async Task SendEmailAtualizacao(Captacao captacao)
        {
            var propostas = _context.Set<Captacao>().AsQueryable()
                .Include(c => c.Propostas)
                .ThenInclude(p => p.Responsavel)
                .First(c => c.Id == captacao.Id).Propostas;


            if (captacao.Termino != null)
            {
                foreach (var proposta in propostas)
                {
                    if (proposta.Responsavel == null)
                        continue;
                    var cancelamento = new AlteracaoPrazo()
                    {
                        Projeto = captacao.Titulo,
                        Prazo = captacao.Termino.Value,
                        PropostaGuid = proposta.Guid
                    };
                    await _sendGridService
                        .Send(proposta.Responsavel.Email,
                            $"A equipe de Suprimentos alterou a data máxima de envio de propostas para o projeto \"{captacao.Titulo}\".",
                            "Email/Captacao/AlteracaoPrazo",
                            cancelamento);
                }
            }
        }

        public async Task SendEmailCancelamento(Captacao captacao, List<Fornecedor> fornecedores)
        {
            var emails = fornecedores.Select(f => f.Responsavel.Email).ToArray();
            var cancelamento = new CancelamentoCaptacao()
            {
                Projeto = captacao.Titulo
            };
            await _sendGridService.Send(emails,
                $"A equipe de Suprimentos cancelou o processo de captação de propostas do projeto \"{captacao.Titulo}\".",
                "Email/Captacao/CancelamentoCaptacao",
                cancelamento);
        }

        public async Task SendEmailSelecao(Captacao captacao)
        {
            var emailRevisor = _context.Users.AsQueryable()
                .Where(u => u.Id == captacao.UsuarioRefinamentoId)
                .Select(u => u.Email)
                .FirstOrDefault();
            var proposta = _context.Set<Proposta>()
                .Include(p => p.Fornecedor)
                .Include(f => f.Responsavel)
                .FirstOrDefault(p => p.Id == captacao.PropostaSelecionadaId) ?? throw new NullReferenceException();
            var fornecedor = proposta.Fornecedor.Nome;


            var convite = new RevisorConvite()
            {
                Captacao = captacao,
                Fornecedor = fornecedor,
                PropostaGuid = proposta.Guid,
                DataAlvo = captacao.DataAlvo ?? throw new NullReferenceException()
            };
            var propostaSelecionada = new PropostaSelecionada()
            {
                Captacao = captacao,
                DataAlvo = convite.DataAlvo,
                PropostaGuid = proposta.Guid
            };

            await _sendGridService.Send(emailRevisor,
                "Você foi convidado a participar da Etapa de Refinamento da Proposta",
                "Email/Captacao/Propostas/RevisorConvite", convite);

            await _sendGridService.Send(proposta.Responsavel.Email,
                "Parabéns, sua proposta foi aprovada na Etapa de Priorização e Seleção",
                "Email/Captacao/Propostas/PropostaSelecionada", propostaSelecionada);
        }

        public async Task SendEmailFormalizacaoPendente(Captacao captacao)
        {
            var email = _context.Users.AsQueryable()
                .Where(u => u.Id == captacao.UsuarioAprovacaoId)
                .Select(u => u.Email)
                .FirstOrDefault() ?? throw new NullReferenceException();

            var formalizacao = new Formalizacao()
            {
                Captacao = captacao
            };
            await _sendGridService.Send(email,
                "Existe um novo projeto preparado para Aprovação e Formalização",
                "Email/Captacao/Formalizacao", formalizacao);
        }

        #endregion

        public string VerificarRelatorioDiretoria(int captacaoId)
        {

            var captacao = _context.Set<Captacao>().Find(captacaoId);
            var status = _context.Set<PropostaRelatorioDiretoria>()
                .Any(x => x.PropostaId == captacao.PropostaSelecionadaId && x.Finalizado)
                ? "Finalizado"
                : "Rascunho";

            return status;
        }
        public string VerificarNotaTecnica(int captacaoId)
        {

            var captacao = _context.Set<Captacao>().Find(captacaoId);
            var status = _context.Set<PropostaNotaTecnica>()
                .Any(x => x.PropostaId == captacao.PropostaSelecionadaId && x.Finalizado)
                ? "Finalizado"
                : "Rascunho";

            return status;
        }


    }
}