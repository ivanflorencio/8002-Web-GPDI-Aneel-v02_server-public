using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PeD.Core.Exceptions.Demandas;
using PeD.Core.Models;
using PeD.Core.Models.Captacoes;
using PeD.Core.Models.Demandas;
using PeD.Core.Models.Demandas.Forms;
using PeD.Data;
using PeD.Services.Sistema;
using PeD.Views.Pdf;
using TaesaCore.Interfaces;
using TaesaCore.Services;

namespace PeD.Services.Demandas
{
    public class DemandaService : BaseService<Demanda>
    {
        protected delegate bool CanDemandaProgress(Demanda demanda, string userId);

        #region Statics

        protected static readonly List<FieldList> Forms = new List<FieldList>
        {
            new EspecificacaoTecnicaForm()
        };

        public static FieldList GetForm(string key)
        {
            return Forms.FirstOrDefault(f => f.Key == key);
        }

        #endregion

        #region Props

        private IService<Captacao> _serviceCaptacao;
        private IViewRenderService _viewRender;
        private ILogger<DemandaService> _logger;
        protected Dictionary<DemandaEtapa, CanDemandaProgress> DemandaProgressCheck;
        public readonly DemandaLogService LogService;
        private IConfiguration Configuration;
        SistemaService sistemaService;
        GestorDbContext _context;
        private SendGridService _sendGridService;
        private PdfService _pdfService;
        private IService<TabelaValorHora> _tabelaService;

        #endregion

        #region Contructor

        public DemandaService(
            IRepository<Demanda> repository,
            GestorDbContext context,
            DemandaLogService logService,
            IWebHostEnvironment hostingEnvironment,
            SistemaService sistemaService, IViewRenderService viewRender, IService<Captacao> serviceCaptacao,
            IConfiguration configuration, SendGridService sendGridService, ArquivoService arquivoService,
            PdfService pdfService, ILogger<DemandaService> logger, IService<TabelaValorHora> tabelaService)
            : base(repository)
        {
            _context = context;

            this.sistemaService = sistemaService;
            _viewRender = viewRender;
            _serviceCaptacao = serviceCaptacao;
            Configuration = configuration;
            _sendGridService = sendGridService;
            _pdfService = pdfService;
            _logger = logger;
            _tabelaService = tabelaService;
            LogService = logService;


            DemandaProgressCheck = new Dictionary<DemandaEtapa, CanDemandaProgress>
            {
                { DemandaEtapa.Elaboracao, ElaboracaoProgress },
                { DemandaEtapa.PreAprovacao, PreAprovacaoProgress },
                { DemandaEtapa.RevisorPendente, AprovacaoCoordenadorProgress },
                { DemandaEtapa.AprovacaoRevisor, AprovacaoRevisorProgress },
                { DemandaEtapa.AprovacaoCoordenador, AprovacaoCoordenadorProgress },
                { DemandaEtapa.AprovacaoGerente, AprovacaoGerenteProgress },
                { DemandaEtapa.AprovacaoDiretor, AprovacaoDiretorProgress }
            };
        }

        #endregion

        #region Helpers

        public Demanda GetById(int id)
        {
            return _context.Demandas
                .Include(d => d.TabelaValorHora)
                .Include("Criador")
                .Include("Revisor")
                .Include("SuperiorDireto")
                .Include("AnalistaPed")
                .Include("AnalistaTecnico")
                .Include("Comentarios.User")
                .FirstOrDefault(d => d.Id == id);
        }

        public bool DemandaExist(int id)
        {
            return _context.Demandas.Any(d => d.Id == id);
        }

        public bool UserCanAccess(int id, string userId)
        {
            if (DemandaExist(id))
            {
                if (sistemaService.GetEquipePeD().CargosChavesIds.Contains(userId))
                    return true;
                var demanda = _context.Demandas.Find(id);
                return demanda.CriadorId == userId || demanda.SuperiorDiretoId == userId ||
                       demanda.RevisorId == userId;
            }

            return false;
        }

        #endregion

        #region Listar Demandas

        protected IQueryable<Demanda> QueryDemandas(string userId = null)
        {
            var cargosChavesIds = sistemaService.GetEquipePeD().CargosChavesIds;
            return _context.Demandas
                .Include("Criador")
                .Include("SuperiorDireto")
                .Include("TabelaValorHora")
                .Include("Revisor")
                .ByUser(cargosChavesIds.Contains(userId) ? null : userId);
        }

