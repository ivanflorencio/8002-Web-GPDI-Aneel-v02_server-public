using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeD.Core.Models.Propostas;
using PeD.Data;
using TaesaCore.Interfaces;
using TaesaCore.Services;

namespace PeD.Services.Analises
{
    public class AnalisePedService : BaseService<AnalisePed>
    {
        private ILogger<AnalisePedService> _logger;
        private GestorDbContext _context;

        public AnalisePedService(IRepository<AnalisePed> repository, GestorDbContext context,
            ILogger<AnalisePedService> logger,
            IMapper mapper) : base(repository)
        {
            _context = context;
            _logger = logger;
        }

        protected void ThrowIfNotExist(int id)
        {
            if (!Exist(id))
            {
                throw new Exception("Analise PeD não encontrada");
            }
        }

        public List<Proposta> GetPropostasAnalisePedPendente()
        {
            var analisesTecnicas = _context.Set<AnaliseTecnica>().AsQueryable();
            var analisesPed = _context.Set<AnalisePed>().AsQueryable();
            var propostas = _context.Set<Proposta>().AsQueryable();

            var query =
                from proposta in propostas
                where (
                    proposta.Participacao == StatusParticipacao.Aceito
                    || proposta.Participacao == StatusParticipacao.Concluido
                )
                && (from analiseTecnica in analisesTecnicas
                    where analiseTecnica.PropostaId == proposta.Id
                    && analiseTecnica.Status == "Enviada"
                    select analiseTecnica.PropostaId).Contains(proposta.Id)
                && !(from analisePed in analisesPed
                     where analisePed.PropostaId == proposta.Id
                     && (
                         analisePed.Status != "Aberta"
                         && analisePed.Status != "Pendente"
                         && analisePed.Status != "Enviada"
                     )
                     select analisePed.PropostaId).Contains(proposta.Id)
                select proposta;

            return query.Include(c => c.Captacao)
                .ThenInclude(d => d.Demanda)
                .ThenInclude(d => d.AnalistaPed)
                .Include(f => f.Fornecedor)
                .Distinct()
                .ToList();
        }

        public AnalisePed GetAnalisePedProposta(int propostaId)
        {
            var query =
                from analise in _context.Set<AnalisePed>().AsQueryable()
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
                .Include(p => p.Proposta)
                .ThenInclude(c => c.Captacao)
                .ThenInclude(c => c.Demanda)
                .ThenInclude(c => c.AnalistaPed)
                .FirstOrDefault();
        }

        public void SalvarAnalisePed(AnalisePed analisePed)
        {
            var analises = _context.Set<AnalisePed>();
            var analiseId = analisePed.Id;

            // Caso seja um update
            if (analiseId > 0)
            {
                var analise = _context.AnalisePed.First(x => x.Id == analiseId);
                analise.Originalidade = analisePed.Originalidade;
                analise.PontuacaoOriginalidade = analisePed.PontuacaoOriginalidade;
                analise.Aplicabilidade = analisePed.Aplicabilidade;
                analise.PontuacaoAplicabilidade = analisePed.PontuacaoAplicabilidade;
                analise.Relevancia = analisePed.Relevancia;
                analise.PontuacaoRelevancia = analisePed.PontuacaoRelevancia;
                analise.RazoabilidadeCustos = analisePed.RazoabilidadeCustos;
                analise.PontuacaoRazoabilidadeCustos = analisePed.PontuacaoRazoabilidadeCustos;
                analise.PontosCriticos = analisePed.PontosCriticos;
                analise.Comentarios = analisePed.Comentarios;
                analise.Conceito = analisePed.Conceito;
                analise.PontuacaoFinal = analisePed.PontuacaoFinal;
                analise.DataHora = DateTime.Now;

                // Alterando Status caso esteja definido
                if (!String.IsNullOrEmpty(analisePed.Status))
                    analise.Status = analisePed.Status;

                _context.SaveChanges();

                // Caso seja um insert
            }
            else
            {
                analisePed.Guid = Guid.NewGuid();
                analisePed.Status = "Aberta";
                analisePed.DataHora = DateTime.Now;
                analises.Add(analisePed);

                _context.SaveChanges();

            }
        }

        public void EnviarAnalisePed(AnalisePed analisePed)
        {
            analisePed.Status = "Enviada";
            this.SalvarAnalisePed(analisePed);
        }

        internal bool VerificarAnalisePedFinalizada(int propostaId)
        {
            return _context.AnalisePed.Any(x => x.PropostaId == propostaId && x.Status == "Enviada");
        }

        internal static string MontarRelatorio(AnalisePed analise)
        {
            var sb = new StringBuilder();

            sb.Append("<style>.analise td {background-color: #f0f0f0; border: solid 2px #ffffff;}</style>");
            sb.Append("<style>.analise th {background-color: #007984;color: #ffffff;}</style>");
            sb.Append("<table class='analise' style='width:100%' cellpadding='8'><tr><th style='text-align:left;'>Critérios</th><th>Pontuação</th></tr>");
            sb.Append(MontarCriterio("Originalidade", analise.Originalidade, analise.PontuacaoOriginalidade.ToString()));
            sb.Append(MontarCriterio("Aplicabilidade", analise.Aplicabilidade, analise.PontuacaoAplicabilidade.ToString()));
            sb.Append(MontarCriterio("Relevância", analise.Relevancia, analise.PontuacaoRelevancia.ToString()));
            sb.Append(MontarCriterio("Razoabilidade de Custos", analise.RazoabilidadeCustos, analise.PontuacaoRazoabilidadeCustos.ToString()));
            sb.Append(MontarCriterio("Pontos Críticos", analise.PontosCriticos));
            sb.Append(MontarCriterio("Comentários", analise.Comentarios));
            sb.Append("</table><br/>");

            sb.Append("<table class='analise' style='width:100%' cellpadding='8'><tr><th style='text-align:left;'>Conceito</th><th>Pontuação Final</th></tr>");
            sb.Append($"<tr><td><p>Conceito do projeto em função da média de pontuação obtida: <strong>{analise.Conceito}</strong></p></td>");
            sb.Append($"<td><h3 style='text-align:center'>{analise.PontuacaoFinal}</h3></td></tr>");
            sb.Append("</table>");

            return sb.ToString();
        }

        internal static string MontarCriterio(string titulo, string justificativa, string pontuacao = "")
        {
            return $"<tr><td colspan='{(String.IsNullOrEmpty(pontuacao) ? "2" : "1")}'><p><strong>{titulo}</strong><br/>{justificativa}</p></td>"
                     + ((String.IsNullOrEmpty(pontuacao)) ? "</tr>" : $"<td style='text-align:center'><strong>{pontuacao}</strong></td></tr>");
        }
    }
}