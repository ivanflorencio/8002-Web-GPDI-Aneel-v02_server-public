using System.ComponentModel.DataAnnotations.Schema;
using System;
using PeD.Core.Models.Demandas;
using TaesaCore.Models;

namespace PeD.Core.Models.Propostas
{
	[Table("PropostaCriterioAvaliacao")]
	public class CriterioAvaliacao : BaseEntity
    {
		public Guid Guid { get; set; }
		public int DemandaId { get; set; }
		[ForeignKey("DemandaId")] public Demanda Demanda { get; set; }
		public string ResponsavelId { get; set; }
		[ForeignKey("ResponsavelId")] public ApplicationUser Responsavel { get; set; }
		public DateTime DataHora { get; set; }
		public string Descricao { get; set; }
		public int Peso { get; set; }
		public Boolean DoGestor { get; set; }
	}
}