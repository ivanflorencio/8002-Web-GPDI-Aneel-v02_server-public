using System.ComponentModel.DataAnnotations.Schema;

namespace PeD.Core.Models.Projetos
{
    public class Orcamento
    {
        public int Id { get; set; }
        public int ProjetoId { get; set; }
        public string Tipo { get; set; }
        public int EtapaId { get; set; }
        public string HorasEtapas { get; set; }
        public short Ordem { get; set; }
        public int? RecursoHumanoId { get; set; }
        public int? RecursoMaterialId { get; set; }
        public string CategoriaContabil { get; set; }
        public string CategoriaContabilCodigo { get; set; }
        public string Recurso { get; set; }
        public int FinanciadoraId { get; set; }
        public string Financiadora { get; set; }
        public int RecebedoraId { get; set; }
        public string Recebedora { get; set; }
        [Column(TypeName = "decimal(18, 2)")] public decimal Quantidade { get; set; }
        [Column(TypeName = "decimal(18, 2)")] public decimal Custo { get; set; }
        [Column(TypeName = "decimal(18, 2)")] public decimal Total { get; set; }
    }
}