using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AutoMapper;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeD.Core.ApiModels;
using PeD.Core.ApiModels.Demandas;
using PeD.Core.Exceptions.Demandas;
using PeD.Core.Models.Demandas;
using PeD.Core.Requests.Demanda;
using PeD.Data;
using PeD.Services;
using PeD.Services.Demandas;

namespace PeD.Controllers.Demandas
{
    public partial class DemandaController
    {
        [HttpPost("Criar")]
        public ActionResult<DemandaDto> CriarDemanda([FromBody] string titulo)
        {
            if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Split(' ').Length > 3)
            {
                return BadRequest();
            }

            var demanda = DemandaService.CriarDemanda(titulo, this.UserId());
            return _mapper.Map<DemandaDto>(demanda);
        }

        [HttpHead("{id:int}")]
        public ActionResult HasAccess(int id)
        {
            if (DemandaService.DemandaExist(id))
            {
                if (this.IsAdmin() || DemandaService.UserCanAccess(id, this.UserId()))
                    return Ok();
                return Forbid();
            }

            return NotFound();
        }

        [HttpGet("{id:int}")]
        public ActionResult<DemandaDto> GetById(int id)
        {
            if (this.IsAdmin() || DemandaService.UserCanAccess(id, this.UserId()))
            {
                var demanda = DemandaService.GetById(id);
                return _mapper.Map<DemandaDto>(demanda);
            }

            return NotFound();
        }

        [HttpPut("{id}/Captacao")]
        public ActionResult EnviarCaptacao(int id)
        {
            DemandaService.EnviarCaptacao(id, this.UserId());
            return Ok();
        }

        [HttpPut("{id}/EquipeValidacao")]
        public ActionResult SetEquipeValidacao(int id, [FromBody] SuperiorRequest request)
        {
            if (!DemandaService.DemandaExist(id))
            {
                return NotFound();
            }

            DemandaService.SetSuperiorDireto(id, request.SuperiorDireto, request.TabelaValorHoraId, request.AnalistaPedId, request.AnalistaTecnicoId);

            return Ok();
        }

        [HttpGet("{id}/EquipeValidacao")]
        public ActionResult<object> GetEquipeValidacao(int id)
        {
            var dadosDemanda = DemandaService.GetById(id);
            return dadosDemanda != null ? new {
                    superiorDireto = dadosDemanda.SuperiorDiretoId,
                    analistaTecnico = dadosDemanda.AnalistaTecnicoId,
                    analistaPed = dadosDemanda.AnalistaPedId,
                    tabelaValorHora = dadosDemanda.TabelaValorHora?.Id,
                } : null;
            
        }

        [HttpPut("{id}/Revisor")]
        public ActionResult<DemandaDto> SetRevisor(int id, [FromBody] RevisorRequest request)
        {
            if (!DemandaService.DemandaExist(id))
            {
                return NotFound();
            }

            if (this.IsAdmin() || sistemaService.GetEquipePeD().Coordenador == this.UserId())
            {
                try
                {
                    DemandaService.ProximaEtapa(id, this.UserId(), request.RevisorId, this.IsAdmin());
                }
                catch (DemandaException exception)
                {
                    // DemandaException não expoem o funcionamento da aplicação
                    return BadRequest(exception);
                }
                catch (Exception e)
                {
                    _logger.LogError("Erro na configuração do revisor da demanda: {Error}", e.Message);
                    return Problem();
                }

                return GetById(id);
            }

            return Forbid();
        }

        [HttpPut("{id}/ProximaEtapa")]
        public ActionResult<DemandaDto> AlterarStatusDemanda(int id, [FromBody] DemandaEtapaRequest request)
        {
            DemandaService.ProximaEtapa(id, this.UserId(), asAdmin: this.IsAdmin());

            if (!string.IsNullOrWhiteSpace(request.Comentario))
            {
                DemandaService.AddComentario(id, request.Comentario, this.UserId());
            }

            return GetById(id);
        }

        [HttpPut("{id}/Etapa")]
        public ActionResult<DemandaDto> SetEtapa(int id, [FromBody] StatusRequest data)
        {
            var etapa = data.Status;
            try
            {
                if (etapa < DemandaEtapa.Captacao)
                {
                    DemandaService.SetEtapa(id, etapa, this.UserId());
                }
                else
                {
                    DemandaService.EnviarCaptacao(id, this.UserId());
                }

                return GetById(id);
            }
            catch (Exception e)
            {
                _logger.LogError("Erro na configuração da etapa da demanda: {Error}", e.Message);
                return Problem("Erro na configuração da etapa da demanda", statusCode: StatusCodes.Status409Conflict);
            }
        }

