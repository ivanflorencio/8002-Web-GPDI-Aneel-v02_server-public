using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeD.Core.ApiModels.Analises;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.Models;
using PeD.Core.Models.Propostas;
using PeD.Core.Requests.Analises;
using PeD.Core.Requests.Proposta;
using PeD.Services.Analises;
using PeD.Services.Captacoes;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Interfaces;
using System.Linq;

namespace PeD.Controllers.Propostas
{
    [SwaggerTag("Proposta ")]
    [ApiController]
    [Authorize("Bearer")]
    [Route("api/Propostas/{propostaId:guid}/[controller]")]
    public class AnaliseTecnicaController : PropostaNodeBaseController<AnaliseTecnica>
    {
        public AnaliseTecnicaService _analiseTecnicaService;
        public AnaliseTecnicaController(AnaliseTecnicaService service, IMapper mapper,
            IAuthorizationService authorizationService, PropostaService propostaService) : base(service, mapper,
            authorizationService, propostaService)
        {
            _analiseTecnicaService = service;
        }

        [SwaggerOperation("Lista das propostas com análise pendente")]
        [HttpGet("/api/AnaliseTecnica/PropostasPendentes")]
        public ActionResult<List<PropostaAnaliseDto>> Index()
        {
            var propostas = new List<PropostaAnaliseDto>();
            var pendentes = _analiseTecnicaService.GetPropostasAnaliseTecnicaPendente();
            foreach(var item in pendentes) {
                var analise = _analiseTecnicaService.GetAnaliseTecnicaProposta(item.Id);
                var status = "Pendente";
                var responsavel = "";
                if (analise != null) {
                    status = analise.Status;
                    responsavel = analise.Responsavel.NomeCompleto;
                }
                propostas.Add(new PropostaAnaliseDto {
                    PropostaId = item.Id,
                    DemandaId = item.Captacao.Demanda.Id,
                    TituloDemanda = item.Captacao.Demanda.Titulo,
                    DataHora = item.DataCriacao.ToString("dd/MM/yyyy"),
                    Fornecedor = item.Fornecedor.Nome,
                    StatusAnalise = status,
                    AnalistaResponsavel = responsavel,
                });
            }
            return Ok(propostas);
        }

        [SwaggerOperation("Salvar Critérios de Avaliação da Demanda")]
        [HttpPost("/api/AnaliseTecnica/CriteriosAvaliacao")]
        public ActionResult SalvarCriterioAvaliacao(CriterioAvaliacaoRequest criterioAvaliacao)
        {
            _analiseTecnicaService.SalvarCriterioAvaliacao(
                new CriterioAvaliacao {
                    Id = criterioAvaliacao.CriterioId,
                    DemandaId = criterioAvaliacao.DemandaId,
                    Descricao = criterioAvaliacao.Descricao,
                    Peso = criterioAvaliacao.Peso,
                    ResponsavelId = this.UserId(),
                    DoGestor = criterioAvaliacao.DoGestor
                }
            );
            return Ok();
        }

        [SwaggerOperation("Lista dos Critérios de Avaliação das Demandas Pendentes")]
        [HttpGet("/api/AnaliseTecnica/CriteriosAvaliacao")]
        public ActionResult<List<CriteriosDemandasDto>> ListarCriteriosAvaliacao()
        {
            var criteriosDemandas = new List<CriteriosDemandasDto>();
            var demandas = _analiseTecnicaService.GetDemandasPendentesAnalise();
            foreach (var demanda in demandas)
            {
                var criterios = _analiseTecnicaService.GetCriteriosAvaliacaoDemanda(demanda.Id);
                criteriosDemandas.Add(new CriteriosDemandasDto {
                    DemandaId = demanda.Id,
                    TituloDemanda = demanda.Titulo,
                    CriteriosAvaliacao = criterios
                });
            }
            return Ok(criteriosDemandas);
        }
        
        [SwaggerOperation("Analise Tecnica da Proposta")]
        [HttpGet("/api/AnaliseTecnica/{propostaId:int}")]
        public ActionResult<AnaliseTecnicaDto> AbrirAnaliseTecnicaProposta(int propostaId)
        {

            var analiseTecnica = _analiseTecnicaService.GetAnaliseTecnicaProposta(propostaId);
            var criteriosAvaliacao = _analiseTecnicaService.GetCriteriosAvaliacaoDemanda(analiseTecnica.Proposta.Captacao.DemandaId);

            var pareceres = new List<ParecerTecnicoDto>();
            foreach (var item in criteriosAvaliacao)
            {
                var parecerId = 0;
                var justificativa = "";
                var pontuacao = 0;
                if (analiseTecnica.Pareceres.Count() > 0 ) {
                    var parecer = analiseTecnica.Pareceres.Where(x=>x.CriterioId == item.Id).First();
                    if (parecer != null) {
                        parecerId = parecer.Id;
                        justificativa = parecer.Justificativa;
                        pontuacao = parecer.Pontuacao;
                    }
                }
                pareceres.Add(new ParecerTecnicoDto() {
                    CriterioId = item.Id,
                    Peso = item.Peso,
                    DescricaoCriterio = item.Descricao,
                    Id = parecerId,
                    Justificativa = justificativa,
                    Pontuacao = pontuacao,
                });
            }

            var analise = new AnaliseTecnicaDto();
            analise.Id = analiseTecnica.Id;
            analise.Comentarios = analiseTecnica.Comentarios;
            analise.Justificativa = analiseTecnica.Justificativa;
            analise.PropostaId = analiseTecnica.PropostaId;
            analise.Status = analiseTecnica.Status;
            analise.Pareceres = pareceres;
        
            return Ok(analise);
        }

        [SwaggerOperation("Salvar Dados da Análise Técnica")]
        [HttpPost("/api/AnaliseTecnica")]
        public ActionResult SalvarCriterioAvaliacao(AnaliseTecnicaRequest analiseTecnica)
        {
            var pareceres = new List<ParecerTecnico>();
            foreach (var item in analiseTecnica.Pareceres) {
                pareceres.Add(new ParecerTecnico {
                    Pontuacao = item.Pontuacao,
                    Justificativa = item.Justificativa,
                    CriterioId = item.CriterioId,
                });
            }
            _analiseTecnicaService.SalvarAnaliseTecnica(
                new AnaliseTecnica {
                    Id = analiseTecnica.Id,
                    Comentarios = analiseTecnica.Comentarios,
                    Justificativa = analiseTecnica.Justificativa,
                    PontuacaoFinal = analiseTecnica.PontuacaoFinal,
                    PropostaId = analiseTecnica.PropostaId,
                    ResponsavelId = this.UserId(),
                    Pareceres = pareceres
                }
            );
            return Ok();
        }
    }
}