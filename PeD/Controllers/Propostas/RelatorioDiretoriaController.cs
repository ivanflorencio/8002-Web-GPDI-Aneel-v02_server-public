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
    [Route("api/Propostas/{propostaId:guid}/[controller]")]
    public class RelatorioDiretoriaController : PropostaNodeBaseController<PropostaRelatorioDiretoria>
    {
        private GestorDbContext _context;

        public RelatorioDiretoriaController(IService<PropostaRelatorioDiretoria> service, IMapper mapper,
            IAuthorizationService authorizationService, PropostaService propostaService, GestorDbContext context) :
            base(service, mapper,
                authorizationService, propostaService)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<ActionResult<PropostaRelatorioDiretoriaDto>> Get([FromRoute] Guid propostaId, int captacaoId)
        {
            if (!await HasAccess())
                return Forbid();
            var relatorio = PropostaService.GetRelatorioDiretoria(captacaoId);
            relatorio = PropostaService.GetRelatorioDiretoriaFull(relatorio.PropostaId);
            
            return Ok(Mapper.Map<PropostaRelatorioDiretoriaDto>(relatorio));
        }

        [HttpPost("")]
        public async Task<ActionResult> Post([FromRoute] int contratoId,
            [FromBody] RelatorioDiretoriaRequest request)
        {
            if (!await HasAccess(true))
                return Forbid();

            var relatorioProposta = PropostaService.GetRelatorioDiretoria(contratoId);
            var hash = relatorioProposta.Conteudo?.ToSHA256() ?? "";
            var hasChanges = !hash.Equals(request.Conteudo.ToSHA256());

            relatorioProposta.Finalizado = relatorioProposta.Finalizado || !request.Draft;
            relatorioProposta.Conteudo = request.Conteudo;
            
            if (relatorioProposta.Id != 0)
            {
                Service.Put(relatorioProposta);
            }
            else
            {
                Service.Post(relatorioProposta);                
            }

            if (!request.Draft && (hasChanges || relatorioProposta.FileId == null))
            {
                var file = PropostaService.SaveRelatorioDiretoriaPdf(relatorioProposta);
                relatorioProposta.File = file;
                relatorioProposta.FileId = file.Id;
                Service.Put(relatorioProposta);
            }

            return Ok(new { relatorioProposta.Id });
        }
    }
}