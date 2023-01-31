using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PeD.Authorizations;
using PeD.Core.ApiModels.Sistema;      
using PeD.Core.Models;
using PeD.Core.Requests.Sistema.TabelaValorHora;
using PeD.Data;
using PeD.Services;
using PeD.Services.Captacoes;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Controllers;
using TaesaCore.Interfaces;

namespace PeD.Controllers.Sistema
{
    [SwaggerTag("TabelasValorHora")]
    [Route("api/Sistema/TabelasValorHora")]
    [ApiController]
    [Authorize("Bearer")]
    public class TabelasValorHoraController : ControllerCrudBase<TabelaValorHora, TabelaValorHoraDto,
        TabelaValorHoraCreateRequest,
        TabelaValorHoraEditRequest>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private UserService _userService;
        private PropostaService _propostaService;
        protected AccessManager AccessManager;
        private GestorDbContext _context;
        private ILogger<TabelasValorHoraController> _logger;

        public TabelasValorHoraController(IService<TabelaValorHora> service, IMapper mapper,
            UserManager<ApplicationUser> userManager, AccessManager accessManager, UserService userService,
            PropostaService propostaService, GestorDbContext context, ILogger<TabelasValorHoraController> logger) : base(
            service, mapper)
        {
            _userManager = userManager;
            AccessManager = accessManager;
            _userService = userService;
            _propostaService = propostaService;
            _context = context;
            _logger = logger;
        }


        [Authorize(Policy = Policies.IsUserNorteEnergia)]
        public override ActionResult<TabelaValorHoraDto> Get(int id)
        {
            if (!Service.Exist(id))
                return NotFound();
            var tabela = Service.Filter(q => q.Where(t => t.Id == id)).FirstOrDefault();
            return Mapper.Map<TabelaValorHoraDto>(tabela);
        }

        [Authorize(Policy = Policies.IsUserNorteEnergia)]
        public override ActionResult<List<TabelaValorHoraDto>> Get()
        {
            var tabelas = Service.Get().ToList();
            return Mapper.Map<List<TabelaValorHoraDto>>(tabelas);
        }

        [Authorize(Policy = Policies.IsAdmin)]
        [HttpPost]
        public override IActionResult Post(TabelaValorHoraCreateRequest model)
        {
            var tabela = new TabelaValorHora()
            {
                Nome = model.Nome,
                Registros = model.Registros,                
            };
            try
            {
                Service.Post(tabela);
            }
            catch (Exception e)
            {
                _logger.LogError("Não foi possível salvar a tabela:{Error}", e.Message);
                return Problem("Não foi possível salvar a tabela");
            }


            return Ok(tabela);
        }

        [Authorize(Policy = Policies.IsAdmin)]
        [HttpPut]
        public override IActionResult Put(TabelaValorHoraEditRequest model)
        {
            if (!Service.Exist(model.Id))
                return NotFound();
            var tabela = Service.Get(model.Id);

            tabela.Nome = model.Nome;
            tabela.Registros = model.Registros;

            Service.Put(tabela);
            
            return Ok(tabela);
        }

        [Authorize(Policy = Policies.IsAdmin)]
        [HttpDelete("{id}")]
        public override IActionResult Delete(int id)
        {
            if (!Service.Exist(id))
                return NotFound();
                Service.Delete(id);
            return Ok();
        }
    }
}