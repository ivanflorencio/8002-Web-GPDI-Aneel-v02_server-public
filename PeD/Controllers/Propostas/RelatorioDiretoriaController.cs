using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeD.Authorizations;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.Extensions;
using PeD.Core.Models;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Propostas;
using PeD.Core.Requests.Proposta;
using PeD.Data;
using PeD.Services;
using PeD.Services.Captacoes;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Extensions;
using TaesaCore.Interfaces;

namespace PeD.Controllers.Propostas
{
    [SwaggerTag("Proposta ")]
    [ApiController]
    [Authorize("Bearer")]
    [Route("api/Proposta/[controller]")]
    public class RelatorioDiretoriaController : PropostaNodeBaseController<PropostaRelatorioDiretoria>
    {
        private GestorDbContext _context;
        private PropostaService _propostaService;

        public RelatorioDiretoriaController(IService<PropostaRelatorioDiretoria> service, IMapper mapper,
            IAuthorizationService authorizationService, PropostaService propostaService, GestorDbContext context) :
            base(service, mapper,
                authorizationService, propostaService)
        {
            _context = context;
            _propostaService = propostaService;
        }

        [HttpGet("{captacaoId}")]
        public ActionResult<PropostaRelatorioDiretoriaDto> Get([FromRoute] int captacaoId)
        {
            var relatorio = PropostaService.GetRelatorioDiretoria(captacaoId);

            if (relatorio == null) return NotFound();

            var relatorioDto = Mapper.Map<PropostaRelatorioDiretoriaDto>(relatorio);
            relatorioDto.Titulo = relatorio.Proposta.Captacao.Titulo + " - " + relatorio.Proposta.Fornecedor.Nome;

            return Ok(relatorioDto);
        }

        [HttpPut("")]
        public ActionResult Salvar([FromBody] RelatorioDiretoriaRequest request)
        {
            PropostaService.SalvarRelatorioDiretoria(request.Id, request.Conteudo, request.Draft);
            return Ok(new { request.Id });
        }

        [HttpGet("Download/{propostaId}")]
        public ActionResult PropostaRelatorioDiretoriaDownload(int propostaId)
        {
            var proposta = _propostaService.GetProposta(propostaId);

            if (proposta != null)
            {
                var relatorio = _propostaService.GetRelatorioDiretoriaPdf(proposta.CaptacaoId);
                if (relatorio != null)
                {
                    return PhysicalFile(relatorio.Path, "application/pdf", relatorio.FileName);
                }

                return NotFound();
            }

            return Forbid();
        }



    }
}