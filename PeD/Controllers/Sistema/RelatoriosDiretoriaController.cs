using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeD.Authorizations;
using PeD.Core.Models;
using PeD.Services.Captacoes;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Controllers;
using TaesaCore.Interfaces;

namespace PeD.Controllers.Sistema
{
    [SwaggerTag("Relatórios Diretoria")]
    [Route("api/Sistema/RelatoriosDiretoria")]
    [ApiController]
    [Authorize("Bearer")]
    public class RelatoriosDiretoriaController : ControllerCrudBase<RelatorioDiretoria>
    {
        public RelatoriosDiretoriaController(IService<RelatorioDiretoria> service, IMapper mapper) : base(service, mapper)
        {
        }

        [ResponseCache(Duration = 3600)]
        [HttpGet("Shortcodes")]
        public ActionResult GetShortCodes()
        {
            return Ok(ContratoService.GetShortcodesDescriptionsRD());
        }

        [Authorize(Policy = Policies.IsAdmin)]
        public override IActionResult Post(RelatorioDiretoria model)
        {
            return base.Post(model);
        }

        [Authorize(Policy = Policies.IsAdmin)]
        public override IActionResult Put(RelatorioDiretoria model)
        {
            return base.Put(model);
        }

        [Authorize(Policy = Policies.IsAdmin)]
        public override IActionResult Delete(int id)
        {
            return base.Delete(id);
        }
    }
}