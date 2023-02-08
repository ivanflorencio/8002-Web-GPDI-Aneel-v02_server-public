using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using TaesaCore.Models;

namespace PeD.Core.Models.Propostas
{
	[Table("PropostaAnaliseTecnica")]
	public class AnaliseTecnica : BaseEntity
    {
		public Guid Guid { get; set; }
		public int PropostaId { get; set; }
		[ForeignKey("PropostaId")] public Proposta Proposta { get; set; }
		public string ResponsavelId { get; set; }
		[ForeignKey("ResponsavelId")] public ApplicationUser Responsavel { get; set; }
		public double PontuacaoFinal { get; set; }
		public DateTime DataHora { get; set; }
		public string Justificativa{ get; set; }
		public string Comentarios{ get; set; }
		public string Status { get; set; }
		[InverseProperty("AnaliseTecnica")] public List<ParecerTecnico> Pareceres { get; set; }

	}
}