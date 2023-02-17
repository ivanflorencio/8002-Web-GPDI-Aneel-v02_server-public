using TaesaCore.Models;

namespace PeD.Core.ApiModels.Analises
{
    public class AnalisePedDto : BaseEntity
    {
        public int PropostaId { get; set; }
		public string Originalidade{ get; set; }
		public string Aplicabilidade {get; set; }
		public string Relevancia{ get; set; }
		public string RazoabilidadeCustos{ get; set; }
		public int PontuacaoOriginalidade{ get; set; }
		public int PontuacaoAplicabilidade {get; set; }
		public int PontuacaoRelevancia{ get; set; }
		public int PontuacaoRazoabilidadeCustos{ get; set; }
		public string PontosCriticos{ get; set; }
		public string Comentarios{ get; set; }
		public double PontuacaoFinal { get; set; }
		public string Conceito { get; set; }
		public string Status { get; set; }
    }
}