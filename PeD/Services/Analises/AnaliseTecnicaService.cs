using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeD.Core.ApiModels.Demandas;
using PeD.Core.Exceptions.Captacoes;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Demandas;
using PeD.Core.Models.Propostas;
using PeD.Data;
using TaesaCore.Interfaces;
using TaesaCore.Services;
using static PeD.Core.Models.Captacoes.Captacao;

namespace PeD.Services.Analises
{
    public class AnaliseTecnicaService : BaseService<AnaliseTecnica>
    {
        private ILogger<AnaliseTecnicaService> _logger;
        private GestorDbContext _context;
        private DbSet<CriterioAvaliacao> _criterioAvaliacao;
        private DbSet<ParecerTecnico> _parecerTecnico;
        
        public AnaliseTecnicaService(IRepository<AnaliseTecnica> repository, GestorDbContext context,
            ILogger<AnaliseTecnicaService> logger, 
            IMapper mapper) : base(repository)
        {
            _context = context;
            _logger = logger;
            _criterioAvaliacao = context.Set<CriterioAvaliacao>();
            _parecerTecnico = context.Set<ParecerTecnico>();
        }

        protected void ThrowIfNotExist(int id)
        {
            if (!Exist(id))
            {
                throw new Exception("Analise Técnica não encontrada");
            }
        }

        public List<Proposta> GetPropostasAnaliseTecnicaPendente()
        {
            var analises = _context.Set<AnaliseTecnica>().AsQueryable();
            var propostas = _context.Set<Proposta>().AsQueryable();

            var query = 
                from proposta in propostas
                where (
                    proposta.Participacao == StatusParticipacao.Aceito
                    || proposta.Participacao == StatusParticipacao.Concluido
                )
                && !(from analise in analises 
                        where analise.PropostaId == proposta.Id
                        && (
                            analise.Status != "Aberta" 
                            && analise.Status != "Pendente"
                            && analise.Status != "Enviada"
                        )
                        select analise.PropostaId).Contains(proposta.Id)
                select proposta;
            
            return query.Include(c => c.Captacao)
                .ThenInclude(d => d.Demanda)
                .ThenInclude(d => d.AnalistaTecnico)
                .Include(f=>f.Fornecedor)
                .Distinct()
                .ToList();
        }

        public AnaliseTecnica GetAnaliseTecnicaProposta(int propostaId)
        {
            var query =
                from analise in _context.Set<AnaliseTecnica>().AsQueryable()
                where
                    analise.PropostaId == propostaId
                    && (
                        analise.Status == "Aberta"
                        || analise.Status == "Pendente"
                        || analise.Status == "Enviada"
                    )
                select analise;


            return query
                .Include(r => r.Responsavel)
                .Include(c => c.Pareceres)
                .ThenInclude(r => r.Responsavel)
                .Include(p => p.Proposta)
                .ThenInclude(c => c.Captacao)
                .FirstOrDefault();
        }


        public List<CriterioAvaliacao> GetCriteriosAvaliacaoProposta(int propostaId)
        {
            var demandas = _context.Set<Proposta>().AsQueryable();
            var demanda = demandas
                                .Include(x=>x.Captacao)
                                .ThenInclude(x=>x.Demanda)
                                .First(x=>x.Id == propostaId)?.Captacao.Demanda;
            return this.GetCriteriosAvaliacaoDemanda(demanda?.Id ?? 0);
        }

        public List<CriterioAvaliacao> GetCriteriosAvaliacaoDemanda(int demandaId)
        {
            var query =
                from criterio in _context.Set<CriterioAvaliacao>().AsQueryable()
                where
                    criterio.DemandaId == demandaId                    
                select criterio;

            return query
                .Include(x => x.Responsavel)
                .ToList();
        }

