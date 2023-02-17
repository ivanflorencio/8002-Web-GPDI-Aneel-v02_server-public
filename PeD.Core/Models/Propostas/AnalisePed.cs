using System.ComponentModel.DataAnnotations.Schema;
using System;
using TaesaCore.Models;

namespace PeD.Core.Models.Propostas
{
	[Table("PropostaAnalisePed")]
	public class AnalisePed : BaseEntity
    {
		public Guid Guid { get; set; }
		public int PropostaId { get; set; }
		[ForeignKey("PropostaId")] public Proposta Proposta { get; set; }
		public string ResponsavelId { get; set; }
		[ForeignKey("ResponsavelId")] public ApplicationUser Responsavel { get; set; }
		public DateTime DataHora { get; set; }
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