        public List<Demanda> GetByEtapa(DemandaEtapa demandaEtapa, string userId = null)
        {
            return QueryDemandas(userId).Where(d => d.EtapaAtual == demandaEtapa).ToList();
        }

        public List<Demanda> GetByEtapaStatus(DemandaStatus status, string userId = null)
        {
            return QueryDemandas(userId)
                .Where(d => d.Status == status).ToList();
        }

        public List<Demanda> GetDemandasReprovadas(string userId = null)
        {
            return QueryDemandas(userId)
                .Where(d => d.Status == DemandaStatus.ReprovadaPermanente).ToList();
        }

        public List<Demanda> GetDemandasAprovadas(string userId = null)
        {
            return QueryDemandas(userId)
                // .Where(d => d.EtapaAtual == Etapa.AprovacaoDiretor && (d.EtapaStatus == EtapaStatus.Aprovada && d.EtapaStatus == EtapaStatus.Concluido)).ToList();
                .Where(d => d.EtapaAtual == DemandaEtapa.AprovacaoDiretor && d.Status == DemandaStatus.Aprovada)
                .ToList();
        }

        public List<Demanda> GetDemandasEmElaboracao(string userId = null)
        {
            return QueryDemandas(userId)
                .Where(d => d.EtapaAtual == DemandaEtapa.Elaboracao || d.Status == DemandaStatus.EmElaboracao ||
                            d.Status == DemandaStatus.Pendente).ToList();
        }

        public List<Demanda> GetDemandasCaptacao(string userId = null)
        {
            var demandas = QueryDemandas(userId).ToList();
            return demandas.Where(d => d.EtapaAtual == DemandaEtapa.Captacao).ToList();
        }

        public List<Demanda> GetDemandasPendentes(string userId = null)
        {
            return GetByEtapaStatus(DemandaStatus.Pendente, userId);
        }

        #endregion

        #region Criação e Alteração de Demandas

        public Demanda CriarDemanda(string titulo, string userId)
        {
            var demanda = new Demanda
            {
                Titulo = titulo,
                CriadorId = userId,
                EtapaAtual = DemandaEtapa.Elaboracao,
                Status = DemandaStatus.EmElaboracao
            };
            _context.Demandas.Add(demanda);
            _context.SaveChanges();
            demanda = GetById(demanda.Id);
            LogService.Incluir(userId, demanda.Id, "Criou Demanda",
                string.Format(" {0} criou demanda \"{1}\"", demanda.Criador.NomeCompleto, demanda.Titulo));
            return demanda;
        }

        public Demanda AlterarStatusDemanda(int id, DemandaStatus status)
        {
            var demanda = GetById(id);
            if (demanda != null)
            {
                demanda.Status = status;
                _context.SaveChanges();
            }

            return demanda;
        }

        public void SetEtapa(int id, DemandaEtapa demandaEtapa, string userId)
        {
            var demanda = GetById(id);
            if (demanda == null)
                return;
            var user = _context.Users.Find(userId);
            demanda.IrParaEtapa(demandaEtapa);
            _context.SaveChanges();
            NotificarResponsavel(demanda, userId);
            if (demanda.EtapaAtual < DemandaEtapa.Captacao && demanda.Status == DemandaStatus.EmElaboracao)
            {
                LogService.Incluir(userId, demanda.Id, "Alterou Etapa",
                    string.Format(" {0} alterou a etapa da demanda para \"{1}\"", user.NomeCompleto,
                        demanda.EtapaDesc));
            }
            else if (demanda.EtapaAtual == DemandaEtapa.AprovacaoDiretor && demanda.Status == DemandaStatus.Concluido)
            {
                LogService.Incluir(userId, demanda.Id, "Aprovou a etapa",
                    string.Format(" {0} aprovou a demanda.", user.NomeCompleto));
            }
        }

