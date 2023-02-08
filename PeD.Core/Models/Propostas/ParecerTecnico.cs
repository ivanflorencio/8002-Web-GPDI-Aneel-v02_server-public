using System.ComponentModel.DataAnnotations.Schema;
using System;
using TaesaCore.Models;

namespace PeD.Core.Models.Propostas
{
	[Table("PropostaParecerTecnico")]
	public class ParecerTecnico : BaseEntity
    {
		public int CriterioId { get; set; }
		public int AnaliseTecnicaId { get; set; }
		[ForeignKey("AnaliseTecnicaId")] public AnaliseTecnica AnaliseTecnica { get; set; }
		public string ResponsavelId { get; set; }
		[ForeignKey("ResponsavelId")] public ApplicationUser Responsavel { get; set; }
		public DateTime DataHora { get; set; }
		public string Justificativa { get; set; }
		public int Pontuacao { get; set; }
	}
}