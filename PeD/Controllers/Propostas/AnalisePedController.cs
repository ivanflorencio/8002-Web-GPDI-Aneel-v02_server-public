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
    public class AnalisePedController : PropostaNodeBaseController<AnalisePed>
    {
        public AnalisePedService _analisePedService;
        public AnalisePedController(AnalisePedService service, IMapper mapper,
            IAuthorizationService authorizationService, PropostaService propostaService) : base(service, mapper,
            authorizationService, propostaService)
        {
            _analisePedService = service;
        }

        [SwaggerOperation("Lista das propostas com análise PeD pendente")]
        [HttpGet("/api/AnalisePed/PropostasPendentes")]
        public ActionResult<List<PropostaAnaliseDto>> Index()
        {
            var propostas = new List<PropostaAnaliseDto>();
            var pendentes = _analisePedService.GetPropostasAnalisePedPendente();
            foreach(var item in pendentes) {
                var analise = _analisePedService.GetAnalisePedProposta(item.Id);
                var status = "Pendente";
                var responsavel = item.Captacao.Demanda.AnalistaPed?.NomeCompleto;
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

        [SwaggerOperation("Analise Ped da Proposta")]
        [HttpGet("/api/AnalisePed/{propostaId:int}")]
        public ActionResult<AnalisePedDto> AbrirAnalisePedProposta(int propostaId)
        {
            var analisePed = _analisePedService.GetAnalisePedProposta(propostaId);
            
            var analise = new AnalisePedDto();
            analise.Id = analisePed?.Id ?? 0;
            analise.PropostaId = propostaId;
            analise.Originalidade = analisePed?.Originalidade ?? "";
            analise.PontuacaoOriginalidade = analisePed?.PontuacaoOriginalidade ?? 0;
            analise.Aplicabilidade = analisePed?.Aplicabilidade ?? "";
            analise.PontuacaoAplicabilidade = analisePed?.PontuacaoAplicabilidade ?? 0;
            analise.Relevancia = analisePed?.Relevancia ?? "";
            analise.PontuacaoRelevancia = analisePed?.PontuacaoRelevancia ?? 0;
            analise.RazoabilidadeCustos = analisePed?.RazoabilidadeCustos ?? "";
            analise.PontuacaoRazoabilidadeCustos = analisePed?.PontuacaoRazoabilidadeCustos ?? 0;
            analise.Status = analisePed?.Status ?? "Aberta";
            analise.PontosCriticos = analisePed?.PontosCriticos ?? "";
            analise.Comentarios = analisePed?.Comentarios ?? "";
            analise.Conceito = analisePed?.Conceito ?? "";
            analise.PontuacaoFinal = analisePed?.PontuacaoFinal ?? 0;
        
            return Ok(analise);
        }

        [SwaggerOperation("Salvar Dados da Análise PeD")]
        [HttpPost("/api/AnalisePed")]
        public ActionResult SalvarPareceresAnalisePed(AnalisePedRequest analisePed)
        {
            _analisePedService.SalvarAnalisePed(
                new AnalisePed {
                    Id = analisePed.Id,
                    PropostaId = analisePed.PropostaId,
                    Originalidade = analisePed?.Originalidade ?? "",
                    PontuacaoOriginalidade = analisePed?.PontuacaoOriginalidade ?? 0,
                    Aplicabilidade = analisePed?.Aplicabilidade ?? "",
                    PontuacaoAplicabilidade = analisePed?.PontuacaoAplicabilidade ?? 0,
                    Relevancia = analisePed?.Relevancia ?? "",
                    PontuacaoRelevancia = analisePed?.PontuacaoRelevancia ?? 0,
                    RazoabilidadeCustos = analisePed?.RazoabilidadeCustos ?? "",
                    PontuacaoRazoabilidadeCustos = analisePed?.PontuacaoRazoabilidadeCustos ?? 0,
                    Status = analisePed?.Status ?? "Aberta",
                    PontosCriticos = analisePed?.PontosCriticos ?? "",
                    Comentarios = analisePed?.Comentarios ?? "",
                    Conceito = analisePed?.Conceito ?? "",
                    PontuacaoFinal = analisePed?.PontuacaoFinal ?? 0,
                    ResponsavelId = this.UserId(),                    
                }
            );
            return Ok();
        }

        [SwaggerOperation("Enviar da Análise Técnica Finalizada")]
        [HttpPost("/api/AnalisePed/Enviar")]
        public ActionResult EnviarAnalisePed(AnalisePedRequest analisePed)
        {
            _analisePedService.EnviarAnalisePed(
                new AnalisePed {
                    Id = analisePed.Id,
                    PropostaId = analisePed.PropostaId,
                    Originalidade = analisePed?.Originalidade ?? "",
                    PontuacaoOriginalidade = analisePed?.PontuacaoOriginalidade ?? 0,
                    Aplicabilidade = analisePed?.Aplicabilidade ?? "",
                    PontuacaoAplicabilidade = analisePed?.PontuacaoAplicabilidade ?? 0,
                    Relevancia = analisePed?.Relevancia ?? "",
                    PontuacaoRelevancia = analisePed?.PontuacaoRelevancia ?? 0,
                    RazoabilidadeCustos = analisePed?.RazoabilidadeCustos ?? "",
                    PontuacaoRazoabilidadeCustos = analisePed?.PontuacaoRazoabilidadeCustos ?? 0,
                    Status = analisePed?.Status ?? "Aberta",
                    PontosCriticos = analisePed?.PontosCriticos ?? "",
                    Comentarios = analisePed?.Comentarios ?? "",
                    Conceito = analisePed?.Conceito ?? "",
                    PontuacaoFinal = analisePed?.PontuacaoFinal ?? 0,
                    ResponsavelId = this.UserId(),                    
                }
            );            
            return Ok();
        }
    }
}