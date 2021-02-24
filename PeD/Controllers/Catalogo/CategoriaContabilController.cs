using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeD.Core.Models.Catalogos;
using PeD.Data;
using Swashbuckle.AspNetCore.Annotations;
using TaesaCore.Controllers;
using TaesaCore.Interfaces;

namespace PeD.Controllers.Catalogo
{
    [SwaggerTag("Catalogo")]
    [Route("api/Catalogo/[controller]")]
    [ApiController]
    [Authorize("Bearer")]
    public class CategoriaContabilController : CatalogController<CategoriaContabil>
    {
        public CategoriaContabilController(GestorDbContext context) : base(context)
        {
        }
    }
}