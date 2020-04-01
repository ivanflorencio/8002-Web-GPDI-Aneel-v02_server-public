using APIGestor.Models.Captacao;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Controllers;
using TaesaCore.Interfaces;

namespace APIGestor.Controllers.Sistema
{
    [SwaggerTag("Contratos Padrão")]
    [Route("api/Sistema/Contratos")]
    [ApiController]
    [Authorize("Bearer")]
    public class ContratosController : ControllerServiceBase<Contrato>
    {
        public ContratosController(IService<Contrato> service, IMapper mapper) : base(service, mapper)
        {
        }
    }
}