        public void ProximaEtapa(int id, string userId, string revisorId = null, bool asAdmin = false)
        {
            var demanda = GetById(id);


            if (demanda != null)
            {
                if (!string.IsNullOrWhiteSpace(revisorId) && _context.Users.Any(user => user.Id == revisorId))
                {
                    demanda.RevisorId = revisorId;
                }

                var responsavelId = GetResponsavelAtual(demanda);
                if (responsavelId == userId || asAdmin)
                {
                    demanda.ProximaEtapa();
                    _context.SaveChanges();
                    NotificarResponsavel(demanda, responsavelId);

                    var user = _context.Users.Find(userId);
                    if (demanda.Status == DemandaStatus.Aprovada)
                    {
                        LogService.Incluir(responsavelId, demanda.Id, "Aprovação de demanda",
                            string.Format("O usuário {0} aprovou a demanda", user.NomeCompleto));
                    }
                    else
                    {
                        LogService.Incluir(responsavelId, demanda.Id, "Avanço de Etapa",
                            string.Format(" {0} alterou a etapa da demanda para \"{1}\"", user.NomeCompleto,
                                demanda.EtapaDesc));
                    }

                    var revisao = _context.DemandaFormValues.Include("Files")
                        .FirstOrDefault(df => df.DemandaId == id && df.FormKey == EspecificacaoTecnicaForm.Key)
                        ?.Revisao ?? 1;
                    SavePdf(demanda, EspecificacaoTecnicaForm.Key, revisao.ToString()).Wait();
                }
                else
                {
                    throw new DemandaException("O usuário não é responsável pela continuidade da demanda");
                }
            }
            else
            {
                throw new Exception("Demanda não encontrada");
            }
        }

        public void SetSuperiorDireto(int id, string superiorDiretoId, string tabelaValorHoraId, string analistaPedId, string analistaTecnicoId)
        {
            if (DemandaExist(id))
            {
                var demanda = GetById(id);
                var novoAnalistaTecnico = demanda.AnalistaTecnicoId != analistaTecnicoId;
                var novoAnalistaPed = demanda.AnalistaPedId != analistaPedId;

                // Definindo o superior
                demanda.SuperiorDiretoId = superiorDiretoId;

                // Definindo analistas responsáveis
                if (novoAnalistaPed)
                {
                    demanda.AnalistaPedId = analistaPedId;
                }
                if (novoAnalistaTecnico)
                {
                    demanda.AnalistaTecnicoId = analistaTecnicoId;
                }

                // Definindo tabela de valor Hora Homem da demanda
                if (tabelaValorHoraId != null)
                {
                    demanda.TabelaValorHoraId = Int32.Parse(tabelaValorHoraId);
                }
                _context.SaveChanges();

                var tabela = _tabelaService.Get(demanda.TabelaValorHoraId.Value);
                var user = _context.Users.Find(superiorDiretoId);
                LogService.Incluir(demanda.CriadorId, demanda.Id, "Definiu Superior Direto e Tabela de Valor/Hora",
                    string.Format(" {0} definiu o usuário {1} como superior direto e definiu a tabela: '{2}'", demanda.Criador.NomeCompleto,
                        user.NomeCompleto, tabela.Nome));


                //Notificando analistas
                demanda = GetById(id);
                if (novoAnalistaPed)
                {
                    NotificarAnalistaPed(demanda);
                }
                if (novoAnalistaTecnico)
                {
                    NotificarAnalistaTecnico(demanda);
                }

                return;
            }

            throw new DemandaException("Demanda não existe");
        }

        public void ReprovarReiniciar(int id, string userId)
        {
            if (!DemandaExist(id))
            {
                throw new DemandaException("Demanda Não existe");
            }

            var demanda = GetById(id);

            if (demanda != null && DemandaProgressCheck.ContainsKey(demanda.EtapaAtual))
            {
                demanda.ReprovarReiniciar();
                _context.SaveChanges();
                NotificarReprovacao(demanda, _context.Users.Find(userId));
                var user = _context.Users.Find(userId);
                LogService.Incluir(userId, id, "Reiniciou a demanda",
                    string.Format("O usuário {0} reiniciou a demanda", user.NomeCompleto));
            }
        }

        public void ReprovarPermanente(int id, string userId)
        {
            if (!DemandaExist(id))
            {
                throw new DemandaException("Demanda Não existe");
            }

            var demanda = GetById(id);

            if (demanda != null && DemandaProgressCheck.ContainsKey(demanda.EtapaAtual))
            {
                demanda.ReprovarPermanente();
                _context.SaveChanges();
                NotificarReprovacaoPermanente(demanda, _context.Users.Find(userId));
                var user = _context.Users.Find(userId);
                LogService.Incluir(userId, id, "Arquivou a demanda",
                    string.Format("O usuário {0} reprovou e arquivou a demanda", user.NomeCompleto));
            }
        }

        public void AddComentario(int id, DemandaComentario comentario)
        {
            var demanda = GetById(id);
            if (demanda != null)
            {
                demanda.Comentarios = demanda.Comentarios ?? new List<DemandaComentario>();
                demanda.Comentarios.Add(comentario);
                _context.SaveChanges();
            }
        }

