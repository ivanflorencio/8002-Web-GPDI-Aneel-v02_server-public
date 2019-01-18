﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APIGestor.Business;
using APIGestor.Models;

namespace APIGestor.Controllers
{
    [Route("api/projeto/")]
    [ApiController]
    [Authorize("Bearer")]
    public class LogProjetosController : ControllerBase
    {
        private LogProjetoService _service;

        public LogProjetosController(LogProjetoService service)
        {
            _service = service;
        }

        [HttpGet("{projetoId}/log")]
        public IEnumerable<LogProjeto> Get(int projetoId)
        {
            return _service.ListarTodos(projetoId);
        }

        [Route("[controller]")]
        [HttpPost]
        public Resultado Post([FromBody]LogProjeto LogProjeto)
        {
            return _service.Incluir(LogProjeto);
        }

        [HttpDelete("[controller]/{Id}")]
        public Resultado Delete(int id)
        {
            return _service.Excluir(id);
        }
    }
}