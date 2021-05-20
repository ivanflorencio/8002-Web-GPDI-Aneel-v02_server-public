using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel.CalcEngine.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeD.Core.Exceptions.Captacoes;
using PeD.Core.Models;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Fornecedores;
using PeD.Core.Models.Propostas;
using PeD.Data;
using PeD.Views.Email.Captacao;
using PeD.Views.Email.Captacao.Propostas;
using TaesaCore.Interfaces;
using TaesaCore.Services;

namespace PeD.Services.Captacoes
{
    public class CaptacaoService : BaseService<Captacao>
    {
        private SendGridService _sendGridService;
        private GestorDbContext _context;
        private DbSet<CaptacaoArquivo> _captacaoArquivos;
        private DbSet<CaptacaoFornecedor> _captacaoFornecedors;
        private DbSet<Proposta> _captacaoPropostas;
        private ILogger<CaptacaoService> _logger;

        public CaptacaoService(IRepository<Captacao> repository, GestorDbContext context,
            SendGridService sendGridService, ILogger<CaptacaoService> logger) : base(repository)
        {
            _context = context;
            _sendGridService = sendGridService;
            _logger = logger;
            _captacaoArquivos = context.Set<CaptacaoArquivo>();
            _captacaoFornecedors = context.Set<CaptacaoFornecedor>();
            _captacaoPropostas = context.Set<Proposta>();
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
            return Filter(q => q
                    .Include(c => c.Criador)
                    .Include(c => c.UsuarioSuprimento)
                    .Include(c => c.Propostas)
                    .Where(c => c.Status == Captacao.CaptacaoStatus.Cancelada ||
                                c.Status >= Captacao.CaptacaoStatus.Encerrada &&
                                c.Propostas.Count == 0)) //@todo Verificar se funciona propostas zeradas
                .ToList();
        }

        public List<Captacao> GetCaptacoesEncerradas()
        {
            return Filter(q => q
                    .Include(c => c.Criador)
                    .Include(c => c.UsuarioSuprimento)
                    .Include(c => c.Propostas)
                    .ThenInclude(p => p.Fornecedor)
                    .Where(c => c.Status >= Captacao.CaptacaoStatus.Encerrada || c.Termino < DateTime.Today))
                .ToList();
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
                .ToList();
        }

        public List<Captacao> GetCaptacoesSelecaoFinalizada()
        {
            return Filter(q => q
                    .Include(c => c.Propostas)
                    .ThenInclude(p => p.Fornecedor)
                    .Include(c => c.UsuarioRefinamento)
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

        public async Task ConfigurarCaptacao(int id, DateTime termino, string consideracoes,
            IEnumerable<int> arquivosIds, IEnumerable<int> fornecedoresIds, int contratoId)
        {
            ThrowIfNotExist(id);
            if (termino < DateTime.Today)
            {
                throw new CaptacaoException("A data máxima não pode ser anterior a data de hoje");
            }

            var captacao = Get(id);
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
                {CaptacaoId = id, FornecedorId = fid});

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
                .Select(cf => cf.Fornecedor);

            var propostas = fornecedores.Select(f => new Proposta()
            {
                FornecedorId = f.Id,
                Fornecedor = f,
                ResponsavelId = f.ResponsavelId,
                CaptacaoId = id,
                Contrato = new PropostaContrato()
                {
                    ParentId = (int) captacao.ContratoId
                },
                Participacao = StatusParticipacao.Pendente,
                DataCriacao = DateTime.Now
            });

            _context.AddRange(propostas);
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

            await SendEmailAtualizacao(captacao);
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
                             (!cp.Finalizado || (cp.Contrato != null && !cp.Contrato.Finalizado))
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
                    q.Where(c => c.Status == Captacao.CaptacaoStatus.Fornecedor && c.Termino < DateTime.Today))
                .ToList();
            foreach (var expirada in expiradas)
            {
                expirada.Status = Captacao.CaptacaoStatus.Encerrada;
            }

            Put(expiradas);
            _logger.LogInformation($"Captações expiradas: {expiradas.Count}");
        }

        public IEnumerable<Proposta> GetPropostasRefinamento(string userId, bool asFornecedor = false)
        {
            return _captacaoPropostas
                .Include(p => p.Fornecedor)
                .Include(p => p.Captacao)
                .Include(p => p.Captacao)
                .Where(proposta =>
                    proposta.Id == proposta.Captacao.PropostaSelecionadaId &&
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
                .Include(p => p.Responsavel);
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
                            "Você foi convidado para participar de um novo projeto para a área de P&D da Taesa",
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
            var emails = fornecedores.Select(f => f.Responsavel.Email);
            var cancelamento = new CancelamentoCaptacao()
            {
                Projeto = captacao.Titulo,
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

        #endregion
    }
}