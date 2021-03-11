using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PeD.Data;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using PeD.Core.ApiModels.Captacao;
using PeD.Core.ApiModels.Fornecedores;
using PeD.Core.ApiModels.Propostas;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Propostas;
using PeD.Core.Requests.Captacao;
using PeD.Services.Captacoes;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Controllers;

namespace PeD.Controllers.Captacoes
{
    [SwaggerTag("Captacao")]
    [Route("api/Captacoes/Suprimento")]
    [ApiController]
    [Authorize("Bearer")]
    public class SuprimentoController : ControllerServiceBase<Captacao>
    {
        private IUrlHelper _urlHelper;
        private CaptacaoService service;

        public SuprimentoController(CaptacaoService service, IMapper mapper, IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor) : base(service, mapper)
        {
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            this.service = service;
        }

        [HttpGet("")]
        public ActionResult GetCaptacoes()
        {
            var captacoes = Service.Filter(q =>
                q.Include(c => c.UsuarioSuprimento)
                    .Where(c => (c.Status == Captacao.CaptacaoStatus.Elaboracao ||
                                 c.Status == Captacao.CaptacaoStatus.Fornecedor) &&
                                c.UsuarioSuprimentoId == this.UserId()));
            return Ok(Mapper.Map<List<CaptacaoElaboracaoDto>>(captacoes));
        }

        [HttpGet("{id}")]
        public ActionResult<CaptacaoDetalhesDto> GetCaptacao(int id)
        {
            var captacao = Service.Filter(q => q
                .Include(c => c.Arquivos)
                .Include(c => c.FornecedoresSugeridos)
                .ThenInclude(fs => fs.Fornecedor)
                .Include(c => c.FornecedoresConvidados)
                .ThenInclude(fs => fs.Fornecedor)
                .Where(c => (c.Status == Captacao.CaptacaoStatus.Elaboracao ||
                             c.Status == Captacao.CaptacaoStatus.Fornecedor) &&
                            c.UsuarioSuprimentoId == this.UserId() &&
                            c.Id == id
                )).FirstOrDefault();
            if (captacao == null)
            {
                return NotFound();
            }

            var detalhes = Mapper.Map<CaptacaoDetalhesDto>(captacao);
            detalhes.EspecificacaoTecnicaUrl = _urlHelper.Link("DemandaPdf",
                new {id = captacao.DemandaId, form = "especificacao-tecnica"});

            return Ok(detalhes);
        }

        // @todo Authorization ConfigurarCaptacao
        [HttpPut("{id}")]
        public async Task<ActionResult> ConfigurarCaptacao(int id, ConfiguracaoRequest request)
        {
            try
            {
                if (service.UserSuprimento(id) == this.UserId())
                {
                    await service.ConfigurarCaptacao(id, request.Termino, request.Consideracoes, request.Arquivos,
                        request.Fornecedores, request.ContratoId);
                    await service.EnviarParaFornecedores(id);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return NotFound();
            }

            return Ok();
        }

        [HttpPut("{id}/Estender")]
        public ActionResult EstenderCaptacao(int id, ConfiguracaoRequest request)
        {
            try
            {
                if (service.UserSuprimento(id) == this.UserId())
                {
                    service.EstenderCaptacao(id, request.Termino);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return NotFound();
            }

            return Ok();
        }

        [HttpDelete("{id}/Cancelar")]
        public ActionResult CancelarCaptacao(int id)
        {
            try
            {
                if (service.UserSuprimento(id) == this.UserId())
                {
                    service.CancelarCaptacao(id);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Problem(e.Message);
            }

            return Ok();
        }

        [HttpGet("{id}/Propostas")]
        public ActionResult<List<PropostaDto>> GetPropostas(int id)
        {
            if (service.UserSuprimento(id) == this.UserId())
            {
                var propostas = service.GetPropostasPorCaptacao(id);
                return Mapper.Map<List<PropostaDto>>(propostas);
            }

            return Forbid();
        }

        [HttpGet("{id}/Propostas/{status}")]
        public ActionResult<List<PropostaDto>> GetPropostas(int id, StatusParticipacao status)
        {
            if (service.UserSuprimento(id) == this.UserId())
            {
                var propostas = service.GetPropostasPorCaptacao(id, status);
                return Mapper.Map<List<PropostaDto>>(propostas);
            }

            return Forbid();
        }
    }
}