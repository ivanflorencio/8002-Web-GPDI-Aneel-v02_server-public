using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeD.Core.ApiModels.Analises;
using PeD.Core.Models.Propostas;
using PeD.Core.Requests.Analises;
using PeD.Services.Analises;
using PeD.Services.Captacoes;
using Swashbuckle.AspNetCore.Annotations;
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
                var responsavel = item.Captacao.Demanda.AnalistaTecnico?.NomeCompleto;
                double pontuacao = 0;
                if (analise != null) {
                    status = analise.Status;
                    responsavel = analise.Responsavel.NomeCompleto;
                    pontuacao = analise.PontuacaoFinal;
                }
                propostas.Add(new PropostaAnaliseDto {
                    PropostaId = item.Id,
                    CaptacaoId = item.CaptacaoId,
                    DemandaId = item.Captacao.Demanda.Id,
                    TituloDemanda = item.Captacao.Demanda.Titulo,
                    DataHora = item.DataCriacao.ToString("dd/MM/yyyy"),
                    Fornecedor = item.Fornecedor.Nome,
                    StatusAnalise = status,
                    AnalistaResponsavel = responsavel,
                    Pontuacao = pontuacao,
                });
            }
            return Ok(propostas);
        }

        [SwaggerOperation("Salvar Critérios de Avaliação da Demanda")]
        [HttpPost("/api/AnaliseTecnica/CriteriosAvaliacao")]
        public ActionResult<CriterioAvaliacao> SalvarCriterioAvaliacao(CriterioAvaliacaoRequest criterioAvaliacao)
        {
            var criterioSalvo = _analiseTecnicaService.SalvarCriterioAvaliacao(
                new CriterioAvaliacao {
                    Id = criterioAvaliacao.CriterioId,
                    DemandaId = criterioAvaliacao.DemandaId,
                    Descricao = criterioAvaliacao.Descricao,
                    Peso = criterioAvaliacao.Peso,
                    ResponsavelId = this.UserId(),
                    DoGestor = criterioAvaliacao.DoGestor
                }
            );
            return Ok(criterioSalvo);
        }

        [SwaggerOperation("Excluir Critérios de Avaliação da Demanda")]
        [HttpDelete("/api/AnaliseTecnica/CriteriosAvaliacao/{criterioId:int}")]
        public ActionResult RemoverCriterioAvaliacao(int criterioId)
        {
            _analiseTecnicaService.RemoverCriterioAvaliacao(criterioId);
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
            var criteriosAvaliacao = _analiseTecnicaService.GetCriteriosAvaliacaoProposta(propostaId);

            var pareceres = new List<ParecerTecnicoDto>();
            foreach (var item in criteriosAvaliacao)
            {
                var parecerId = 0;
                var justificativa = "";
                var pontuacao = 0;
                var analistaResponsavel = "";
                var dataHora = "";
                if (
                        analiseTecnica != null 
                        && analiseTecnica.Pareceres.Count() > 0 
                        && analiseTecnica.Pareceres.Any(x=>x.CriterioId == item.Id) 
                    ) {
                    var parecer = analiseTecnica.Pareceres.First(x=>x.CriterioId == item.Id);
                    if (parecer != null) {
                        parecerId = parecer.Id;
                        justificativa = parecer.Justificativa;
                        pontuacao = parecer.Pontuacao;
                        analistaResponsavel = parecer.Responsavel.NomeCompleto;
                        dataHora = parecer.DataHora.ToString("dd/MM/yyyy H:i");
                    }
                }
                pareceres.Add(new ParecerTecnicoDto() {
                    CriterioId = item.Id,
                    Peso = item.Peso,
                    DescricaoCriterio = item.Descricao,
                    Id = parecerId,
                    Justificativa = justificativa,
                    Pontuacao = pontuacao,       
                    AnalistaResponsavel = analistaResponsavel,     
                    DataHora = dataHora,
                });
            }

            var analise = new AnaliseTecnicaDto();
            analise.Id = analiseTecnica?.Id ?? 0;
            analise.Comentarios = analiseTecnica?.Comentarios ?? "";
            analise.Justificativa = analiseTecnica?.Justificativa ?? "";
            analise.PropostaId = propostaId;
            analise.Status = analiseTecnica?.Status ?? "Aberta";
            analise.Pareceres = pareceres;
            analise.PontuacaoFinal = analiseTecnica?.PontuacaoFinal ?? 0;
        
            return Ok(analise);
        }

        [SwaggerOperation("Salvar Dados da Análise Técnica")]
        [HttpPost("/api/AnaliseTecnica")]
        public ActionResult SalvarPareceresAnaliseTecnica(AnaliseTecnicaRequest analiseTecnica)
        {
            var pareceres = new List<ParecerTecnico>();
            foreach (var item in analiseTecnica.Pareceres) {
                pareceres.Add(new ParecerTecnico {
                    Id = item.ParecerId,
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

        [SwaggerOperation("Enviar da Análise Técnica Finalizada")]
        [HttpPost("/api/AnaliseTecnica/Enviar")]
        public ActionResult EnviarAnaliseTecnica(AnaliseTecnicaRequest analiseTecnica)
        {
            var pareceres = new List<ParecerTecnico>();
            foreach (var item in analiseTecnica.Pareceres) {
                pareceres.Add(new ParecerTecnico {
                    Id = item.ParecerId,
                    Pontuacao = item.Pontuacao,
                    Justificativa = item.Justificativa,
                    CriterioId = item.CriterioId,
                });
            }
            _analiseTecnicaService.EnviarAnaliseTecnica(
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