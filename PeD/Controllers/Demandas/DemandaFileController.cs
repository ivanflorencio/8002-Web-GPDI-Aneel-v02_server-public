using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PeD.Authorizations;
using PeD.Core.Models.Demandas;
using PeD.Data;

namespace PeD.Controllers.Demandas
{
    [Route("api/Demandas/")]
    [ApiController]
    [Authorize("Bearer")]
    [Authorize(Policy = Policies.IsColaborador)]
    public class DemandaFileController : FileBaseController<DemandaFile>
    {
        public DemandaFileController(GestorDbContext context, IConfiguration configuration) : base(context,
            configuration)
        {
        }

        [HttpDelete("{id}/Files/{fileId}")]
        public IActionResult RemoveFile(int id, int fileId)
        {
            var file = context.DemandaFiles.FirstOrDefault(df => df.DemandaId == id && df.Id == fileId);
            if (file == null) return NotFound();
            try
            {
                context.DemandaFiles.Remove(file);
                context.SaveChanges();
                System.IO.File.Delete(file.Path);
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    e.Message,
                    e.StackTrace,
                    file.Name,
                    file.Path
                });
            }

            return Ok();
        }

        [HttpGet("{id}/Files")]
        public ActionResult<List<DemandaFile>> GetFiles(int id)
        {
            return context
                .DemandaFiles
                .Where(file => file.DemandaId == id)
                .ToList();
        }

        [HttpPost("{id}/Files")]
        [RequestSizeLimit(1073741824)]
        public async Task<ActionResult<List<DemandaFile>>> Upload(int id)
        {
            return await base.Upload(file =>
            {
                file.DemandaId = id;
                return file;
            });
        }

        protected override DemandaFile FromFormFile(IFormFile file, string filename)
        {
            return new DemandaFile()
            {
                FileName = file.FileName,
                Name = file.FileName,
                ContentType = file.ContentType,
                Path = filename,
                Size = file.Length,
                UserId = this.UserId(),
                CreatedAt = DateTime.Now
            };
        }
    }
}