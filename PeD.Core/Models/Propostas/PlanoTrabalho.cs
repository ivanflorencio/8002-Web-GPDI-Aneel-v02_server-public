using System.ComponentModel.DataAnnotations.Schema;

namespace PeD.Core.Models.Propostas
{
    [Table("PropostaPlanosTrabalhos")]
    public class PlanoTrabalho : PropostaNode
    {
        public string Motivacao { get; set; }
        public string Originalidade { get; set; }
        public string Aplicabilidade { get; set; }
        public string Relevancia { get; set; }
        public string RazoabilidadeCustos { get; set; }
        public string PesquisasCorrelatas { get; set; }
        public string MetodologiaTrabalho { get; set; }
        public string BuscaAnterioridade { get; set; }
    }
}