        [HttpPut("{id:int}/Reiniciar")]
        public ActionResult<DemandaDto> Reiniciar(int id, [FromBody] DemandaReprovacao request)
        {
            if (!DemandaService.DemandaExist(id))
                return NotFound();

            var motivo = request.Motivo;

            if (string.IsNullOrWhiteSpace(motivo))
            {
                motivo = "Motivo não informado";
            }

            DemandaService.ReprovarReiniciar(id, this.UserId());
            DemandaService.AddComentario(id, motivo, this.UserId());


            return GetById(id);
        }

        [HttpPut("{id:int}/ReprovarPermanente")]
        public ActionResult<DemandaDto> Finalizar(int id, [FromBody] DemandaReprovacao request)
        {
            if (!DemandaService.DemandaExist(id))
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                return BadRequest();
            }

            DemandaService.ReprovarPermanente(id, this.UserId());
            DemandaService.AddComentario(id, request.Motivo, this.UserId());

            return GetById(id);
        }

        [HttpGet("{id:int}/File/")]
        public ActionResult<List<DemandaFileDto>> GetDemandaFiles(int id)
        {
            var files = DemandaService.GetDemandaFiles(id);
            return _mapper.Map<List<DemandaFileDto>>(files);
        }

        [HttpGet("{id:int}/File/{file_id:int}")]
        public ActionResult<object> GetDemandaFile(int id, int file_id)
        {
            var file = DemandaService.GetDemandaFile(id, file_id);
            if (file != null && System.IO.File.Exists(file.Path))
            {
                return PhysicalFile(file.Path, file.ContentType, file.FileName);
            }

            return NotFound();
        }

        [HttpGet("{id:int}/Form/{form}")]
        public ActionResult<DemandaFormValuesDto> GetDemandaFormValue(int id, string form)
        {
            var data = DemandaService.GetDemandaFormData(id, form);
            return data != null ? Mapper.Map<DemandaFormValuesDto>(data) : null;
        }

        [HttpPut("{id}/Form/{form}")]
        public ActionResult<DemandaFormValuesDto> SalvarDemandaFormValue(int id, string form, [FromBody] JObject data)
        {
            if (DemandaService.DemandaExist(id))
            {
                if (!DemandaService.UserCanAccess(id, this.UserId()))
                    return Forbid();
                var demanda = DemandaService.GetById(id);
                context.Entry(demanda).State = EntityState.Detached;
                if (demanda.SuperiorDiretoId == null)
                    return BadRequest();
                try
                {
                    DemandaService.SalvarDemandaFormData(id, form, data).Wait();
                    var formName = DemandaService.GetForm(form).Title;
                    DemandaService.LogService.Incluir(this.UserId(), id,
                        string.Format("Atualizou Dados do formulário {0}", formName), data, "demanda-form");
                    return Ok();
                }
                catch (DemandaException e)
                {
                    // DemandaException não expoem o funcionamento da aplicação
                    return Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
                }
                catch (Exception e)
                {
                    _logger.LogError("Erro ao salvar formulário da demanda: {Error}", e.Message);
                    return Problem("Erro ao salvar formulário");
                }
            }

            return NotFound();
        }

        [HttpGet("{id:int}/EspecificacaoTecnica/Pdf", Name = "DemandaPdf")]
        [HttpGet("{id:int}/Form/especificacao-tecnica/Pdf")]
        public ActionResult GetDemandaPdf(int id)
        {
            var demanda = DemandaService.Get(id);
            if (demanda.EspecificacaoTecnicaFileId is null)
                return NotFound();

            var file = context.Files.FirstOrDefault(f => f.Id == demanda.EspecificacaoTecnicaFileId.Value);
            if (file is null)
                return NotFound();
            return PhysicalFile(file.Path, "application/pdf", file.FileName);
        }

        [HttpGet("{id:int}/Form/{form}/History")]
        public ActionResult GetDemandaHistorico(int id, string form, [FromServices] IMapper mapper)
        {
            var historico =
                mapper.Map<List<DemandaFormHistoricoListItemDto>>(DemandaService.GetDemandaFormHistoricos(id, form));
            return Ok(historico);
        }

        [HttpGet("{id:int}/Form/{form}/Diff/{historyId}")]
        public async Task<ActionResult> GetDemandaHistoricoDiff(int id, string form, int historyId,
            [FromServices] IViewRenderService viewRenderService)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var demandaForm = DemandaService.GetDemandaFormData(id, form);
            var historico = DemandaService.GetDemandaFormHistorico(historyId);
            var htmlOld = new HtmlDocument();
            var htmlNew = new HtmlDocument();
            IChunker chunker;

            chunker = new CustomFunctionChunker(s =>
                Regex.Split(s, "(<[\\w|\\d]+(?:\\b[^>]*)?>\\s*|\\s*</[\\w|\\d]+>\\s*)"));

            if (historico == null)
                return NotFound(new
                {
                    revisaoAtual = 0,
                    html = ""
                });

            htmlNew.LoadHtml(demandaForm.Html);
            htmlOld.LoadHtml(historico.Content);
            var bodyNew = htmlNew.DocumentNode.SelectSingleNode("//body");
            var bodyOld = htmlOld.DocumentNode.SelectSingleNode("//body");

            var diffFrom = diffBuilder.BuildDiffModel(
                bodyOld.InnerHtml, //HttpUtility.HtmlDecode(htmlOld.DocumentNode.InnerText),
                bodyNew.InnerHtml,
                true,
                true,
                chunker
            );
            var from = await viewRenderService.RenderToStringAsync("Pdf/Diff", diffFrom);

            return Ok(new
            {
                revisaoAtual = demandaForm.Revisao,
                lastUpdate = demandaForm.LastUpdate,
                html = from //bodyNew.InnerHtml,
            });
        }

        [HttpGet("{id:int}/Form/{form}/Debug")]
        public async Task<ActionResult<object>> GetDemandaTeste(int id, string form)
        {
            var doc = await DemandaService.DemandaFormHtml(DemandaService.GetDemandaFormView(id, form));
            if (doc != null)
            {
                return Content(doc, "text/html");
            }

            return NotFound();
        }

        [HttpGet("{id:int}/Form/{form}/DiffPlex/{version}")]
        public ActionResult TestDiffPlex(int id, string form, int version, [FromServices] IMapper mapper,
            [FromServices] IViewRenderService viewRenderService)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var demandaForm = DemandaService.GetDemandaFormData(id, form);
            var historico =
                mapper.Map<List<DemandaFormHistoricoDto>>(DemandaService.GetDemandaFormHistoricos(id, form));
            var html = new HtmlDocument();
            var htmlNew = new HtmlDocument();
            IChunker chunker;

            chunker = new CustomFunctionChunker(s =>
                Regex.Split(s, "(<[\\w|\\d]+(?:\\b[^>]*)?>\\s*|\\s*</[\\w|\\d]+>\\s*)"));
            //chunker = new CustomFunctionChunker(s => Regex.Split(s, "(?=(?:<[\\w|\\d]+\\b[^>]*>|</[\\w|\\d]+>))"));
            // chunker = new CustomFunctionChunker(s => Regex.Split(s, "(?=[\\.;!\\?]|\\n{2,})\\s*?"));
            //chunker = new LineChunker();
            //chunker = new DelimiterChunker(new[] {'.', ';', '!', '?'});


            if (historico == null || historico.Count < version) return Ok();

            htmlNew.LoadHtml(demandaForm.Html);
            html.LoadHtml(historico.ElementAt(version).Content);
            var bodyNew = htmlNew.DocumentNode.SelectSingleNode("//body");
            var bodyOld = html.DocumentNode.SelectSingleNode("//body");

            var diff = diffBuilder.BuildDiffModel(
                bodyOld.InnerHtml, //HttpUtility.HtmlDecode(htmlOld.DocumentNode.InnerText),
                bodyNew.InnerHtml,
                true,
                true,
                chunker
            ); // HttpUtility.HtmlDecode(htmlNew.DocumentNode.InnerText));
            //var content = await viewRenderService.RenderToStringAsync("Pdf/Diff", diff);
            return Ok(diff);
            //return Content(content, "text/html");
        }


        [HttpGet("{id:int}/Logs")]
        public ActionResult<List<DemandaLog>> GetDemandaLog(int id)
        {
            if (this.IsAdmin() || DemandaService.UserCanAccess(id, this.UserId()))
            {
                var logs = DemandaService.GetDemandaLogs(id);
                return logs;
            }

            return Forbid();
        }
    }
}