        public List<Demanda> GetDemandasPendentesAnalise()
        {
            var query =
                from captacao in _context.Set<Captacao>().AsQueryable()
                where (
                    captacao.Status == CaptacaoStatus.Elaboracao ||
                    captacao.Status == CaptacaoStatus.Fornecedor ||
                    captacao.Status == CaptacaoStatus.Pendente ||
                    captacao.Status == CaptacaoStatus.Encerrada ||
                    captacao.Status == CaptacaoStatus.Refinamento
                    )
                select captacao.Demanda;


            return query
                .ToList();
        }

        public CriterioAvaliacao SalvarCriterioAvaliacao(CriterioAvaliacao criterioAvaliacao)
        {
            var criterios = _context.Set<CriterioAvaliacao>();
            
            if (criterioAvaliacao.Id > 0) {
                var criterio = criterios.Where(x=>x.Id == criterioAvaliacao.Id).FirstOrDefault();
                criterio.Descricao = criterioAvaliacao.Descricao;
                criterio.Peso = criterioAvaliacao.Peso;
                criterios.Update(criterio);                
            } else {                
                criterioAvaliacao.DataHora = DateTime.Now;
                criterioAvaliacao.Guid = Guid.NewGuid();
                criterios.Add(criterioAvaliacao);
            }
            
            _context.SaveChanges();

            return criterioAvaliacao;
        }

        public void RemoverCriterioAvaliacao(int criterioId)
        {
            var criterios = _context.Set<CriterioAvaliacao>();
            var criterio = criterios.First(x=>x.Id == criterioId);
            if (criterio != null) {
               criterios.Remove(criterio);
               _context.SaveChanges();
            }
        }

        public void SalvarAnaliseTecnica(AnaliseTecnica analiseTecnica)
        {
            var analises = _context.Set<AnaliseTecnica>();
            var pareceres = _context.Set<ParecerTecnico>();
            var analiseId = analiseTecnica.Id;

            foreach (var item in analiseTecnica.Pareceres) {
                item.ResponsavelId = analiseTecnica.ResponsavelId;
                item.DataHora = DateTime.Now;                    
            }
            
            // Caso seja um update
            if (analiseId > 0) {
                var analise = _context.AnaliseTecnica.First(x=>x.Id == analiseId);
                analise.Comentarios = analiseTecnica.Comentarios;
                analise.Justificativa = analiseTecnica.Justificativa;
                analise.PontuacaoFinal = analiseTecnica.PontuacaoFinal;
                analise.DataHora = DateTime.Now;
                
                // Alterando Status caso esteja definido
                if (!String.IsNullOrEmpty(analiseTecnica.Status))
                    analise.Status = analiseTecnica.Status;
                
                // Limpando pareceres anteriores e adicionado novos
                pareceres.RemoveRange(pareceres.Where(x=>x.AnaliseTecnicaId == analiseId));
                _context.SaveChanges();                

                // Adicionando informações (nao enviadas) aos pareceres da analise
                foreach (var item in analiseTecnica.Pareceres) {
                    item.Id = 0;
                    item.ResponsavelId = analiseTecnica.ResponsavelId;
                    item.DataHora = DateTime.Now;
                    item.AnaliseTecnicaId = analiseTecnica.Id;
                }
                pareceres.AddRange(analiseTecnica.Pareceres);
                _context.SaveChanges();

            // Caso seja um insert
            } else {
                analiseTecnica.Guid = Guid.NewGuid();
                analiseTecnica.Status = "Aberta";
                analises.Add(analiseTecnica);               
                
                _context.SaveChanges();
                
            }
        }

        public void EnviarAnaliseTecnica(AnaliseTecnica analiseTecnica)
        {
            analiseTecnica.Status = "Enviada";
            this.SalvarAnaliseTecnica(analiseTecnica);            
        }

        internal bool VerificarAnaliseTecnicaFinalizada(int propostaId)
        {
            return _context.AnaliseTecnica.Any(x=>x.PropostaId == propostaId && x.Status == "Enviada");
        }
    }
}