        public void AddComentario(int id, string comentario, string userId)
        {
            AddComentario(id, new DemandaComentario
            {
                Content = comentario,
                DemandaId = id,
                UserId = userId,
                CreatedAt = DateTime.Now
            });
        }

        public IEnumerable<CaptacaoSubTema> CaptacaoSubTemasFromJArray(JArray jArray)
        {
            return jArray.Select(t =>
            {
                var subtema = t as JObject;
                if (subtema != null && subtema.TryGetValue("catalogSubTemaId", out var subtemaId))
                {
                    try
                    {
                        return new CaptacaoSubTema()
                        {
                            SubTemaId = subtemaId.Value<int>(),
                            Outro = subtema.GetValue("outroDesc")?.Value<string>() ?? ""
                        };
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }

                return null;
            }).Where(s => s != null);
        }

        public void EnviarCaptacao(int id, string userId)
        {
            var demanda = GetById(id);
            if (demanda is null || demanda.EtapaAtual == DemandaEtapa.Captacao)
                return;

            if (!demanda.EspecificacaoTecnicaFileId.HasValue)
            {
                throw new DemandaException("Especificação Técnica ausente");
            }

            var especificacaoTecnicaForm = GetDemandaFormData(id, EspecificacaoTecnicaForm.Key);
            var temaAneel =
                especificacaoTecnicaForm?.Object.SelectToken(EspecificacaoTecnicaForm.TemaPath) as JObject ??
                throw new DemandaException("Especificação Técnica não configurada corretamente");

            if (temaAneel == null)
                throw new DemandaException("Tema Aneel ausente");

            var catalogTemaId = temaAneel.GetValue("catalogTemaId").ToString();
            int temaId = String.IsNullOrEmpty(catalogTemaId) ? 0 : Int32.Parse(catalogTemaId);
            var temaOutro = temaAneel.GetValue("outroDesc")?.Value<string>();
            var subtemas = temaAneel.GetValue("subTemas") as JArray;
            var captacaoSubTemas = CaptacaoSubTemasFromJArray(subtemas);
            var prevCaptcao = _context.Set<Captacao>().FirstOrDefault(c => c.DemandaId == id);
            var captacao = prevCaptcao ?? new Captacao
            {
                DemandaId = id,
                CriadorId = demanda.CriadorId,
                TemaId = temaId > 0 ? temaId : (int?)null,
                TemaOutro = temaOutro,
                Titulo = demanda.Titulo,
                Status = Captacao.CaptacaoStatus.Pendente,
                SubTemas = captacaoSubTemas?.ToList() ?? new List<CaptacaoSubTema>()
            };
            captacao.EspecificacaoTecnicaFileId = demanda.EspecificacaoTecnicaFileId.Value;
            demanda.EtapaAtual = DemandaEtapa.Captacao;
            demanda.Status = DemandaStatus.Concluido;
            demanda.CaptacaoDate = DateTime.UtcNow;
            demanda.ValidarContinuidade();
            _context.SaveChanges();

            if (captacao.Id == 0)
                _serviceCaptacao.Post(captacao);
            else
            {
                _serviceCaptacao.Put(captacao);
            }

            var user = _context.Users.Find(userId);

            LogService.Incluir(userId, id, "Demanda para captação",
                string.Format("O usuário {0} enviou a demanda para a captação", user.NomeCompleto));
        }

        #endregion

        #region Progresso das demandas

        protected string GetResponsavelAtual(Demanda demanda)
        {
            switch (demanda.EtapaAtual)
            {
                case DemandaEtapa.Elaboracao:
                    return demanda.CriadorId;
                case DemandaEtapa.PreAprovacao:
                    return demanda.SuperiorDiretoId;
                case DemandaEtapa.AprovacaoRevisor:
                    return demanda.RevisorId;
                case DemandaEtapa.RevisorPendente:
                case DemandaEtapa.AprovacaoCoordenador:
                    return sistemaService.GetEquipePeD().Coordenador;
                case DemandaEtapa.AprovacaoGerente:
                    return sistemaService.GetEquipePeD().Gerente;
                case DemandaEtapa.AprovacaoDiretor:
                    return sistemaService.GetEquipePeD().Diretor;
                default:
                    return null;
            }
        }

        protected bool ElaboracaoProgress(Demanda demanda, string userId)
        {
            return demanda.CriadorId == userId;
        }

        protected bool PreAprovacaoProgress(Demanda demanda, string userId)
        {
            return demanda.SuperiorDiretoId == userId;
        }

        protected bool AprovacaoRevisorProgress(Demanda demanda, string userId)
        {
            return demanda.RevisorId == userId;
        }

        protected bool AprovacaoCoordenadorProgress(Demanda demanda, string userId)
        {
            return userId == sistemaService.GetEquipePeD().Coordenador;
        }

        protected bool AprovacaoGerenteProgress(Demanda demanda, string userId)
        {
            return userId == sistemaService.GetEquipePeD().Gerente;
        }

        protected bool AprovacaoDiretorProgress(Demanda demanda, string userId)
        {
            return userId == sistemaService.GetEquipePeD().Diretor;
        }

        #endregion

        #region Documentos das demandas

        public DemandaFormValues GetDemandaFormData(int id, string form)
        {
            return _context.DemandaFormValues.Include("Files.File")
                .FirstOrDefault(df => df.DemandaId == id && df.FormKey == form);
        }

        public List<DemandaFormHistorico> GetDemandaFormHistoricos(int id, string form)
        {
            return _context.DemandaFormValues.Include("Historico")
                .FirstOrDefault(df => df.DemandaId == id && df.FormKey == form)?.Historico
                .Where(hist => hist.Revisao != hist.FormValues.Revisao)
                .OrderByDescending(hist => hist.CreatedAt)
                .ToList();
        }

        public DemandaFormHistorico GetDemandaFormHistorico(int id)
        {
            return _context.DemandaFormHistoricos.FirstOrDefault(h => h.Id == id);
        }

        public List<DemandaFile> GetDemandaFiles(int id)
        {
            return _context.DemandaFormValues
                .Include("Files.File")
                .Where(df => df.DemandaId == id)
                .SelectMany(_dfv => _dfv.Files.Select(dff => dff.File))
                .Distinct()
                .ToList();
        }

        public FileUpload GetDemandaFile(int id, int file_id)
        {
            return GetDemandaFiles(id).First(file => file.Id == file_id);
        }

        public async Task SalvarDemandaFormData(int id, string form, JObject data)
        {
            var formdata = data.Value<JObject>("form");
            var formanexos = data.Value<JArray>("anexos");
            var dfData = _context.DemandaFormValues.Include("Files")
                .FirstOrDefault(df => df.DemandaId == id && df.FormKey == form);
            if (dfData != null)
            {
                dfData.SetValue(formdata);
                dfData.Files = formanexos.ToList().Select(item => new DemandaFormFile
                {
                    DemandaFormId = dfData.Id,
                    FileId = item.Value<int>()
                }).ToList();
                dfData.Revisao++;
                _context.DemandaFormValues.Update(dfData);
            }
            else
            {
                dfData = new DemandaFormValues();
                dfData.Revisao = 1;
                dfData.DemandaId = id;
                dfData.FormKey = form;
                dfData.SetValue(formdata);
                dfData.Files = formanexos.ToList().Select(item => new DemandaFormFile
                {
                    FileId = item.Value<int>()
                }).ToList();
                _context.DemandaFormValues.Add(dfData);
            }

            dfData.LastUpdate = DateTime.Now;

            _context.SaveChanges();

            var demanda = _context.Demandas.FirstOrDefault(d => d.Id == id);
            await SavePdf(demanda, form, dfData.Revisao.ToString());
        }

        protected async Task SavePdf(Demanda demanda, string form, string revisao)
        {
            if (string.IsNullOrWhiteSpace(demanda?.SuperiorDiretoId))
                throw new DemandaException("Defina o superior direto antes de continuar");

            var demandaFormView = GetDemandaFormView(demanda.Id, form);
            var versao = await SaveDemandaFormHistorico(demandaFormView);
            var file = SaveDemandaFormPdf(demanda.Id, form, versao.Content, revisao);
            demanda.EspecificacaoTecnicaFileId = file.Id;
            _context.SaveChanges();
        }

        public DemandaFormView GetDemandaFormView(int id, string form)
        {
            var mainForm = GetForm(form);
            var demanda = GetById(id);
            var renderDocument = RenderDocument(id, form);
            var formDemanda = _context.DemandaFormValues.FirstOrDefault(df => df.DemandaId == id && df.FormKey == form);
            var equipePed = sistemaService.GetEquipePeD();

            return new DemandaFormView
            {
                Diretor = _context.Users.First(u => u.Id == equipePed.Diretor),
                Coordenador = _context.Users.First(u => u.Id == equipePed.Coordenador),
                Gerente = _context.Users.First(u => u.Id == equipePed.Gerente),
                Demanda = demanda,
                Form = mainForm,
                Rendered = renderDocument,
                DemandaFormValues = formDemanda
            };
        }

        public async Task<string> DemandaFormHtml(DemandaFormView demandaFormView)
        {
            return await _viewRender.RenderToStringAsync("Pdf/DemandaFormView", demandaFormView);
        }

        public FieldRendered RenderDocument(int id, string form)
        {
            var field = GetForm(form);
            var data = GetDemandaFormData(id, form);
            if (data != null && field != null)
            {
                field.RenderHandler = SanitizerFieldRender;
                return field.Render(data.Object);
            }

            return new FieldRendered("Error", "No data or Form found");
        }

        public FileUpload SaveDemandaFormPdf(int id, string form, string html, string revisao)
        {
            var arquivo = _pdfService.HtmlToPdf(html, $"{form}-rv{revisao}");
            UpdatePdf(arquivo.Path);
            return arquivo;
        }

        public async Task<DemandaFormHistorico> SaveDemandaFormHistorico(DemandaFormView demandaFormView)
        {
            var html = await DemandaFormHtml(demandaFormView);
            var historico = new DemandaFormHistorico
            {
                FormValuesId = demandaFormView.DemandaFormValues.Id,
                Content = html,
                Revisao = demandaFormView.DemandaFormValues.Revisao,
                CreatedAt = DateTime.Now
            };
            _context.DemandaFormHistoricos.Add(historico);
            _context.SaveChanges();

            return historico;
        }

        public string GetDemandaFormPdfFilename(int id, string form, string revisao, bool createDirectory = false)
        {
            var storagePath = Configuration.GetValue<string>("StoragePath");

            var folderName = Path.Combine(storagePath, "demandas", id.ToString());

            var filename = string.Format("{0}-{1}.pdf", form, revisao);
            if (createDirectory && !Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            return Path.Combine(folderName, filename);
        }

        protected void UpdatePdf(string filename)
        {
            var filetmp = filename + ".tmp";
            var pdfDoc = new PdfDocument(new PdfReader(filename), new PdfWriter(filetmp));
            var doc = new Document(pdfDoc);
            //var font = PdfFontFactory.CreateFont(Path.Combine(_hostingEnvironment.WebRootPath, "Assets/fonts/Roboto-Regular.ttf"));
            //doc.SetFont(font);
            for (var i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var width = pdfDoc.GetPage(i).GetPageSize().GetWidth();
                var height = pdfDoc.GetPage(i).GetPageSize().GetTop();
                // float bottom = pdfDoc.GetPage(i).GetPageSize().GetBottom();
                //
                // float x = pdfDoc.GetPage(i).GetPageSize().GetWidth() / 2;
                // float y = pdfDoc.GetPage(i).GetPageSize().GetBottom() + 20;

                var pages = new Paragraph(string.Format("Folha {0} de {1}", i, pdfDoc.GetNumberOfPages()))
                    .SetFontSize(12)
                    .SetFontColor(ColorConstants.BLACK);
                doc.ShowTextAligned(pages, width - 120, height - 90, i, TextAlignment.CENTER, VerticalAlignment.BOTTOM,
                    0);
                //doc.ShowTextAligned(pages, width, top + 40, i, TextAlignment.RIGHT, VerticalAlignment.BOTTOM, 0);
            }

            doc.Close();
            File.Delete(filename);
            File.Move(filetmp, filename);
        }

        protected void SanitizerFieldRender(Field field, JToken _data, ref FieldRendered fieldRendered)
        {
            if (field.FieldType == "Temas")
            {
                fieldRendered.Value = "";

                var tema = (_data as JObject).GetValue("value") as JObject;
                var catalogId = tema.GetValue("catalogTemaId").Value<int>();
                var outroDesc = tema.GetValue("outroDesc").Value<string>();

                var catalogTema = _context.Temas.Find(catalogId);
                if (catalogTema != null)
                {
                    fieldRendered.Value = catalogTema.Nome;
                    if (!string.IsNullOrWhiteSpace(outroDesc))
                    {
                        fieldRendered.Value = string.Concat(catalogTema.Nome, ": ", outroDesc);
                    }
                }


                var subtemas = tema.GetValue("subTemas") as JArray;
                var subtemasList = new List<FieldRendered>();
                subtemas.Children().ToList().ForEach(child =>
                {
                    var catalogSubTemaId = (child as JObject).GetValue("catalogSubTemaId").Value<int>();
                    var subOutroDesc = (child as JObject).GetValue("outroDesc").Value<string>();

                    var catalogSubTema = _context.Temas.Find(catalogSubTemaId);
                    if (catalogSubTema != null)
                    {
                        var item = new FieldRendered("Sub Tema", catalogSubTema.Nome);
                        if (!string.IsNullOrWhiteSpace(subOutroDesc))
                        {
                            item.Value = string.Concat(catalogSubTema.Nome, ": ", subOutroDesc);
                        }

                        subtemasList.Add(item);
                    }
                });

                fieldRendered.Children.AddRange(subtemasList);
            }
        }

        #endregion

        #region Notificação do usuários

        public void NotificarResponsavel(Demanda demanda, string userId)
        {
            try
            {
                switch (demanda.EtapaAtual)
                {
                    case DemandaEtapa.PreAprovacao:
                        NotificarSuperior(demanda);
                        break;

                    case DemandaEtapa.RevisorPendente:
                        NotificarRevisorPendente(demanda);
                        break;

                    case DemandaEtapa.AprovacaoRevisor:
                        NotificarRevisor(demanda);
                        break;

                    case DemandaEtapa.AprovacaoCoordenador:
                    case DemandaEtapa.AprovacaoGerente:
                        NotificarAprovador(demanda, userId);
                        break;

                    case DemandaEtapa.AprovacaoDiretor:
                        if (demanda.Status != DemandaStatus.Aprovada)
                            NotificarAprovador(demanda, userId);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Erro ao notificar o usuário responsável. \n {Error}", e.Message);
            }
        }

        public void NotificarSuperior(Demanda demanda)
        {
            var url = Configuration.GetValue<string>("Url");
            var titulo = $"Nova Demanda para Pré-Aprovação:\"{demanda.Titulo}\"";
            var body =
                $"O usuário {demanda.Criador.NomeCompleto} enviou a demanda \"{demanda.Titulo}\" para Pré-Aprovação pelo seu superior direto. Clique abaixo para mais detalhes.";

            _sendGridService.Send(demanda.SuperiorDireto.Email, titulo, body,
                actionLabel: "Ver Demanda",
                actionUrl: $"{url}/demandas/{demanda.Id}").Wait();
        }

        public void NotificarAnalistaTecnico(Demanda demanda, bool isFimCaptacao = false)
        {
            if (demanda == null) return;

            var url = Configuration.GetValue<string>("Url");
            var titulo = $"Demanda para Análise Técnica:\"{demanda.Titulo}\"";
            var body =
                $"O usuário {demanda.Criador.NomeCompleto} atribuiu as propostas da demanda \"{demanda.Titulo}\" para que você faça a Análise Técnica. Clique abaixo para mais detalhes.";

            if (isFimCaptacao)
            {
                body =
                $"A demanda \"{demanda.Titulo}\" está com a captação finalizada. Verifique se existem propostas com Análise Técnica pendentes ou abertas. Clique abaixo para mais detalhes.";
            }

            _sendGridService.Send(demanda.AnalistaPed.Email, titulo, body,
                actionLabel: "Análise Técnica",
                actionUrl: $"{url}/analise-tecnica").Wait();
        }

        public void NotificarAnalistaPed(Demanda demanda, bool isFimCaptacao = false)
        {
            if (demanda == null) return;

            var url = Configuration.GetValue<string>("Url");
            var titulo = $"Demanda para Análise P&D:\"{demanda.Titulo}\"";
            var body =
                $"O usuário {demanda.Criador.NomeCompleto} atribuiu as propostas da demanda \"{demanda.Titulo}\" para que você faça a Análise P&D. Clique abaixo para mais detalhes.";

            if (isFimCaptacao)
            {
                body =
                $"A demanda \"{demanda.Titulo}\" está com a captação finalizada. Verifique se existem propostas com Análise P&D pendentes ou abertas. Clique abaixo para mais detalhes.";
            }

            _sendGridService.Send(demanda.AnalistaPed.Email, titulo, body,
                actionLabel: "Análise P&D",
                actionUrl: $"{url}/analise-ped").Wait();
        }

        public void NotificarReprovacao(Demanda demanda, ApplicationUser avaliador)
        {
            var url = Configuration.GetValue<string>("Url");
            var titulo = $"Foram solicitados ajustes para o \"{demanda.Titulo}\" na etapa de Revisão";

            var body =
                $"O usuário {avaliador.NomeCompleto} revisor da sua demanda, inseriu alguns comentários e solicitou alterações no projeto. Clique abaixo para mais detalhes e enviar novamentepara revisão";
            _sendGridService.Send(demanda.Criador.Email, titulo, body, actionLabel: "Ver Demanda",
                actionUrl: $"{url}/demandas/{demanda.Id}").Wait();
        }

        public void NotificarReprovacaoPermanente(Demanda demanda, ApplicationUser avaliador)
        {
            var url = Configuration.GetValue<string>("Url");
            var titulo =
                $"Sua demanda \"{demanda.Titulo}\" foi reprovada e arquivada na etapa de Revisão. Nova Demanda para Pré-Aprovação:";

            var body =
                $"O usuário {avaliador.NomeCompleto} revisor da sua demanda, reprovou e arquivou sua demanda . Clique abaixo para mais detalhes:";
            _sendGridService.Send(demanda.Criador.Email, titulo, body,
                actionLabel: "Ver Demanda",
                actionUrl: $"{url}/demandas/{demanda.Id}").Wait();
        }

        public void NotificarRevisorPendente(Demanda demanda)
        {
            var url = Configuration.GetValue<string>("Url");
            var coordenador = _context.Users.Find(sistemaService.GetEquipePeD().Coordenador);
            var titulo = $"Nova Demanda para Definição de Revisor: \"{demanda.Titulo}\"";

            var body =
                $"O usuário {demanda.Criador.NomeCompleto} cadastrou uma nova demanda \"{demanda.Titulo}\" que já foi pré-aprovada pelo seu superior direto. Precisamos agora que seja definido o revisor responsável pela demanda. Clique abaixo para mais detalhes.";

            _sendGridService.Send(coordenador.Email, titulo, body,
                actionLabel: "Ver Demanda",
                actionUrl: $"{url}/demandas/{demanda.Id}").Wait();
        }

        public void NotificarRevisor(Demanda demanda)
        {
            var url = Configuration.GetValue<string>("Url");
            var coordenador = _context.Users.Find(sistemaService.GetEquipePeD().Coordenador);
            var titulo = $"Nova Demanda para Revisão: \"{demanda.Titulo}\"";

            var body =
                $"O usuário {coordenador.NomeCompleto} enviou a demanda \"{demanda.Titulo}\" para Revisão. Clique abaixo para mais detalhes.";

            _sendGridService.Send(demanda.Revisor.Email, titulo, body,
                actionLabel: "Ver Demanda",
                actionUrl: $"{url}/demandas/{demanda.Id}").Wait();
        }

        public void NotificarAprovador(Demanda demanda, string avaliadorAnteriorId)
        {
            var url = Configuration.GetValue<string>("Url");
            var avaliador = _context.Users.Find(avaliadorAnteriorId);
            var responsavel = _context.Users.Find(GetResponsavelAtual(demanda));
            var titulo = $"Nova Demanda para Aprovação: \"{demanda.Titulo}\"";
            var body =
                $"O usuário {avaliador.NomeCompleto} enviou a demanda \"{demanda.Titulo}\" para Aprovação. Clique abaixo para mais detalhes.";
            //.SendMailBase(responsavel, titulo, body, ("Ver Demanda", $"/demandas/{demanda.Id}"));
            _sendGridService.Send(responsavel.Email, titulo, body,
                    actionLabel: "Ver Demanda",
                    actionUrl: $"{url}/demandas/{demanda.Id}")
                .Wait();
        }

        #endregion

        #region Logs da demanda

        public List<DemandaLog> GetDemandaLogs(int demandaId)
        {
            return _context.DemandaLogs.Include("User").Where(dl => dl.DemandaId == demandaId).OrderBy(dl => dl.Id)
                .ToList();
        }

        #endregion
    }

    public static class DemandaExtension
    {
        public static IQueryable<Demanda> ByUser(this IQueryable<Demanda> dbSet, string userId)
        {
            return dbSet.Where(demanda =>
                string.IsNullOrWhiteSpace(userId) || demanda.CriadorId == userId ||
                demanda.SuperiorDiretoId == userId || demanda.RevisorId == userId);
        }
    }
}